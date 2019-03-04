using System;
using System.Threading.Tasks;
using Alexa.NET;
using Alexa.NET.Request;
using Alexa.NET.Request.Type;
using Alexa.NET.Response;
using CodemotionRome19.Core.Azure;
using CodemotionRome19.Functions.Configuration;
using CodemotionRome19.Functions.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace CodemotionRome19.Functions
{
    public class CodemotionRomeSkill
    {
        readonly AppSettings appSettings;
        readonly IAzureService azureService;

        public CodemotionRomeSkill(AppSettings appSettings, IAzureService azureService)
        {
            this.appSettings = appSettings;
            this.azureService = azureService;
        }

        [FunctionName("CodemotionRomeSkill")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)]HttpRequest req, 
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

            var request = skillRequest.Request;
            SkillResponse response = null;

            try
            {
                switch (request)
                {
                    case LaunchRequest launchRequest:
                        log.LogInformation("Session started");
                        response = ResponseBuilder.Tell("Ciao! Benvenuto in Codemotion Roma.");
                        response.Response.ShouldEndSession = false;
                        break;
                    case IntentRequest intentRequest:
                        // Checks whether to handle system messages defined by Amazon.
                        var systemIntentResponse = HandleSystemIntentRequest(intentRequest);
                        if (systemIntentResponse.IsHandled)
                        {
                            response = systemIntentResponse.Response;
                        }
                        else
                        {
                            // Processes request according to intentRequest.Intent.Name...
                            response = GetResponse(intentRequest, log);
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
                case "AMAZON.CancelIntent":
                    response = ResponseBuilder.Tell("Canceling...");
                    break;
                case "AMAZON.HelpIntent":
                    response = ResponseBuilder.Tell("Help...");
                    response.Response.ShouldEndSession = false;
                    break;
                case "AMAZON.StopIntent":
                    response = ResponseBuilder.Tell("Stopping...");
                    break;
                default:
                    break;
            }

            return (response != null, response);
        }

        static SkillResponse GetResponse(IntentRequest request, ILogger log)
        {
            try
            {
                SkillResponse response = null;

                switch (request.Intent.Name)
                {
                    case "CreateAzureResourceIntent":
                        var reprompt = new Reprompt
                        {
                            OutputSpeech = new PlainTextOutputSpeech
                            {
                                Text = "Confermi?"
                            }
                        };
                        response = ResponseBuilder.Ask($"Sto per creare la risorsa {request.Intent.Slots["AzureResource"].Value}, ti avviserò quando ho terminato", reprompt);
                        break;
                    default:
                        break;
                }

                return response;

            }
            catch (Exception e)
            {
                var error = $"{e.Message}\n\r{e.StackTrace}";
                log.LogError(error);
                return ResponseBuilder.Tell("Purtroppo non riesco a fare il deploy della risorsa richiesta.");
            }
        }

    }
}
