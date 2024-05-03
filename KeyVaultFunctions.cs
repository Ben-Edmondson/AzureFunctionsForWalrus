using System.Net;
using System.Text.Json;
using Azure.Identity;
using Azure.Security.KeyVault.Keys;
using Azure.Security.KeyVault.Keys.Cryptography;
using Azure.Security.KeyVault.Secrets;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace WalrusFunc
{
    public class KeyVaultFunctions
    {
        private readonly ILogger<KeyVaultFunctions> _logger;

        public KeyVaultFunctions(ILogger<KeyVaultFunctions> logger)
        {
            _logger = logger;
        }
        [Function("GetMasterKey")]
        public static async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequestData req,
            FunctionContext executionContext)
        {
            var logger = executionContext.GetLogger("GetMasterKey");
            var secretUrl = req.Query["secretUrl"];

            if (string.IsNullOrEmpty(secretUrl))
            {
                logger.LogError("Secret URL must be provided as a query parameter.");
                return req.CreateResponse(HttpStatusCode.BadRequest);
            }

            try
            {
                var uri = new Uri(secretUrl);
                var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);

                if (segments.Length < 2)
                {
                    logger.LogError("Invalid secret URL format.");
                    return req.CreateResponse(HttpStatusCode.BadRequest);
                }

                string vaultBaseUrl = $"{uri.Scheme}://{uri.Host}";
                string secretName = segments[1];

                var credential = new DefaultAzureCredential();
                var client = new SecretClient(new Uri(vaultBaseUrl), credential);

                logger.LogInformation("Successfully connected to Azure Key Vault.");

                KeyVaultSecret secret = await client.GetSecretAsync(secretName);

                var response = req.CreateResponse(HttpStatusCode.OK);

                // Assuming plain text, set appropriate content type 
                response.Headers.Add("Content-Type", "text/plain");
                response.WriteStringAsync(secret.Value);
                logger.LogInformation($"Secret :{secret.Value}. exists and we got it!");
                return response;
            }
            catch (Exception ex)
            {
                logger.LogError($"Failed to retrieve the secret: {ex.Message}");
                return req.CreateResponse(HttpStatusCode.InternalServerError);
            }
        }

    }
}
