using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Azure;
using Azure.Communication.Sms;
using System.Text.Json;

namespace ACSGPTFunctions
{
    public class SMSTrigger
    {
        private readonly ILogger<SMSTrigger> _logger;
        private readonly SmsClient _smsClient;
        private string? sender = Environment.GetEnvironmentVariable("SENDER_PHONE_NUMBER");

        public SMSTrigger(ILogger<SMSTrigger> logger)
        {
            _logger = logger;
            string? connectionString = Environment.GetEnvironmentVariable("COMMUNICATION_SERVICES_CONNECTION_STRING");
            if (connectionString is null)
            {
                throw new InvalidOperationException("COMMUNICATION_SERVICES_CONNECTION_STRING environment variable is not set.");
            }
            _smsClient = new SmsClient(connectionString);
        }

        public class SmsRequest
        {
            public string Message { get; set; } = string.Empty;
            public string PhoneNumber { get; set; } = string.Empty;
        }

        [Function("SmsTrigger")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req)
        {
            _logger.LogInformation("Processing request.");

            // Read the request body
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

            // Try to deserialize the request body into an SmsRequest object
            SmsRequest? data = JsonSerializer.Deserialize<SmsRequest>(requestBody, new JsonSerializerOptions() {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            // If the deserialization failed or the data is null, return a 400 Bad Request response
            if (data is null)
            {
                return new BadRequestResult();
            }

            // Define the sender phone number
            

            // Try to send the SMS
            try
            {
                _logger.LogInformation("Sending SMS...");
                SmsSendResult smsSendResult = await _smsClient.SendAsync(
                    sender,
                    data.PhoneNumber,
                    data.Message
                );

                _logger.LogInformation($"SMS Sent. Successful = {smsSendResult.Successful}");
                _logger.LogInformation($"SMS operation id = {smsSendResult.MessageId}");
            }
            catch (RequestFailedException ex)
            {
                _logger.LogInformation($"SMS send operation failed with error code: {ex.ErrorCode}, message: {ex.Message}");
            }

            return new OkObjectResult("SMS sent successfully!");
        }
    }
}