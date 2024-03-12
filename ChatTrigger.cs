using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Azure.AI.OpenAI;
using Azure;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;

namespace mockgpt
{
    /// <summary>
    /// The Azure Function Class
    /// </summary>
    public static class ChatTrigger
    {
        [FunctionName("dev-genai-irischat-ns")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "chat")] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");
            BadRequestObjectResult badRequestObj;
            PayLoad payLoad;

            // verify headers
            badRequestObj = CheckHeaders(req);
            if (badRequestObj != null)
            {
                return badRequestObj;
            }
            log.LogInformation("Headers verified!");


            // Verify Token
            string token = req.Headers["Authorization"]; 
            badRequestObj = ValidateToken(token);
            if (badRequestObj != null)
            {
                return badRequestObj;
            }
            log.LogInformation("Token verified! (Access Granted)");
            
            // Read the request body
            try
            {
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                log.LogInformation("Request Body: " + requestBody);
                payLoad = JsonConvert.DeserializeObject<PayLoad>(requestBody);
            }
            catch (JsonSerializationException e)
            {
                log.LogError("malformed request body: " + e.Message);
                return new BadRequestObjectResult("Error reading request body: " + e.Message);
            }        
            catch (Exception e)
            {
                log.LogError("Error reading request body: " + e.Message);
                return new BadRequestObjectResult("Error reading request body: " + e.Message);
            }
            log.LogInformation("Payload verified! (Processessing request...)");

            // call Chat Service and return response
            Uri azureOpenAIResourceUri = new(Environment.GetEnvironmentVariable("AZURE_OPENAI_URI"));
            AzureKeyCredential azureOpenAIApiKey = new(Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY"));
            OpenAIClient client = new(azureOpenAIResourceUri, azureOpenAIApiKey);

            var chatCompletionsOptions = new ChatCompletionsOptions()
            {
                DeploymentName = Environment.GetEnvironmentVariable("AZURE_OPENAI_MODEL"), // Use DeploymentName for "model" with non-Azure clients
                Messages =
                {
                    // The system message represents instructions or other guidance about how the assistant should behave
                    new ChatRequestSystemMessage("You are a helpful assistant. You will answer questions and respond to requests."),
                    new ChatRequestUserMessage(payLoad.message)
                }
            };

            Response<ChatCompletions> response = await client.GetChatCompletionsAsync(chatCompletionsOptions);
            ChatResponseMessage responseMessage = response.Value.Choices[0].Message;
            Console.WriteLine($"[{responseMessage.Role.ToString().ToUpperInvariant()}]: {responseMessage.Content}");

            return new OkObjectResult(responseMessage.Content);
        }

        /// <summary>
        /// Validate the token
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        private static BadRequestObjectResult ValidateToken(string token)
        {

            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token.Replace("Bearer ", ""));

            var scopeClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "scope");
            if (scopeClaim == null)
            {
                return new BadRequestObjectResult($"Token does not contain scope claim.");
            }

            if (scopeClaim.Value != Environment.GetEnvironmentVariable("SCOPE_CLAIM"))
            {
                return new BadRequestObjectResult($"Invalid scope claim. Value: {scopeClaim.Value}");
            }

            return null;

        }

        /// <summary>
        /// Check the headers for the required headers
        /// </summary>
        /// <todo> Externalize headers to Environment variables to be more dynamic for all scenerios</todo>
        /// <param name="req"></param>
        /// <returns></returns>
        private static BadRequestObjectResult CheckHeaders(HttpRequest req)
        {
            if (!req.Headers.ContainsKey("x-traceability-id") || req.Headers["x-traceability-id"] != "123e4567-e89b-12d3-a456-426655440000")
            {
                return new BadRequestObjectResult($"x-traceability-id header missing or invalid. should be: x-traceability-id: 123e4567-e89b-12d3-a456-426655440000");
            }

            if (!req.Headers.ContainsKey("Authorization"))
            {
                return new BadRequestObjectResult("Authorization header is missing");
            }

            if (!req.Headers.ContainsKey("Content-Type") || req.Headers["Content-Type"] != "application/json")
            {
                return new BadRequestObjectResult($"Content-Type header is missing or invalid, should be: Content-Type: application/json");
            }

            return null;
        }
    }
}
