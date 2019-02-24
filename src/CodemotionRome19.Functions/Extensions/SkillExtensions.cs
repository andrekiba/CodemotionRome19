﻿using System;
using System.Threading.Tasks;
using Alexa.NET.Request;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace CodemotionRome19.Functions.Extensions
{
    internal static class StringExtensions
    {
        public static bool IsNullOrWhiteSpace(this string s) => string.IsNullOrWhiteSpace(s);
    }

    internal static class RequestValidationExtensions
    {
        const int AllowedTimestampToleranceInSeconds = 150;

        public static async Task<bool> ValidateRequest(this SkillRequest skillRequest, HttpRequest request, ILogger log)
        {
            // Verifies that the request is indeed coming from Alexa.

            request.Headers.TryGetValue("SignatureCertChainUrl", out var signatureChainUrl);
            if (string.IsNullOrWhiteSpace(signatureChainUrl))
            {
                log.LogError("Validation failed. Empty SignatureCertChainUrl header");
                return false;
            }

            Uri certUrl;
            try
            {
                certUrl = new Uri(signatureChainUrl);
            }
            catch
            {
                log.LogError($"Validation failed. SignatureChainUrl not valid: {signatureChainUrl}");
                return false;
            }

            request.Headers.TryGetValue("Signature", out var signature);
            if (string.IsNullOrWhiteSpace(signature))
            {
                log.LogError("Validation failed - Empty Signature header");
                return false;
            }

            request.Body.Position = 0;
            var body = await request.ReadAsStringAsync();
            request.Body.Position = 0;

            if (string.IsNullOrWhiteSpace(body))
            {
                log.LogError("Validation failed - the JSON is empty");
                return false;
            }

            var isTimestampValid = RequestTimestampWithinTolerance(skillRequest.Request.Timestamp);
            var isValid = await RequestVerification.Verify(signature, certUrl, body);

            if (isValid && isTimestampValid)
                return true;

            log.LogError("Validation failed - RequestVerification failed");
            return false;

        }

        static bool RequestTimestampWithinTolerance(DateTime timestamp)
            => Math.Abs(DateTimeOffset.Now.Subtract(timestamp).TotalSeconds) <= AllowedTimestampToleranceInSeconds;
    }
}
