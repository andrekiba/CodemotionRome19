using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Alexa.NET;
using Alexa.NET.Request;
using Alexa.NET.Request.Type;
using Alexa.NET.Response;
using CodemotionRome19.Core.Azure;
using CodemotionRome19.Functions.Alexa;
using CodemotionRome19.Functions.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace CodemotionRome19.Functions
{
    public class Aldo
    {
        [FunctionName("Aldo")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)]HttpRequest req,
            [Queue("azure-resources", Connection = "AzureWebJobsStorage")] IAsyncCollector<AzureResource> azureResourceQueue,
            ILogger log)
        {
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
                        log.LogInformation("Session started");
                        response = ResponseBuilder.Tell("Ciao! sono Aldo, come posso aiutarti?");
                        response.Response.ShouldEndSession = false;
                        break;
                    case IntentRequest intentRequest:
                        // Checks whether to handle system messages defined by Amazon.
                        var systemIntentResponse = HandleSystemIntentRequest(intentRequest);
                        if (systemIntentResponse.IsHandled)
                            response = systemIntentResponse.Response;
                        else
                        {
                            try
                            {
                                switch (intentRequest.Intent.Name)
                                {
                                    case Intents.CreateAzureResourceIntent:
                                        response = HandleCreateAzureResourceIntent(intentRequest, session, log);
                                        break;
                                    case Intents.AzureResourceNameIntent:
                                        response = HandleAzureResourceNameIntent(intentRequest, session, log);
                                        break;
                                    case Intents.CreateAzureResourceConfirmationIntent:
                                        response = await HandleCreateAzureResourceConfirmationIntent(azureResourceQueue, session, log);
                                        break;
                                    default:
                                        break;
                                }
                            }
                            catch (Exception e)
                            {
                                var error = $"{e.Message}\n\r{e.StackTrace}";
                                log.LogError(error);
                                response = ResponseBuilder.Tell("Purtroppo non riesco a fare il deploy della risorsa richiesta.");
                            }
                        }
                        break;
                    case SessionEndedRequest sessionEndedRequest:
                        log.LogInformation("Session ended");
                        response = ResponseBuilder.Empty();
                        response.Response.ShouldEndSession = true;
                        break;
                }
            }
            catch
            {
                response = ResponseBuilder.Tell("Mi dispiace, c'è stato un errore inatteso. Per favore, riprova più tardi.");
            }

            return new OkObjectResult(response);
        }

        static (bool IsHandled, SkillResponse Response) HandleSystemIntentRequest(IntentRequest request)
        {
            SkillResponse response = null;
            switch (request.Intent.Name)
            {
                case Intents.CancelIntent:
                    response = ResponseBuilder.Tell("Canceling...");
                    break;
                case Intents.HelpIntent:
                    response = ResponseBuilder.Tell("Help...");
                    response.Response.ShouldEndSession = false;
                    break;
                case Intents.StopIntent:
                    response = ResponseBuilder.Tell("Stopping...");
                    break;
                default:
                    break;
            }

            return (response != null, response);
        }

        static SkillResponse HandleCreateAzureResourceIntent(IntentRequest request, Session session, ILogger log)
        {
            if (session.Attributes == null)
                session.Attributes = new Dictionary<string, object>();

            SkillResponse response;
            Reprompt reprompt;

            var slots = request.Intent.Slots;
            var arType = slots[Slots.AzureResource].Value;
            log.LogInformation(arType);
            log.LogInformation(slots[Slots.AzureResource].Resolution.Authorities.First().Values.First().Value.Id);

            if (!Enum.TryParse(arType, true, out AzureResourceType azureResourceType))
            {
                reprompt = new Reprompt
                {
                    OutputSpeech = new PlainTextOutputSpeech
                    {
                        Text = "Che tipo di risorsa desideri creare?"
                    }
                };

                response = ResponseBuilder.Ask("Non ho capito che tipo di risorsa vuoi creare, devi specificare un servizio Azure valido. Dimmelo di nuovo per favore", reprompt);
            }
            else
            {
                reprompt = new Reprompt
                {
                    OutputSpeech = new PlainTextOutputSpeech
                    {
                        Text = "Confermi?"
                    }
                };

                if (!slots.ContainsKey(Slots.AzureResourceName))
                {
                    session.Attributes[Slots.AzureResource] = azureResourceType;
                    response = ResponseBuilder.Ask($"Ho capito che vuoi creare la risorsa {arType}", reprompt, session);
                }
                else
                {
                    var arName = slots[Slots.AzureResourceName].Value;

                    session.Attributes[Slots.AzureResource] = azureResourceType;
                    session.Attributes[Slots.AzureResourceName] = arName;
                    response = ResponseBuilder.Ask($"Ho capito che vuoi creare la risorsa {arType} {arName}", reprompt, session);
                }
            }

            return response;
        }

        static SkillResponse HandleAzureResourceNameIntent(IntentRequest request, Session session, ILogger log)
        {
            var slots = request.Intent.Slots;
            var arName = slots[Slots.AzureResourceName].Value;

            session.Attributes[Slots.AzureResourceName] = arName;

            var reprompt = new Reprompt
            {
                OutputSpeech = new PlainTextOutputSpeech
                {
                    Text = "Confermi?"
                }
            };

            var response = ResponseBuilder.Ask($"Ho capito che il nome sarà {arName}", reprompt, session);
            
            return response;
        }

        static async Task<SkillResponse> HandleCreateAzureResourceConfirmationIntent(IAsyncCollector<AzureResource> azureResourceQueue, Session session, ILogger log)
        {
            var azureResourceType = (AzureResourceType)session.Attributes[Slots.AzureResource];
            string arName = null;
            SkillResponse response;

            if (session.Attributes.ContainsKey(Slots.AzureResourceName))
            {
                arName = (string)session.Attributes[Slots.AzureResourceName];
                response = ResponseBuilder.Tell($"Creo la risorsa {azureResourceType} {arName}, ti avviserò appena terminato!");
            }
            else
                response = ResponseBuilder.Tell($"Creo la risorsa {azureResourceType}, ti avviserò appena terminato!");

            var azureResource = new AzureResource
            {
                Type = azureResourceType,
                Name = arName
            };

            await azureResourceQueue.AddAsync(azureResource);

            return response;
        }
    }
}
