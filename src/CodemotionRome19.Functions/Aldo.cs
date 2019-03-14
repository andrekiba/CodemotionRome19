using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Alexa.NET;
using Alexa.NET.Request;
using Alexa.NET.Request.Type;
using Alexa.NET.Response;
using CodemotionRome19.Core.Azure;
using CodemotionRome19.Core.Azure.Deployment;
using CodemotionRome19.Core.Configuration;
using CodemotionRome19.Functions.Alexa;
using CodemotionRome19.Functions.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Serilog;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace CodemotionRome19.Functions
{
    public class Aldo
    {
        readonly IConfiguration configuration;
        readonly IAzureService azureService;
        readonly IDeploymentService deploymentService;

        public Aldo(IConfiguration configuration, IAzureService azureService, IDeploymentService deploymentService)
        {
            this.configuration = configuration;
            this.azureService = azureService;
            this.deploymentService = deploymentService;
        }

        [FunctionName("Aldo")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)]HttpRequest req,
            [Queue("azure-resource-deploy", Connection = "AzureWebJobsStorage")] IAsyncCollector<AzureResourceToDeploy> deployQueue,
            ILogger log)
        {
            var connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage", EnvironmentVariableTarget.Process);
            //var tableName = Environment.GetEnvironmentVariable("deployLog", EnvironmentVariableTarget.Process);
            var serilog = new LoggerConfiguration()
                .WriteTo.AzureTableStorage(connectionString, storageTableName: "AldoLog")
                .CreateLogger();

            var json = await req.ReadAsStringAsync();
            var skillRequest = JsonConvert.DeserializeObject<SkillRequest>(json);

            // Verifies that the request is indeed coming from Alexa.
            var isValid = await skillRequest.ValidateRequest(req, log);
            if (!isValid)
            {
                return new BadRequestResult();
            }

            var session = skillRequest.Session;            
            var request = skillRequest.Request;
            SkillResponse response = null;

            try
            {
                switch (request)
                {
                    case LaunchRequest launchRequest:
                        response = HandleLaunchRequest(launchRequest, session, log);
                        break;
                    case IntentRequest intentRequest when intentRequest.Intent.Name == Intents.HelpIntent:
                        response = HandleHelpIntent(intentRequest, session, log);
                        break;
                    case IntentRequest intentRequest when intentRequest.Intent.Name == Intents.CancelIntent:
                        response = HandleCancelIntent(intentRequest, session, log);
                        break;
                    case IntentRequest intentRequest when intentRequest.Intent.Name == Intents.StopIntent:
                        response = HandleStopIntent(intentRequest, session, log);
                        break;
                    case IntentRequest intentRequest when CanHandleSetResourceTypeIntent(intentRequest, session):
                        response = HandleSetResourceTypeIntent(intentRequest, session, log);
                        break;
                    case IntentRequest intentRequest when CanHandleAskForResourceNameIntent(intentRequest, session):
                        response = HandleAskForResourceNameIntent(intentRequest, session, log);
                        break;
                    case IntentRequest intentRequest when CanHandlSetResourceNameIntent(intentRequest, session):
                        response = HandlSetResourceNameIntent(intentRequest, session, log);
                        break;
                    case IntentRequest intentRequest when CanHandleCreateResourceIntent(intentRequest, session):
                        response = await HandleCreateResourceIntent(intentRequest, session, deployQueue, log);
                        break;
                    case IntentRequest intentRequest: //Unhandled
                        response = HandleUnhandled(intentRequest);
                        break;
                    case SessionEndedRequest sessionEndedRequest:
                        response = HandleSessionEndedRequest(sessionEndedRequest, log);
                        break;
                }
            }
            catch (Exception e)
            {
                var error = $"{e.Message}\n\r{e.StackTrace}";
                log.LogError(error);
                //response = ResponseBuilder.Tell("Purtroppo non riesco a fare il deploy della risorsa richiesta.");
                response = ResponseBuilder.Tell("Mi dispiace, c'è stato un errore inatteso. Per favore, riprova più tardi.");
            }

            return new OkObjectResult(response);
        }

        #region Launch - End

        static SkillResponse HandleLaunchRequest(LaunchRequest request, Session session, ILogger log)
        {
            log.LogInformation("Session started");

            var reprompt = new Reprompt
            {
                OutputSpeech = new PlainTextOutputSpeech
                {
                    Text = "Ad esempio puoi dirmi 'Crea una function app. Oppure, deploya un databse SQL.'"
                }
            };

            var response = ResponseBuilder.Ask("Ciao! Sono Aldo. Posso aiutarti a creare servizi su Azure.", reprompt);
            response.Response.ShouldEndSession = false;

            return response;
        }
        static SkillResponse HandleSessionEndedRequest(SessionEndedRequest request, ILogger log)
        {
            log.LogInformation("Session ended");
            var response = ResponseBuilder.Empty();
            response.Response.ShouldEndSession = true;

            return response;
        }

        #endregion

        #region Help - Cancel - Stop

        static SkillResponse HandleHelpIntent(IntentRequest request, Session session, ILogger log)
        {
            var response = ResponseBuilder.Tell("Ad esempio prova a dirmi 'Crea un App Service. Oppure, deploya CosmosDB'.");
            response.Response.ShouldEndSession = false;
            return response;
        }

        static SkillResponse HandleCancelIntent(IntentRequest request, Session session, ILogger log)
        {
            var reprompt = new Reprompt
            {
                OutputSpeech = new PlainTextOutputSpeech
                {
                    Text = "Desideri ancora creare un servizio Azure?"
                }
            };
            var response = ResponseBuilder.Ask("OK, ricominciamo da capo.", reprompt);
            return response;
        }

        static SkillResponse HandleStopIntent(IntentRequest request, Session session, ILogger log)
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

        static SkillResponse HandleSetResourceTypeIntent(IntentRequest request, Session session, ILogger log)
        {
            if (session.Attributes == null)
                session.Attributes = new Dictionary<string, object>();

            SkillResponse response;

            var slots = request.Intent.Slots;
            var arType = slots[Slots.AzureResourceType];            

            if (!arType.TryParseAzureResourceType(out var azureResourceType, log))
            {
                var reprompt = new Reprompt
                {
                    OutputSpeech = new PlainTextOutputSpeech
                    {
                        Text = "Che tipo di risorsa desideri creare?"
                    }
                };

                response = ResponseBuilder.Ask("Non ho capito che tipo di risorsa vuoi creare, devi specificare un servizio Azure valido. Dimmelo di nuovo per favor.", reprompt);
            }
            else
            {
                var reprompt = new Reprompt { OutputSpeech = new PlainTextOutputSpeech { Text = "Scusa, vuoi dargli un nome?" } };

                session.Attributes.Add(Slots.AzureResourceType, JsonConvert.SerializeObject(azureResourceType));
                session.Attributes["state"] = States.AskForResourceName;

                response = ResponseBuilder.Ask($"Ho capito che vuoi creare la risorsa '{azureResourceType.Name}'. Vuoi dargli un nome?", reprompt, session);           
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

        static SkillResponse HandleAskForResourceNameIntent(IntentRequest request, Session session, ILogger log)
        {
            SkillResponse response;
            Reprompt reprompt;

            if (request.Intent.Name == Intents.YesIntent)
            {
                session.Attributes["state"] = States.SetResourceName;

                reprompt = new Reprompt { OutputSpeech = new PlainTextOutputSpeech { Text = "Quindi come la devo chiamare?" } };

                response = ResponseBuilder.Ask("Che nome vuoi dargli?", reprompt, session);
            }
            else
            {
                var azureResourceType = JsonConvert.DeserializeObject<AzureResourceType>(session.Attributes[Slots.AzureResourceType].ToString());

                session.Attributes["state"] = States.CreateResource;

                reprompt = new Reprompt { OutputSpeech = new PlainTextOutputSpeech { Text = "Quindi confermi?" } };

                response = ResponseBuilder.Ask($"Sto per creare la risorsa '{azureResourceType.Name}'. Confermi?", reprompt, session);
            }

            return response;
        }

        #endregion 

        #region Set Resource Name 

        static bool CanHandlSetResourceNameIntent(IntentRequest request, Session session)
        {
            return request.Intent.Name == Intents.SetResourceNameIntent && 
                   session.Attributes.ContainsValue(States.SetResourceName);
        }

        static SkillResponse HandlSetResourceNameIntent(IntentRequest request, Session session, ILogger log)
        {
            var slots = request.Intent.Slots;
            var arName = slots[Slots.AzureResourceName].Value;
            var azureResourceType = JsonConvert.DeserializeObject<AzureResourceType>(session.Attributes[Slots.AzureResourceType].ToString());

            session.Attributes[Slots.AzureResourceName] = arName;
            session.Attributes["state"] = States.CreateResource;

            var reprompt = new Reprompt{ OutputSpeech = new PlainTextOutputSpeech { Text = "Quindi confermi?" } };

            var response = ResponseBuilder.Ask($"Sto per creare la risorsa '{azureResourceType.Name}', con il nome '{arName}'. Confermi?", reprompt, session);
            
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
            IAsyncCollector<AzureResourceToDeploy> deployQueue, ILogger log)
        {
            SkillResponse response;

            if (request.Intent.Name == Intents.YesIntent)
            {
                var azureResourceType = JsonConvert.DeserializeObject<AzureResourceType>(session.Attributes[Slots.AzureResourceType].ToString());
                string arName = null;

                var reprompt = new Reprompt { OutputSpeech = new PlainTextOutputSpeech { Text = "Vuoi creare un altro servizio?" } };

                if (session.Attributes.ContainsKey(Slots.AzureResourceName))
                {
                    arName = (string)session.Attributes[Slots.AzureResourceName];
                    response = ResponseBuilder.Tell($"OK, creo la risorsa '{azureResourceType.Name}' con nome '{arName}', ti avviserò con una notifica appena terminato!");
                }
                else
                    response = ResponseBuilder.Tell($"OK, creo la risorsa '{azureResourceType.Name}', ti avviserò con una notifica appena terminato!");

                var azureResourceToDeploy = new AzureResourceToDeploy
                {
                    AzureResource = new AzureResource
                    {
                        Type = azureResourceType,
                        Name = arName
                    },
                    RequestedByUser = session.User.UserId
                };

                //await Task.Delay(TimeSpan.FromSeconds(1));
                await deployQueue.AddAsync(azureResourceToDeploy);

                //var deployOptions = new DeploymentOptions
                //{
                //    Region = Region.EuropeWest,
                //    ResourceGroupName = "TestCodemotionRome19",
                //    UseExistingResourceGroup = true
                //};

                //var progressiveResponse = new ProgressiveResponse(skillRequest);
                //await progressiveResponse.SendSpeech("Please wait while I gather your data.");

                //var azure = await azureService.Authenticate(appSettings.ClientId, appSettings.ClientSecret, appSettings.TenantId);
                //var deployResult = await deploymentService.Deploy(azure, deployOptions, azureResource);
            }
            else
            {
                var reprompt = new Reprompt
                {
                    OutputSpeech = new PlainTextOutputSpeech
                    {
                        Text = "Che tipo di risorsa desideri creare?"
                    }
                };

                response = ResponseBuilder.Ask($"Ah ok, forse allora ho capito male. Cosa desidere creare?", reprompt);
            }

            session.Attributes.Clear();

            return response;
        }

        #endregion 

        #region Unhandled 

        static SkillResponse HandleUnhandled(IntentRequest request)
        {
            var reprompt = new Reprompt
            {
                OutputSpeech = new PlainTextOutputSpeech
                {
                    Text = "Che tipo di risorsa desideri creare?"
                }
            };

            var response = ResponseBuilder.Ask("Non ho capito che tipo di risorsa vuoi creare, devi specificare un servizio Azure valido. Dimmelo di nuovo per favore", reprompt);
            return response;
        }

        #endregion 
    }
}
