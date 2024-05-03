// Default URL for triggering event grid function in the local environment.
// http://localhost:7071/runtime/webhooks/EventGrid?functionName={functionname}

using System;
using System.Text.Json;
using Azure.Messaging;
using Azure.Messaging.EventGrid;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace WalrusFunc
{
    public class onDisconnectTrigger
    {
        private readonly ILogger<onDisconnectTrigger> _logger;

        public onDisconnectTrigger(ILogger<onDisconnectTrigger> logger)
        {
            _logger = logger;
        }


        [Function("OnDisconnectBroadcast")]
        [SignalROutput(HubName = "serverless")]
        public async Task<SignalRMessageAction> OnDisconnectBroadcast([EventGridTrigger] EventGridEvent eventGridEvent)
        {
            var eventDataAsString = eventGridEvent.Data?.ToString();

            if (eventDataAsString == null)
            {
                _logger.LogInformation("Empty");
            }
            else
            {
                var jsonDict = JsonSerializer.Deserialize<Dictionary<string, string>>(eventDataAsString);
                var payload = new EventGridPayloadViewModel
                {
                    Timestamp = jsonDict["timestamp"],
                    HubName = jsonDict["hubName"],
                    ConnectionId = jsonDict["connectionId"],
                    UserId = jsonDict["userId"],
                    ErrorMessage = jsonDict["errorMessage"]
                };
                _logger.LogInformation(eventDataAsString);
                _logger.LogInformation($"User ID: {payload.UserId}");
                return new SignalRMessageAction("userDisconnected")
                {
                    Arguments = new[] { eventDataAsString},
                };
            }
                return new SignalRMessageAction("userDisconnected")
                {
                    Arguments = new[] { "No User attached. Yikes."},
                };
        }



        public class EventGridPayloadViewModel
        {
            public string? Timestamp { get; set; }
            public string? HubName { get; set; }
            public string? ConnectionId { get; set; }
            public string? UserId { get; set; }
            public string? ErrorMessage { get; set; }
        }
    }
}
