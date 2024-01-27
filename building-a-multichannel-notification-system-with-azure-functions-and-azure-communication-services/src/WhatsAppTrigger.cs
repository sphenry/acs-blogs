using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

using Azure;
using Azure.Communication.Messages;
using System.Text.Json;

namespace ACSGPTFunctions
{
    public class WhatsAppTrigger
    {
        private readonly ILogger<WhatsAppTrigger> _logger;

        private readonly NotificationMessagesClient _messagesClient;

        private string? sender = Environment.GetEnvironmentVariable("WHATSAPP_NUMBER");

        public WhatsAppTrigger(ILogger<WhatsAppTrigger> logger)
        {
            _logger = logger;
            string? connectionString = Environment.GetEnvironmentVariable("COMMUNICATION_SERVICES_CONNECTION_STRING");
            if (connectionString is null)
            {
                throw new InvalidOperationException("COMMUNICATION_SERVICES_CONNECTION_STRING environment variable is not set.");
            }
            _messagesClient = new NotificationMessagesClient(connectionString);
        }

        public class WhatsAppRequest
        {
            public string PhoneNumber { get; set; } = string.Empty;
            public string TemplateName { get; set; } = "appointment_reminder";
            public string TemplateLanguage { get; set; } = "en";
            public List<string> TemplateParameters { get; set; } = new List<string>();

        }

        [Function("WhatsAppTrigger")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequest req)
        {
            _logger.LogInformation("Processing request.");

            // Read the request body
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

            // Try to deserialize the request body into an EmailRequest object
            WhatsAppRequest? data = JsonSerializer.Deserialize<WhatsAppRequest>(requestBody, new JsonSerializerOptions() {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            // If the deserialization failed or the data is null, return a 400 Bad Request response
            if (data is null)
            {
                return new BadRequestResult();
            }

            var recipientList = new List<string> { data.PhoneNumber }; // Use the provided phone number

            var values = data.TemplateParameters
                .Select((parameter, index) => new MessageTemplateText($"value{index + 1}", parameter))
                .ToList();

            var bindings = new MessageTemplateWhatsAppBindings(
                body: values.Select(value => value.Name).ToList()
            );

            var template = new MessageTemplate(data.TemplateName, data.TemplateLanguage, values, bindings);

            var sendTemplateMessageOptions = new SendMessageOptions(sender, recipientList, template);
            Response<SendMessageResult> templateResponse = await _messagesClient.SendMessageAsync(sendTemplateMessageOptions);

            return new OkObjectResult("WhatsApp sent successfully!");

        }
    }
}
