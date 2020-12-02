using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Azure.KeyVault;
using System.Text;
using Azure.Storage.Blobs;
using Azure.Identity;
using Azure;

namespace UseThisForTest
{
    public static class MSI_UserAssignedKeyVault
    {
        [FunctionName("MSI_UserAssignedKeyVault")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {

            string name = string.Empty;


            try
            { 

            log.LogInformation("C# HTTP trigger function processed a request.");

                var AppID = Environment.GetEnvironmentVariable("AppID");
                var KVURL = Environment.GetEnvironmentVariable("KVURL");

                AzureServiceTokenProvider azureServiceTokenProvider = new AzureServiceTokenProvider($"RunAs=App;AppId={AppID}");
                var keyVaultClient = new KeyVaultClient(
                    new KeyVaultClient.AuthenticationCallback(azureServiceTokenProvider.KeyVaultTokenCallback));

                var secret = await keyVaultClient.GetSecretAsync(KVURL)
                    .ConfigureAwait(false);

                log.LogInformation($"{secret.Value}");

           

             name = secret.Value;

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            name = name ?? data?.name;

         
            }
            catch (Exception ex)
            {
                log.LogInformation($"Exception thrown : {ex}");
            }

            return name != null
             ? (ActionResult)new OkObjectResult($"Hello, {name}")
             : new BadRequestObjectResult("Please pass a name on the query string or in the request body");

        }


       
    }
}
