using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Azure.Core;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage;
using Microsoft.Azure.Services.AppAuthentication;

namespace UseThisForTest
{
    public static class MSI_HStorageSystemIdentity
    {
        [FunctionName("MSI_HStorageSystemIdentity")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            string name = req.Query["name"];
            try
            {
                log.LogInformation("C# HTTP trigger function processed a request.");


                var StorageURL = Environment.GetEnvironmentVariable("StorageURL");


                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                dynamic data = JsonConvert.DeserializeObject(requestBody);
                name = name ?? data?.name;


                var azureServiceTokenProvider = new AzureServiceTokenProvider();
                string accessToken = await azureServiceTokenProvider.GetAccessTokenAsync("https://storage.azure.com");


                log.LogInformation("accessToken : retrieved");


                // create the credential, using the 
                var tokenCredential = new Microsoft.WindowsAzure.Storage.Auth.TokenCredential(accessToken);
                var storageCredentials = new StorageCredentials(tokenCredential);

                log.LogInformation("credentials : created");
                var fileName = "append";
                var Uri = new Uri(StorageURL + fileName);
                var blob = new CloudAppendBlob(Uri, storageCredentials);

                log.LogInformation($"blobfile : setup {0}", Uri);

                if (!(await blob.ExistsAsync()))
                {
                    await blob.CreateOrReplaceAsync(AccessCondition.GenerateIfNotExistsCondition(), null, null);
                }

                await blob.AppendTextAsync(name);

                var fileName2 = "regular.txt";
                var Uri2 = new Uri(StorageURL + fileName2);
                var blob2 = new CloudBlockBlob(Uri2, storageCredentials);

                await blob2.UploadTextAsync(name);
            }
            catch (Exception ex)
            {
                log.LogInformation($"EXEC {ex.ToString()} ");

            }
            return name != null
            ? (ActionResult)new OkObjectResult($"Hello, {name}")
            : new BadRequestObjectResult("Please pass a name on the query string or in the request body");
        }
    }
}
