using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Data.SqlClient;
using Microsoft.Azure.Services.AppAuthentication;

namespace UseThisForAllTest
{
    public static class ConnectToSqlByMSI
    {
        [FunctionName("ConnectToSqlByMSI")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {

            try
            {

                bool Succ = false;
                log.LogInformation("C# HTTP trigger function processed a request.");

                var str = Environment.GetEnvironmentVariable("sqldb_connection");

                string name = req.Query["name"];

                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                dynamic data = JsonConvert.DeserializeObject(requestBody);
                name = name ?? data?.name;

                var tokenProvider = new AzureServiceTokenProvider();

                string accessToken = await tokenProvider.GetAccessTokenAsync("https://database.windows.net/");

                log.LogInformation($"accessToken >> {accessToken}");

                using (SqlConnection con = new SqlConnection(str))
                {
                    con.AccessToken = accessToken;
                    using (SqlCommand cmd = new SqlCommand("SaveMsg", con))
                    {
                        cmd.CommandType = System.Data.CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@name", name);
                        if (con.State == System.Data.ConnectionState.Closed)
                            await con.OpenAsync();
                        var rows = await cmd.ExecuteNonQueryAsync();
                        Succ = rows > 0 ? true : false;

                        log.LogInformation($"{rows} rows were updated !!");
                    }
                }

                return name != null  && Succ
               ? (ActionResult)new OkObjectResult($"Hello, {name}")
               : new BadRequestObjectResult("Please pass a name on the query string or in the request body");
            }
            catch (Exception ex)
            {
                log.LogInformation($" Failed to insert your msg to DB  {ex.ToString()}");

                return new BadRequestObjectResult("Please pass a name on the query string or in the request body");
            }
        }
    }
}
