using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Alexa.NET;
using Alexa.NET.Request;
using Alexa.NET.Request.Type;
using Alexa.NET.Response;
using CodemotionRome19.Core.Azure;
using CodemotionRome19.Core.Configuration;
using CodemotionRome19.Core.Models;
using CodemotionRome19.Functions.Alexa;
using CodemotionRome19.Functions.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Serilog;

namespace CodemotionRome19.Functions
{
    public class Aldo
    {
        #region Fields

        const string BreakStrong = "<break strength=\"strong\"/>";
        const string BreakMedium = "<break strength=\"medium\"/>";
        const string Break = "<break/>";
        const string Notify = "Ti avviserò con una notifica appena terminato!";

        readonly IConfiguration configuration;

        #endregion 

        public Aldo(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        [FunctionName("Aldo")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)]HttpRequest req,
            [Queue("azure-resource-deploy", Connection = "AzureWebJobsStorage")] IAsyncCollector<AzureResourceToDeploy> resourceDeployQueue,
            [Queue("project-deploy", Connection = "AzureWebJobsStorage")] IAsyncCollector<ProjectToDeploy> projectDeployQueue)
        {
            var connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage", EnvironmentVariableTarget.Process);
            //var tableName = Environment.GetEnvironmentVariable("deployLog", EnvironmentVariableTarget.Process);
            var log = new LoggerConfiguration()
                .WriteTo.AzureTableStorage(connectionString, storageTableName: "AldoLog")
                .CreateLogger();

            var json = await req.ReadAsStringAsync();
            var skillRequest = JsonConvert.DeserializeObject<SkillRequest>(json);

            // Verifies that the request is coming from Alexa.
            var isValid = await skillRequest.ValidateRequest(req, log);
            if (!isValid)
                return new BadRequestResult();
            
            var session = skillRequest.Session;            
            var request = skillRequest.Request;
            SkillResponse response = null;

            try
            {
                switch (request)
                {
                    case LaunchRequest launchRequest:
                        response = HandleLaunchRequest(launchRequest, session);
                        break;
                    case IntentRequest intentRequest when intentRequest.Intent.Name == Intents.HelpIntent:
                        response = HandleHelpIntent(intentRequest, session);
                        break;
                    case IntentRequest intentRequest when intentRequest.Intent.Name == Intents.CancelIntent:
                        response = HandleCancelIntent(intentRequest, session);
                        break;
                    case IntentRequest intentRequest when intentRequest.Intent.Name == Intents.StopIntent:
                        response = HandleStopIntent(intentRequest, session);
                        break;
                    case IntentRequest intentRequest when CanHandleSetResourceTypeIntent(intentRequest, session):
                        response = HandleSetResourceTypeIntent(intentRequest, session);
                        break;
                    case IntentRequest intentRequest when CanHandleAskForResourceNameIntent(intentRequest, session):
                        response = HandleAskForResourceNameIntent(intentRequest, session);
                        break;
                    case IntentRequest intentRequest when CanHandlSetResourceNameIntent(intentRequest, session):
                        response = HandlSetResourceNameIntent(intentRequest, session);
                        break;
                    case IntentRequest intentRequest when CanHandleCreateResourceIntent(intentRequest, session):
                        response = await HandleCreateResourceIntent(intentRequest, session, resourceDeployQueue);
                        break;
                    case IntentRequest intentRequest when CanHandleAskForDeployProjectIntent(intentRequest, session):
                        response = await HandleAskForDeployProjectIntent(intentRequest, session, resourceDeployQueue);
                        break;
                    case IntentRequest intentRequest when CanHandleAskForAnotherResourceIntent(intentRequest, session):
                        response = HandleAskForAnotherResourceIntent(intentRequest, session);
                        break;
                    case IntentRequest intentRequest when CanHandleDeployProjectIntent(intentRequest, session):
                        response = await HandleDeployProjectIntent(intentRequest, session, projectDeployQueue, resourceDeployQueue);
                        break;
                    case IntentRequest intentRequest: //Unhandled
                        response = HandleUnhandled(intentRequest);
                        break;
                    case SessionEndedRequest sessionEndedRequest:
                        response = HandleSessionEndedRequest(sessionEndedRequest);
                        break;
                }
            }
            catch (Exception e)
            {
                var error = $"{e.Message}\n\r{e.StackTrace}";
                log.Error(error);
                response = ResponseBuilder.Tell("Mi dispiace, c'è stato un errore inatteso. Per favore, riprova più tardi.");
            }

            return new OkObjectResult(response);
        }

        #region Methods        

        #region Launch - End

        static SkillResponse HandleLaunchRequest(LaunchRequest request, Session session)
        {            
            var reprompt = new Repr($"Ad esempio puoi dirmi, {BreakStrong} Crea una Function App. Oppure, {BreakStrong} Deploya il progetto Super Segreto.".ToSsmlSpeech());
            var response = ResponseBuilder.Ask("Ciao! Sono Aldo, il tuo aiuto DevOps.".ToSsmlSpeech(), reprompt);
            response.Response.ShouldEndSession = false;
            return response;
        }
        static SkillResponse HandleSessionEndedRequest(SessionEndedRequest request)
        {
            var response = ResponseBuilder.Empty();
            response.Response.ShouldEndSession = true;
            return response;
        }

        #endregion

        #region Help - Cancel - Stop

        static SkillResponse HandleHelpIntent(IntentRequest request, Session session)
        {
            var response = ResponseBuilder.Tell($"Ad esempio prova a dirmi {BreakStrong} Crea un App Service. Oppure, {BreakStrong} Fai il deploy del progetto Round.".ToSsmlSpeech());
            response.Response.ShouldEndSession = false;
            return response;
        }

        static SkillResponse HandleCancelIntent(IntentRequest request, Session session)
        {
            var reprompt = new Repr("Desideri ancora creare un servizio su Azure?");
            session.Attributes.Clear();
            var response = ResponseBuilder.Ask("OK, ricominciamo da capo.", reprompt, session);
            return response;
        }

        static SkillResponse HandleStopIntent(IntentRequest request, Session session)
        {
            var response = ResponseBuilder.Tell("OK, ci vediamo al prossimo deploy!");
            return response;
        }

        #endregion

        #region Set Resource Type

        static bool CanHandleSetResourceTypeIntent(IntentRequest request, Session session)
        {
            return request.Intent.Name == Intents.SetResourceTypeIntent;
        }

        static SkillResponse HandleSetResourceTypeIntent(IntentRequest request, Session session)
        {
            if (session.Attributes == null)
                session.Attributes = new Dictionary<string, object>();

            SkillResponse response;

            var slots = request.Intent.Slots;
            var arType = slots[Slots.AzureResourceType];            

            if (!arType.TryParseAzureResourceType(out var azureResourceType))
            {
                var reprompt = new Repr("Che tipo di risorsa desideri creare?");
                response = ResponseBuilder.Ask(("Non ho capito che tipo di risorsa vuoi creare. " +
                                               $"{BreakStrong} Devi specificare un servizio Azure valido. " +
                                               $"{BreakStrong} Per favore, {BreakStrong} puoi dirmelo di nuovo?").ToSsmlSpeech(), reprompt);
            }
            else
            {
                session.Attributes.Add(Slots.AzureResourceType, JsonConvert.SerializeObject(azureResourceType));
                session.Attributes["state"] = States.AskForResourceName;
                var reprompt = new Repr("Scusa, vuoi dare un nome alla nuova risorsa?");
                response = ResponseBuilder.Ask("Vuoi dare un nome alla tua nuova risorsa?", reprompt, session);           
            }

            return response;
        }

        #endregion

        #region Ask for Resource Name

        static bool CanHandleAskForResourceNameIntent(IntentRequest request, Session session)
        {
            return (request.Intent.Name == Intents.YesIntent || request.Intent.Name == Intents.NoIntent) &&
                   session.Attributes.ContainsValue(States.AskForResourceName);
        }

        static SkillResponse HandleAskForResourceNameIntent(IntentRequest request, Session session)
        {
            SkillResponse response;
            Reprompt reprompt;

            if (request.Intent.Name == Intents.YesIntent)
            {
                session.Attributes["state"] = States.SetResourceName;
                reprompt = new Repr("Quindi come la devo chiamare?");
                response = ResponseBuilder.Ask("Che nome vuoi dargli?", reprompt, session);             
            }
            else
            {
                var azureResourceType = JsonConvert.DeserializeObject<AzureResourceType>(session.Attributes[Slots.AzureResourceType].ToString());
                session.Attributes["state"] = States.CreateResource;
                reprompt = new Repr("Quindi confermi?");
                response = ResponseBuilder.Ask($"Sto per creare la risorsa {BreakMedium} {azureResourceType.Name}. Confermi?".ToSsmlSpeech(), reprompt, session);                
            }

            return response;
        }

        #endregion 

        #region Set Resource Name 

        static bool CanHandlSetResourceNameIntent(IntentRequest request, Session session)
        {
            return request.Intent.Name == Intents.SetResourceNameIntent && 
                   (session.Attributes.ContainsValue(States.SetResourceName) || request.Intent.Slots.ContainsKey(Slots.AzureResourceName));
        }

        static SkillResponse HandlSetResourceNameIntent(IntentRequest request, Session session)
        {
            var slots = request.Intent.Slots;
            var arName = slots[Slots.AzureResourceName].Value;
            var azureResourceType = JsonConvert.DeserializeObject<AzureResourceType>(session.Attributes[Slots.AzureResourceType].ToString());

            session.Attributes[Slots.AzureResourceName] = arName;
            session.Attributes["state"] = States.CreateResource;

            var reprompt = new Repr("Quindi confermi?");
            var response = ResponseBuilder.Ask($"Sto per creare la risorsa {BreakMedium} {azureResourceType.Name}, con il nome {arName}. Confermi?".ToSsmlSpeech(), reprompt, session);           
            return response;
        }

        #endregion

        #region Create Resource

        static bool CanHandleCreateResourceIntent(IntentRequest request, Session session)
        {
            return (request.Intent.Name == Intents.YesIntent || request.Intent.Name == Intents.NoIntent) && 
                   session.Attributes.ContainsValue(States.CreateResource);
        }

        static async Task<SkillResponse> HandleCreateResourceIntent(IntentRequest request, Session session,
            IAsyncCollector<AzureResourceToDeploy> resourceDeployQueue)
        {
            SkillResponse response;

            if (request.Intent.Name == Intents.YesIntent)
            {
                var azureResourceType = JsonConvert.DeserializeObject<AzureResourceType>(session.Attributes[Slots.AzureResourceType].ToString());
 
                if (azureResourceType.Id == AzureResourceTypes.Functions.Id)
                {
                    session.Attributes["state"] = States.AskForDeployProject;
                    var reprompt = new Repr("Desideri anche fare il deploy di un progetto?");
                    response = ResponseBuilder.Ask($"Bene! {BreakStrong} Vuoi anche fare il deploy di un progetto sulla tua nuova risorsa?".ToSsmlSpeech(), reprompt, session);
                }
                else
                {
                    response = await StartResourceCreation(session, resourceDeployQueue);
                }
            }
            else
            {
                session.Attributes.Clear();
                var reprompt = new Repr("Che tipo di risorsa desideri creare?");
                response = ResponseBuilder.Ask("Ah, forse allora ho capito male. Cosa desidere creare?", reprompt, session);                
            }

            return response;
        }

        #endregion

        #region Ask for deploy project

        static bool CanHandleAskForDeployProjectIntent(IntentRequest request, Session session)
        {
            return (request.Intent.Name == Intents.YesIntent || request.Intent.Name == Intents.NoIntent) &&
                   session.Attributes.ContainsValue(States.AskForDeployProject);
        }

        static async Task<SkillResponse> HandleAskForDeployProjectIntent(IntentRequest request, Session session,
            IAsyncCollector<AzureResourceToDeploy> resourceDeployQueue)
        {
            SkillResponse response;

            if (request.Intent.Name == Intents.YesIntent)
            {
                var reprompt = new Repr("Di quale progetto vuoi fare il deploy?");
                session.Attributes.Remove("state");
                response = ResponseBuilder.Ask($"Molto bene! {BreakStrong} Come si chiama il progetto?".ToSsmlSpeech(), reprompt, session);
            }
            else
            {
                response = await StartResourceCreation(session, resourceDeployQueue);
            }            

            return response;
        }

        #endregion 

        #region Ask for another resource

        static bool CanHandleAskForAnotherResourceIntent(IntentRequest request, Session session)
        {
            return (request.Intent.Name == Intents.YesIntent || request.Intent.Name == Intents.NoIntent) &&
                   session.Attributes.ContainsValue(States.AskForAnotherResource);
        }

        static SkillResponse HandleAskForAnotherResourceIntent(IntentRequest request, Session session)
        {
            SkillResponse response;

            session.Attributes.Clear();

            if (request.Intent.Name == Intents.YesIntent)
            {
                var reprompt = new Repr("Hai deciso che risorsa vuoi creare?");
                response = ResponseBuilder.Ask("Perfetto! Che tipo di risorsa vuoi creare ora?", reprompt, session);
            }
            else
                response = ResponseBuilder.Tell("OK, ci si vede al prossimo deploy!");       

            return response;
        }

        #endregion

        #region Deploy Project

        static bool CanHandleDeployProjectIntent(IntentRequest request, Session session)
        {
            return request.Intent.Name == Intents.DeployProjectIntent;
        }

        static async Task<SkillResponse> HandleDeployProjectIntent(IntentRequest request, Session session, 
            IAsyncCollector<ProjectToDeploy> projectDeployQueue,
            IAsyncCollector<AzureResourceToDeploy> resourceDeployQueue)
        {
            SkillResponse response;

            if (request.DialogState != States.DialogComplete)
            {
                //response = ResponseBuilder.DialogElicitSlot(new PlainTextOutputSpeech { Text = "Come si chiama il progetto?" }, Slots.ProjectName);
                response = ResponseBuilder.DialogDelegate(session);
            }
            //else if (request.DialogState == States.DialogComplete)
            else if(request.DialogState == States.DialogComplete && request.Intent.ConfirmationStatus == States.IntentConfirmed)
            {
                //Console.WriteLine(request.Intent.Slots[Slots.ProjectName].Dump());
                var projectSlot = request.Intent.Slots[Slots.ProjectName];
                var projectSlotValue = projectSlot.Resolution.Authorities.First().Values.First().Value;

                var projectToDeploy = new ProjectToDeploy
                {
                    Id = projectSlotValue.Id,
                    ProjectName = projectSlot.Value,
                    PipelineName = projectSlotValue.Name,
                    RequestedByUser = session.User.UserId
                };

                if (session.Attributes != null && session.Attributes.ContainsKey(Slots.AzureResourceType))
                {
                    var azureResourceType = JsonConvert.DeserializeObject<AzureResourceType>(session.Attributes[Slots.AzureResourceType].ToString());
                    var arName = session.Attributes.ContainsKey(Slots.AzureResourceName) ? (string)session.Attributes[Slots.AzureResourceName] : null;

                    var ard = new AzureResourceToDeploy
                    {
                        AzureResource = new AzureResource
                        {
                            Type = azureResourceType,
                            Name = arName
                        },
                        RequestedByUser = session.User.UserId,
                        Project = projectToDeploy
                    };
                    ard.Project.FromNewResource = true;

                    var message = ($"OK, creo la risorsa {Break} {arName ?? azureResourceType.Name} " +
                                  $"e deployo il progetto {Break} {projectSlot.Value}. {Notify}").ToSsmlSpeech();

                    response = ResponseBuilder.Tell(message);
                    await resourceDeployQueue.AddAsync(ard);
                }
                else
                {
                    response = ResponseBuilder.Tell($"OK, deployo il progetto {BreakMedium} {projectSlot.Value}. {Notify}".ToSsmlSpeech());
                    await projectDeployQueue.AddAsync(projectToDeploy);
                }                                
            }
            else
            {
                var reprompt = new Repr("Dimmi come si chiama il progetto?");
                response = ResponseBuilder.Ask("Mmm forse non ho capito. Come si chiama il progetto?", reprompt, session);
            }

            return response;
        }

        #endregion 

        #region Unhandled 

        static SkillResponse HandleUnhandled(IntentRequest request)
        {
            var reprompt = new Repr("Che tipo di risorsa desideri creare?");
            var response = ResponseBuilder.Ask("Non ho capito che tipo di risorsa vuoi creare. Per favore, dimmelo di nuovo.".ToSsmlSpeech(), reprompt);
            return response;
        }

        #endregion

        #region Commons

        static async Task<SkillResponse> StartResourceCreation(Session session, IAsyncCollector<AzureResourceToDeploy> resourceDeployQueue)
        {
            var azureResourceType = JsonConvert.DeserializeObject<AzureResourceType>(session.Attributes[Slots.AzureResourceType].ToString());
            var arName = session.Attributes.ContainsKey(Slots.AzureResourceName) ? (string)session.Attributes[Slots.AzureResourceName] : null;

            session.Attributes["state"] = States.AskForAnotherResource;

            var reprompt = new Repr("Vuoi creare un altro servizio?");

            var message = arName is null ? $"OK, creo la risorsa {Break} {azureResourceType.Name}. {BreakStrong} {Notify}".ToSsmlSpeech()
                : $"OK, creo la risorsa {Break} {arName}. {BreakStrong} {Notify}".ToSsmlSpeech();

            var response = ResponseBuilder.Ask(message, reprompt, session);

            var azureResourceToDeploy = new AzureResourceToDeploy
            {
                AzureResource = new AzureResource
                {
                    Type = azureResourceType,
                    Name = arName
                },
                RequestedByUser = session.User.UserId
            };

            await resourceDeployQueue.AddAsync(azureResourceToDeploy);
            return response;
        }

        #endregion 

        #endregion 
    }
}
