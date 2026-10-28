using Azure.AI.Projects;
using Azure.Identity;
using Microsoft.Extensions.Logging;
using RepairPlannerAgent.Models;
using RepairPlannerAgent.Services;

var projectEndpoint = GetRequiredEnvironmentVariable("AZURE_AI_PROJECT_ENDPOINT");
var modelDeploymentName = GetRequiredEnvironmentVariable("MODEL_DEPLOYMENT_NAME");
var cosmosEndpoint = GetRequiredEnvironmentVariable("COSMOS_ENDPOINT");
var cosmosKey = GetRequiredEnvironmentVariable("COSMOS_KEY");
var cosmosDatabaseName = GetRequiredEnvironmentVariable("COSMOS_DATABASE_NAME");

using var loggerFactory = LoggerFactory.Create(builder =>
    builder.AddSimpleConsole(options =>
    {
        options.IncludeScopes = true;
        options.SingleLine = false;
        options.TimestampFormat = "yyyy-MM-dd HH:mm:ss ";
    }));
var logger = loggerFactory.CreateLogger("RepairPlanner");
var credential = new DefaultAzureCredential();
using var cosmosDb = new CosmosDbService(
    cosmosEndpoint,
    cosmosKey,
    cosmosDatabaseName,
    loggerFactory.CreateLogger<CosmosDbService>());

var projectClient = new AIProjectClient(new Uri(projectEndpoint), credential);
var faultMapping = new FaultMappingService();
var repairPlanner = new RepairPlannerAgent.RepairPlannerAgent(
    projectClient,
    cosmosDb,
    faultMapping,
    modelDeploymentName,
    loggerFactory.CreateLogger<RepairPlannerAgent.RepairPlannerAgent>());

var sampleFault = new DiagnosedFault
{
    FaultType = "curing_temperature_excessive",
    MachineId = "machine-001",
    Description = "Curing press temperature is above its configured operating range.",
    Severity = "high",
    DetectedAt = DateTimeOffset.UtcNow,
    Symptoms =
    [
        "Mold temperature exceeds the configured set point",
        "Temperature readings remain elevated during the curing cycle"
    ]
};

logger.LogInformation(
    "Starting repair planning demonstration for {MachineId}, fault {FaultType}",
    sampleFault.MachineId,
    sampleFault.FaultType);

await repairPlanner.EnsureAgentVersionAsync();
var workOrder = await repairPlanner.PlanAndCreateWorkOrderAsync(sampleFault);

Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(
    workOrder,
    new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));

static string GetRequiredEnvironmentVariable(string name)
{
    var value = Environment.GetEnvironmentVariable(name);

    return string.IsNullOrWhiteSpace(value)
        ? throw new InvalidOperationException(
            $"Required environment variable '{name}' is not set.")
        : value;
}
