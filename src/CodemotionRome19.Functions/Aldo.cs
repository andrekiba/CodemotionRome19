using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Alexa.NET;
using Alexa.NET.LocaleSpeech;
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
        #region Field

        readonly IConfiguration configuration;
        readonly ILogger log;
        ILSpeech locale;

        #endregion 

        public Aldo(IConfiguration configuration)
        {
            this.configuration = configuration;

            log = new LoggerConfiguration()
                .WriteTo.AzureTableStorage(configuration.GetValue("AzureWebJobsStorage"), storageTableName: $"{nameof(Aldo)}Log")
                .CreateLogger();
        }

        [FunctionName("Aldo")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)]HttpRequest req,
            [Queue("azure-resource-deploy", Connection = "AzureWebJobsStorage")] IAsyncCollector<AzureResourceToDeploy> resourceDeployQueue,
            [Queue("project-deploy", Connection = "AzureWebJobsStorage")] IAsyncCollector<ProjectToDeploy> projectDeployQueue,
            [Queue("project-build", Connection = "AzureWebJobsStorage")] IAsyncCollector<ProjectToBuild> projectBuildQueue)
        {
            SkillResponse response;

            try
            {
                var json = await req.ReadAsStringAsync();
                var skillRequest = JsonConvert.DeserializeObject<SkillRequest>(json);

                // Verifies that the request is coming from Alexa.
                var isValid = await skillRequest.ValidateRequest(req, log);
                if (!isValid)
                    return new BadRequestResult();

                var store = CreateLocaleStore();
                locale = skillRequest.CreateLocale(store);

                var session = skillRequest.Session;            
                var request = skillRequest.Request;

                response = await ProcessRequestPipeline(request, session, resourceDeployQueue, projectDeployQueue, projectBuildQueue);
            }
            catch (Exception e)
            {
                var error = $"{e.Message}\n\r{e.StackTrace}";
                log.Error(error);
                response = ResponseBuilder.Tell(locale.Get(L.Error));
                response.Response.ShouldEndSession = false;
            }

            return new OkObjectResult(response);
        }

        #region Methods

        async Task<SkillResponse> ProcessRequestPipeline(Request request, Session session,
                IAsyncCollector<AzureResourceToDeploy> resourceDeployQueue,
                IAsyncCollector<ProjectToDeploy> projectDeployQueue,
                IAsyncCollector<ProjectToBuild> projectBuildQueue)
        {
            SkillResponse response = null;

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
                //set Resource type
                case IntentRequest intentRequest when CanHandleSetResourceTypeIntent(intentRequest, session):
                    response = HandleSetResourceTypeIntent(intentRequest, session);
                    break;
                //ask for Resource name
                case IntentRequest intentRequest when CanHandleAskForResourceNameIntent(intentRequest, session):
                    response = HandleAskForResourceNameIntent(intentRequest, session);
                    break;
                //set Resource name
                case IntentRequest intentRequest when CanHandlSetResourceNameIntent(intentRequest, session):
                    response = HandlSetResourceNameIntent(intentRequest, session);
                    break;
                //create Resource
                case IntentRequest intentRequest when CanHandleCreateResourceIntent(intentRequest, session):
                    response = await HandleCreateResourceIntent(intentRequest, session, resourceDeployQueue);
                    break;
                //another Resource
                case IntentRequest intentRequest when CanHandleAskForAnotherResourceIntent(intentRequest, session):
                    response = HandleAskForAnotherResourceIntent(intentRequest, session);
                    break;
                //Deploy on Resource
                case IntentRequest intentRequest when CanHandleAskForDeployProjectIntent(intentRequest, session):
                    response = await HandleAskForDeployProjectIntent(intentRequest, session, resourceDeployQueue);
                    break;
                //Deploy
                case IntentRequest intentRequest when CanHandleDeployProjectIntent(intentRequest, session):
                    response = await HandleDeployProjectIntent(intentRequest, session, projectDeployQueue, resourceDeployQueue);
                    break;
                //another Deploy
                case IntentRequest intentRequest when CanHandleAskForAnotherDeployIntent(intentRequest, session):
                    response = HandleAskForAnotherDeployIntent(intentRequest, session);
                    break;
                //Build
                case IntentRequest intentRequest when CanHandleBuildProjectIntent(intentRequest, session):
                    response = await HandleBuildProjectIntent(intentRequest, session, projectBuildQueue);
                    break;
                //another Build
                case IntentRequest intentRequest when CanHandleAskForAnotherBuildIntent(intentRequest, session):
                    response = HandleAskForAnotherBuildIntent(intentRequest, session);
                    break;
                //Unhandled
                case IntentRequest intentRequest:
                    response = HandleUnhandled(intentRequest);
                    break;
                //End
                case SessionEndedRequest sessionEndedRequest:
                    response = HandleSessionEndedRequest(sessionEndedRequest);
                    break;
            }

            return response;
        }

        #region Launch - End

        SkillResponse HandleLaunchRequest(LaunchRequest request, Session session)
        {
            var reprompt = new Repr(locale.Get(L.WelcomeRepr));
            var response = ResponseBuilder.Ask(locale.Get(L.Welcome), reprompt);
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

        SkillResponse HandleHelpIntent(IntentRequest request, Session session)
        {
            var response = ResponseBuilder.Tell(locale.Get(L.Help));
            response.Response.ShouldEndSession = false;
            return response;
        }

        SkillResponse HandleCancelIntent(IntentRequest request, Session session)
        {
            var reprompt = new Repr(locale.Get(L.CancelRepr));
            session.Attributes.Clear();
            var response = ResponseBuilder.Ask(locale.Get(L.Cancel), reprompt, session);
            return response;
        }

        SkillResponse HandleStopIntent(IntentRequest request, Session session)
        {
            var response = ResponseBuilder.Tell(locale.Get(L.Stop));
            return response;
        }

        #endregion

        #region Set Resource Type

        static bool CanHandleSetResourceTypeIntent(IntentRequest request, Session session) => 
            request.Intent.Name == Intents.SetResourceTypeIntent;

        SkillResponse HandleSetResourceTypeIntent(IntentRequest request, Session session)
        {
            if (session.Attributes == null)
                session.Attributes = new Dictionary<string, object>();

            SkillResponse response;

            var slots = request.Intent.Slots;
            var arType = slots[Slots.AzureResourceType];            

            if (!arType.TryParseAzureResourceType(out var azureResourceType))
            {
                var reprompt = new Repr(locale.Get(L.ResourceTypeErrorRepr));
                response = ResponseBuilder.Ask(locale.Get(L.ResourceTypeError), reprompt);
            }
            else
            {
                session.Attributes.Add(Slots.AzureResourceType, JsonConvert.SerializeObject(azureResourceType));
                session.Attributes["state"] = States.AskForResourceName;
                var reprompt = new Repr(locale.Get(L.AskForResourceNameRepr));
                response = ResponseBuilder.Ask(locale.Get(L.AskForResourceName), reprompt, session);           
            }

            return response;
        }

        #endregion

        #region Ask for Resource Name

        static bool CanHandleAskForResourceNameIntent(IntentRequest request, Session session) => 
            (request.Intent.Name == Intents.YesIntent || request.Intent.Name == Intents.NoIntent) &&
            session.Attributes.ContainsValue(States.AskForResourceName);

        SkillResponse HandleAskForResourceNameIntent(IntentRequest request, Session session)
        {
            SkillResponse response;
            Reprompt reprompt;

            if (request.Intent.Name == Intents.YesIntent)
            {
                session.Attributes["state"] = States.SetResourceName;
                reprompt = new Repr(locale.Get(L.WichResourceNameRepr));
                response = ResponseBuilder.Ask(locale.Get(L.WichResourceName), reprompt, session);             
            }
            else
            {
                var azureResourceType = JsonConvert.DeserializeObject<AzureResourceType>(session.Attributes[Slots.AzureResourceType].ToString());
                session.Attributes["state"] = States.CreateResource;
                reprompt = new Repr(locale.Get(L.ConfirmRepr));
                response = ResponseBuilder.Ask(locale.Get(L.ConfirmCreateResource, azureResourceType.Name), reprompt, session);                
            }

            return response;
        }

        #endregion 

        #region Set Resource Name 

        static bool CanHandlSetResourceNameIntent(IntentRequest request, Session session) => 
            request.Intent.Name == Intents.SetResourceNameIntent && 
            (session.Attributes.ContainsValue(States.SetResourceName) || request.Intent.Slots.ContainsKey(Slots.AzureResourceName));

        SkillResponse HandlSetResourceNameIntent(IntentRequest request, Session session)
        {
            var slots = request.Intent.Slots;
            var arName = slots[Slots.AzureResourceName].Value;
            var azureResourceType = JsonConvert.DeserializeObject<AzureResourceType>(session.Attributes[Slots.AzureResourceType].ToString());

            session.Attributes[Slots.AzureResourceName] = arName;
            session.Attributes["state"] = States.CreateResource;

            var reprompt = new Repr(locale.Get(L.ConfirmRepr));
            var response = ResponseBuilder.Ask(locale.Get(L.ConfirmCreateResourceWithName, azureResourceType.Name, arName), reprompt, session);           
            return response;
        }

        #endregion

        #region Create Resource

        static bool CanHandleCreateResourceIntent(IntentRequest request, Session session) => 
            (request.Intent.Name == Intents.YesIntent || request.Intent.Name == Intents.NoIntent) && 
            session.Attributes.ContainsValue(States.CreateResource);

        async Task<SkillResponse> HandleCreateResourceIntent(IntentRequest request, Session session,
            IAsyncCollector<AzureResourceToDeploy> resourceDeployQueue)
        {
            SkillResponse response;

            if (request.Intent.Name == Intents.YesIntent)
            {
                var azureResourceType = JsonConvert.DeserializeObject<AzureResourceType>(session.Attributes[Slots.AzureResourceType].ToString());
 
                if (azureResourceType.Id == AzureResourceTypes.Functions.Id)
                {
                    session.Attributes["state"] = States.AskForDeployProject;
                    var reprompt = new Repr(locale.Get(L.AlsoDeployProjectRepr));
                    response = ResponseBuilder.Ask(locale.Get(L.AlsoDeployProject), reprompt, session);
                }
                else
                {
                    response = await StartResourceCreation(session, resourceDeployQueue);
                }
            }
            else
            {
                session.Attributes.Clear();
                var reprompt = new Repr(locale.Get(L.CreateResourceMisunderstoodRepr));
                response = ResponseBuilder.Ask(locale.Get(L.CreateResourceMisunderstood), reprompt, session);                
            }

            return response;
        }

        #endregion

        #region Ask for deploy project

        static bool CanHandleAskForDeployProjectIntent(IntentRequest request, Session session) => 
            (request.Intent.Name == Intents.YesIntent || request.Intent.Name == Intents.NoIntent) &&
            session.Attributes.ContainsValue(States.AskForDeployProject);

        async Task<SkillResponse> HandleAskForDeployProjectIntent(IntentRequest request, Session session,
            IAsyncCollector<AzureResourceToDeploy> resourceDeployQueue)
        {
            SkillResponse response;

            if (request.Intent.Name == Intents.YesIntent)
            {
                var reprompt = new Repr(locale.Get(L.WichProjectRepr));
                session.Attributes.Remove("state");
                response = ResponseBuilder.Ask(locale.Get(L.WichProject), reprompt, session);
            }
            else
            {
                response = await StartResourceCreation(session, resourceDeployQueue);
            }            

            return response;
        }

        #endregion 

        #region Deploy Project

        static bool CanHandleDeployProjectIntent(IntentRequest request, Session session) => 
            request.Intent.Name == Intents.DeployProjectIntent;

        async Task<SkillResponse> HandleDeployProjectIntent(IntentRequest request, Session session, 
            IAsyncCollector<ProjectToDeploy> projectDeployQueue,
            IAsyncCollector<AzureResourceToDeploy> resourceDeployQueue)
        {
            if (session.Attributes == null)
                session.Attributes = new Dictionary<string, object>();

            SkillResponse response;

            if (request.DialogState != States.DialogComplete)
            {
                //response = ResponseBuilder.DialogElicitSlot(new PlainTextOutputSpeech { Text = "Come si chiama il progetto?" }, Slots.ProjectName);
                response = ResponseBuilder.DialogDelegate(session);
            }
            else if(request.DialogState == States.DialogComplete && request.Intent.ConfirmationStatus == States.IntentConfirmed)
            {
                var projectSlot = request.Intent.Slots[Slots.ProjectName];
                var projectSlotValue = projectSlot.Resolution.Authorities.First().Values.First().Value;

                var projectToDeploy = new ProjectToDeploy
                {
                    Id = projectSlotValue.Id,
                    ProjectName = projectSlot.Value,
                    PipelineName = projectSlotValue.Name,
                    RequestedByUser = session.User.UserId
                };

                if (session.Attributes.ContainsKey(Slots.AzureResourceType))
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

                    session.Attributes["state"] = States.AskForAnotherResource;
                    var reprompt = new Repr(locale.Get(L.AskForAnotherResourceRepr));

                    response = ResponseBuilder.Ask(locale.Get(L.CreateResourceAndDeploy, arName ?? azureResourceType.Name, projectSlot.Value), reprompt, session);
                    await resourceDeployQueue.AddAsync(ard);
                }
                else
                {
                    session.Attributes["state"] = States.AskForAnotherDeploy;
                    var reprompt = new Repr(locale.Get(L.AskForAnotherDeployRepr));
                    response = ResponseBuilder.Ask(locale.Get(L.DeployProject, projectSlot.Value), reprompt, session);

                    await projectDeployQueue.AddAsync(projectToDeploy);
                }                                
            }
            else
            {
                var reprompt = new Repr(locale.Get(L.ProjectMisunderstoodRepr));
                response = ResponseBuilder.Ask(locale.Get(L.ProjectMisunderstood), reprompt, session);
            }

            return response;
        }

        #endregion

        #region Build Project

        static bool CanHandleBuildProjectIntent(IntentRequest request, Session session) => 
            request.Intent.Name == Intents.BuildProjectIntent;

        async Task<SkillResponse> HandleBuildProjectIntent(IntentRequest request, Session session,
            IAsyncCollector<ProjectToBuild> projectBuildQueue)
        {
            if (session.Attributes == null)
                session.Attributes = new Dictionary<string, object>();

            SkillResponse response;

            if (request.DialogState != States.DialogComplete)
            {
                //response = ResponseBuilder.DialogElicitSlot(new PlainTextOutputSpeech { Text = "Come si chiama il progetto?" }, Slots.ProjectName);
                response = ResponseBuilder.DialogDelegate(session);
            }
            else if (request.DialogState == States.DialogComplete && request.Intent.ConfirmationStatus == States.IntentConfirmed)
            {
                var projectSlot = request.Intent.Slots[Slots.ProjectName];
                var projectSlotValue = projectSlot.Resolution.Authorities.First().Values.First().Value;

                var projectToBuild = new ProjectToBuild
                {
                    Id = projectSlotValue.Id,
                    ProjectName = projectSlot.Value,
                    PipelineName = projectSlotValue.Name,
                    RequestedByUser = session.User.UserId
                };

                session.Attributes["state"] = States.AskForAnotherBuild;
                var reprompt = new Repr(locale.Get(L.AskForAnotherBuildRepr));
                response = ResponseBuilder.Ask(locale.Get(L.BuildProject, projectSlot.Value), reprompt, session);
                await projectBuildQueue.AddAsync(projectToBuild);                
            }
            else
            {
                var reprompt = new Repr(locale.Get(L.ProjectMisunderstoodRepr));
                response = ResponseBuilder.Ask(locale.Get(L.ProjectMisunderstood), reprompt, session);
            }

            return response;
        }

        #endregion 

        #region Ask for another resource

        static bool CanHandleAskForAnotherResourceIntent(IntentRequest request, Session session) => 
            (request.Intent.Name == Intents.YesIntent || request.Intent.Name == Intents.NoIntent) &&
            session.Attributes.ContainsValue(States.AskForAnotherResource);

        SkillResponse HandleAskForAnotherResourceIntent(IntentRequest request, Session session)
        {
            SkillResponse response;

            session.Attributes.Clear();

            if (request.Intent.Name == Intents.YesIntent)
            {
                var reprompt = new Repr(locale.Get(L.AnotherResourceRepr));
                response = ResponseBuilder.Ask(locale.Get(L.AnotherResource), reprompt, session);
            }
            else
                response = ResponseBuilder.Tell(S.NextTime);

            return response;
        }

        #endregion

        #region Ask for another deploy

        static bool CanHandleAskForAnotherDeployIntent(IntentRequest request, Session session) => 
            (request.Intent.Name == Intents.YesIntent || request.Intent.Name == Intents.NoIntent) &&
            session.Attributes.ContainsValue(States.AskForAnotherDeploy);

        SkillResponse HandleAskForAnotherDeployIntent(IntentRequest request, Session session)
        {
            SkillResponse response;

            session.Attributes.Clear();

            if (request.Intent.Name == Intents.YesIntent)
            {
                var reprompt = new Repr(locale.Get(L.AnotherDeployRepr));
                response = ResponseBuilder.Ask(locale.Get(L.AnotherDeploy), reprompt, session);
            }
            else
                response = ResponseBuilder.Tell(S.NextTime);

            return response;
        }

        #endregion

        #region Ask for another build

        static bool CanHandleAskForAnotherBuildIntent(IntentRequest request, Session session) => 
            (request.Intent.Name == Intents.YesIntent || request.Intent.Name == Intents.NoIntent) &&
            session.Attributes.ContainsValue(States.AskForAnotherBuild);

        SkillResponse HandleAskForAnotherBuildIntent(IntentRequest request, Session session)
        {
            SkillResponse response;

            session.Attributes.Clear();

            if (request.Intent.Name == Intents.YesIntent)
            {
                var reprompt = new Repr(locale.Get(L.AnotherBuildRepr));
                response = ResponseBuilder.Ask(locale.Get(L.AnotherBuild), reprompt, session);
            }
            else
                response = ResponseBuilder.Tell(S.NextTime);

            return response;
        }

        #endregion

        #region Unhandled 

        SkillResponse HandleUnhandled(IntentRequest request)
        {
            var reprompt = new Repr(locale.Get(L.UnhandledRepr));
            var response = ResponseBuilder.Ask(locale.Get(L.Unhandled), reprompt);
            return response;
        }

        #endregion

        #region Commons

        async Task<SkillResponse> StartResourceCreation(Session session, IAsyncCollector<AzureResourceToDeploy> resourceDeployQueue)
        {
            var azureResourceType = JsonConvert.DeserializeObject<AzureResourceType>(session.Attributes[Slots.AzureResourceType].ToString());
            var arName = session.Attributes.ContainsKey(Slots.AzureResourceName) ? (string)session.Attributes[Slots.AzureResourceName] : null;

            session.Attributes["state"] = States.AskForAnotherResource;

            var reprompt = new Repr(locale.Get(L.AskForAnotherResourceRepr));

            var response = ResponseBuilder.Ask(locale.Get(L.CreateResource, arName ?? azureResourceType.Name), reprompt, session);

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

        #region Language

        static DictionaryLSpeechStore CreateLocaleStore()
        {
            // Creates the locale speech store for each supported languages.
            var store = new DictionaryLSpeechStore();

            store.AddLanguage("en", new Dictionary<string, object>
            {
                [L.Welcome] = "Ciao! I'm Aldo, your DevOps help.",
                [L.WelcomeRepr] = $"For example, you can tell me, {S.BreakStrong} Create a Function App. Or, {S.BreakStrong} Deploy the Super Secret project.".ToSsmlSpeech(),
                [L.Cancel] = "OK, let's start again.",
                [L.CancelRepr] = "Do you still want to create a service on Azure or deploy a project?",
                [L.Help] = $"For example, try to tell me {S.BreakStrong} Create an App Service. Or, {S.BreakStrong} Buil Round project.".ToSsmlSpeech(),
                [L.Stop] = "OK, see you at the next deploy!",
                [L.Error] = "I'm sorry, there was an unexpected error. Please try again later.",
                [L.ResourceTypeError] = ("I didn't understand what kind of resource you want to create. " +
                                         $"{S.BreakStrong} You must specify a valid Azure service. " +
                                         $"{S.BreakStrong} Please, {S.BreakStrong} can you tell me again?").ToSsmlSpeech(),
                [L.ResourceTypeErrorRepr] = "Which kind of resource?",
                [L.AskForResourceName] = "Do you want to give it a name?",
                [L.AskForResourceNameRepr] = "Sorry, do you want to name the new resource?",
                [L.WichResourceName] = "What name?",
                [L.WichResourceNameRepr] = "How should I call it?",
                [L.ConfirmCreateResource] = $"I'm about to create the resource {S.BreakMedium} {{0}}. It's correct?".ToSsmlSpeech(),
                [L.ConfirmRepr] = "So do you confirm?",
                [L.ConfirmCreateResourceWithName] = $"I'm about to create the resource {S.BreakMedium} {{0}}, with name {{1}}. It's correct?".ToSsmlSpeech(),
                [L.AlsoDeployProjectRepr] = "Do you also want to deploy a project?",
                [L.AlsoDeployProject] = $"Cool! {S.BreakStrong} Do you also want to deploy a project on your new resource?".ToSsmlSpeech(),
                [L.CreateResourceMisunderstood] = "Ah, maybe then I misunderstood. What do you want to create?",
                [L.CreateResourceMisunderstoodRepr] = "What kind of resource do you want to create?",
                [L.WichProject] = $"All right! {S.BreakStrong} What is the name of the project?".ToSsmlSpeech(),
                [L.WichProjectRepr] = "What project do you want to deploy?",
                [L.ProjectMisunderstood] = "Mmm maybe I didn't understand. What is the name of the project?",
                [L.ProjectMisunderstoodRepr] = "What is the name of the project?",
                [L.CreateResourceAndDeploy] = ($"OK, I create the resource {S.Break} {{0}} and deploy the project {S.Break} {{1}}. {S.Notify}").ToSsmlSpeech(),
                [L.AskForAnotherResourceRepr] = "Do you want to create another resource on Azure?",
                [L.AskForAnotherDeployRepr] = "Do you want to deploy another project?",
                [L.AskForAnotherBuildRepr] = "Do you want to build another project?",
                [L.AnotherResourceRepr] = "What resource you want to create?",
                [L.AnotherResource] = "Perfect! What kind of resource do you want to create now?",
                [L.AnotherDeployRepr] = "Can you tell me the name of the project?",
                [L.AnotherDeploy] = "Perfect! What project do you want to deploy now?",
                [L.AnotherBuildRepr] = "Can you tell me the name of the project?",
                [L.AnotherBuild] = "Perfect! Which project do you want to build now?",
                [L.DeployProject] = $"OK, I deploy the project {S.BreakMedium} {{0}}. {S.Notify}".ToSsmlSpeech(),
                [L.BuildProject] = $"OK, I build the project {S.BreakMedium} {{0}}. {S.Notify}".ToSsmlSpeech(),
                [L.Unhandled] = $"Sorry but I didn't understand what you asked me. {S.BreakStrong} Please tell me again.".ToSsmlSpeech(),
                [L.UnhandledRepr] = "If you need help try saying Help",
                [L.CreateResource] = $"OK, I create the resource {S.Break} {{0}}. {S.BreakStrong} {S.Notify}".ToSsmlSpeech()

            });

            store.AddLanguage("it", new Dictionary<string, object>
            {
                [L.Welcome] = "Ciao! Sono Aldo, il tuo aiuto DevOps.",
                [L.WelcomeRepr] = $"Ad esempio puoi dirmi, {S.BreakStrong} Crea una Function App. Oppure, {S.BreakStrong} Deploya il progetto Super Segreto.".ToSsmlSpeech(),
                [L.Cancel] = "OK, ricominciamo da capo.",
                [L.CancelRepr] = "Desideri ancora creare un servizio su Azure o fare il deploy di un progetto?",
                [L.Help] = $"Ad esempio prova a dirmi {S.BreakStrong} Crea un App Service. Oppure, {S.BreakStrong} Fai la build del progetto Round.".ToSsmlSpeech(),
                [L.Stop] = "OK, ci vediamo al prossimo deploy!",
                [L.Error] = "Mi dispiace, c'è stato un errore inatteso. Per favore, riprova più tardi.",
                [L.ResourceTypeError] = ("Non ho capito che tipo di risorsa vuoi creare. " +
                                         $"{S.BreakStrong} Devi specificare un servizio Azure valido. " +
                                         $"{S.BreakStrong} Per favore, {S.BreakStrong} puoi dirmelo di nuovo?").ToSsmlSpeech(),
                [L.ResourceTypeErrorRepr] = "Che tipo di risorsa desideri creare?",
                [L.AskForResourceName] = "Vuoi dare un nome alla tua nuova risorsa?",
                [L.AskForResourceNameRepr] = "Scusa, vuoi dare un nome alla nuova risorsa?",
                [L.WichResourceName] = "Che nome vuoi dargli?",
                [L.WichResourceNameRepr] = "Come la devo chiamare?",
                [L.ConfirmCreateResource] = $"Sto per creare la risorsa {S.BreakMedium} {{0}}. Confermi?".ToSsmlSpeech(),
                [L.ConfirmRepr] = "Quindi confermi?",
                [L.ConfirmCreateResourceWithName] = $"Sto per creare la risorsa {S.BreakMedium} {{0}}, con il nome {{1}}. Confermi?".ToSsmlSpeech(),
                [L.AlsoDeployProjectRepr] = "Desideri anche fare il deploy di un progetto?",
                [L.AlsoDeployProject] = $"Bene! {S.BreakStrong} Vuoi anche fare il deploy di un progetto sulla tua nuova risorsa?".ToSsmlSpeech(),
                [L.CreateResourceMisunderstood] = "Ah, forse allora ho capito male. Cosa desidere creare?",
                [L.CreateResourceMisunderstoodRepr] = "Che tipo di risorsa desideri creare?",
                [L.WichProject] = $"Molto bene! {S.BreakStrong} Come si chiama il progetto?".ToSsmlSpeech(),
                [L.WichProjectRepr] = "Di quale progetto vuoi fare il deploy?",
                [L.ProjectMisunderstood] = "Mmm forse non ho capito. Come si chiama il progetto?",
                [L.ProjectMisunderstoodRepr] = "Come si chiama il progetto?",
                [L.CreateResourceAndDeploy] = ($"OK, creo la risorsa {S.Break} {{0}} e deployo il progetto {S.Break} {{1}}. {S.Notify}").ToSsmlSpeech(),
                [L.AskForAnotherResourceRepr] = "Vuoi creare un'altra risorsa su Azure?",
                [L.AskForAnotherDeployRepr] = "Vuoi deployare un altro progetto?",
                [L.AskForAnotherBuildRepr] = "Vuoi fare la build di un altro progetto?",
                [L.AnotherResourceRepr] = "Hai deciso che risorsa vuoi creare?",
                [L.AnotherResource] = "Perfetto! Che tipo di risorsa vuoi creare ora?",
                [L.AnotherDeployRepr] = "Mi dici il nome del progetto?",
                [L.AnotherDeploy] = "Perfetto! Quale progetto vuoi deployare ora?",
                [L.AnotherBuildRepr] = "Mi dici il nome del progetto?",
                [L.AnotherBuild] = "Perfetto! Di quale progetto vuoi fare la build ora?",
                [L.DeployProject] = $"OK, deployo il progetto {S.BreakMedium} {{0}}. {S.Notify}".ToSsmlSpeech(),
                [L.BuildProject] = $"OK, faccio la build del progetto {S.BreakMedium} {{0}}. {S.Notify}".ToSsmlSpeech(),
                [L.Unhandled] = $"Scusa ma non ho capito cosa mi hai chiesto. {S.BreakStrong} Per favore, dimmelo di nuovo.".ToSsmlSpeech(),
                [L.UnhandledRepr] = "Se hai bisogno di aiuto prova a dire Aiuto",
                [L.CreateResource] = $"OK, creo la risorsa {S.Break} {{0}}. {S.BreakStrong} {S.Notify}".ToSsmlSpeech()

            });

            return store;
        }

        #endregion 

        #endregion
    }
}
