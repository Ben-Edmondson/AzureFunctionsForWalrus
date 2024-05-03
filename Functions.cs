using System.Net;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Azure.Messaging.EventGrid;


public class Functions
{

    private readonly ILogger _logger;

    public Functions(ILogger logger)
    {
        _logger = logger;
    }

    [Function("negotiate")]
    public static async Task<HttpResponseData> Negotiate(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req,
        [SignalRConnectionInfoInput(HubName = "serverless", UserId = "{headers.x-myapp-userid}")] SignalRConnectionInfo connectionInfo,
        FunctionContext executionContext)
    {
        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(new { connectionInfo.Url, connectionInfo.AccessToken });
        return response;
    }

    public class UserInfo
    {
        public string? UserId { get; set; }
        public string? Name { get; set; }
    }

    [Function("broadcastPresence")]
    [SignalROutput(HubName = "serverless")]
    public static async Task<SignalRMessageAction> BroadcastPresence(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req)
    {
        using var bodyReader = new StreamReader(req.Body);
        var requestBody = await bodyReader.ReadToEndAsync();

        var userInfo = JsonSerializer.Deserialize<UserInfo>(requestBody);

        if (userInfo == null)
        {
            throw new Exception("There Is No User To Broadcast");
        }

        var userInfoJson = JsonSerializer.Serialize(new { userId = userInfo.UserId, name = userInfo.Name });

        return new SignalRMessageAction("broadcastUser")
        {
            Arguments = new[] { userInfoJson },
        };
    }

    [Function("broadcastTemplate")]
    [SignalROutput(HubName = "serverless")]
    public static async Task<SignalRMessageAction> BroadcastToAll([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req)
    {
        using var bodyReader = new StreamReader(req.Body);
        var requestBody = await bodyReader.ReadToEndAsync();

        return new SignalRMessageAction("templateUseForTestingPurposesOnly")
        {
            Arguments = new[] { requestBody },
        };
    }


    [Function("broadcastToUser")]
    [SignalROutput(HubName = "serverless")]
    public static async Task<SignalRMessageAction> BroadcastToUser([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req)
    {
        using var bodyReader = new StreamReader(req.Body);
        var requestBody = await bodyReader.ReadToEndAsync();
        var messageContent = JsonSerializer.Deserialize<MessageContent>(requestBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (messageContent == null)
        {
            throw new Exception("Fuck");
        }

        return new SignalRMessageAction("messageForUser")
        {
            UserId = messageContent.UserId,
            Arguments = new[] { messageContent.Message },
        };
    }
    public class MessageContent
    {
        public string UserId { get; set; }
        public string Message { get; set; }
    }
}

