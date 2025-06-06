using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

public static class FilterHttpFunction
{
    [Function("FilterHttpFunction")]
    public static async Task<HttpResponseData> Run(
        [Microsoft.Azure.Functions.Worker.HttpTrigger(Microsoft.Azure.Functions.Worker.AuthorizationLevel.Function, "post")] HttpRequestData req,
        FunctionContext executionContext)
    {
        var logger = executionContext.GetLogger("FilterHttpFunction");
        logger.LogInformation("Received a request to filter telemetry data");

        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        if (string.IsNullOrEmpty(requestBody))
        {
            logger.LogWarning("No data provided in the request body");
            var badResponse = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
            await badResponse.WriteStringAsync("Please provide a JSON payload with telemetry data.");
            return badResponse;
        }

        try
        {
            var telemetryData = JsonConvert.DeserializeObject<List<TelemetryData>>(requestBody);
            if (telemetryData == null || telemetryData.Count == 0)
            {
                logger.LogWarning("Invalid or empty telemetry data");
                var badResponse = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
                await badResponse.WriteStringAsync("Invalid or empty telemetry data.");
                return badResponse;
            }

            logger.LogInformation($"Processing {telemetryData.Count} telemetry records");

            var filteredData = telemetryData
                .Where(doc => doc.Status == "active")
                .Take(40)
                .ToList();

            logger.LogInformation($"Filtered to {filteredData.Count} active records");

            var okResponse = req.CreateResponse(System.Net.HttpStatusCode.OK);
            await okResponse.WriteAsJsonAsync(filteredData);
            return okResponse;
        }
        catch (JsonException ex)
        {
            logger.LogError($"Failed to parse JSON: {ex.Message}");
            var badResponse = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
            await badResponse.WriteStringAsync("Invalid JSON format.");
            return badResponse;
        }
    }
}

public class TelemetryData
{
    public string Id { get; set; }
    public string Status { get; set; }
    public double Value { get; set; }
}
