using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using RepairPlannerAgent.Models;

namespace RepairPlannerAgent.Services;

public sealed class CosmosDbService : IDisposable
{
    private readonly CosmosClient _client;
    private readonly Container _techniciansContainer;
    private readonly Container _partsContainer;
    private readonly Container _workOrdersContainer;
    private readonly ILogger<CosmosDbService> _logger;

    public CosmosDbService(
        string endpoint,
        string key,
        string databaseName,
        ILogger<CosmosDbService> logger)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(endpoint);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentException.ThrowIfNullOrWhiteSpace(databaseName);
        ArgumentNullException.ThrowIfNull(logger);

        _logger = logger;
        _client = new CosmosClient(endpoint, key);

        var database = _client.GetDatabase(databaseName);
        _techniciansContainer = database.GetContainer("Technicians");
        _partsContainer = database.GetContainer("PartsInventory");
        _workOrdersContainer = database.GetContainer("WorkOrders");
    }

    public async Task<List<Technician>> GetAvailableTechniciansWithSkillsAsync(
        List<string> requiredSkills,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(requiredSkills);
        cancellationToken.ThrowIfCancellationRequested();

        var skills = requiredSkills
            .Where(skill => !string.IsNullOrWhiteSpace(skill))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var query = new QueryDefinition(
            "SELECT * FROM c WHERE c.available = true");

        if (skills.Count > 0)
        {
            var skillFilters = skills
                .Select((_, index) => $"ARRAY_CONTAINS(c.skills, @skill{index})")
                .ToArray();

            query = new QueryDefinition(
                $"SELECT * FROM c WHERE c.available = true AND ({string.Join(" OR ", skillFilters)})");

            for (var index = 0; index < skills.Count; index++)
            {
                query.WithParameter($"@skill{index}", skills[index]);
            }
        }

        try
        {
            var technicians = new List<Technician>();
            using var iterator = _techniciansContainer.GetItemQueryIterator<Technician>(query);

            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync(cancellationToken);
                technicians.AddRange(response);
            }

            _logger.LogInformation(
                "Retrieved {TechnicianCount} available technicians matching {SkillCount} required skills",
                technicians.Count,
                skills.Count);

            return technicians;
        }
        catch (CosmosException exception)
        {
            _logger.LogError(
                exception,
                "Cosmos DB failed while querying available technicians by skills");
            throw;
        }
    }

    public async Task<List<Part>> GetPartsInventoryAsync(
        List<string> partNumbers,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(partNumbers);
        cancellationToken.ThrowIfCancellationRequested();

        var requestedPartNumbers = partNumbers
            .Where(partNumber => !string.IsNullOrWhiteSpace(partNumber))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (requestedPartNumbers.Length == 0)
        {
            return [];
        }

        try
        {
            var query = new QueryDefinition(
                    "SELECT * FROM c WHERE ARRAY_CONTAINS(@partNumbers, c.partNumber)")
                .WithParameter("@partNumbers", requestedPartNumbers);

            var parts = new List<Part>();
            using var iterator = _partsContainer.GetItemQueryIterator<Part>(query);

            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync(cancellationToken);
                parts.AddRange(response);
            }

            _logger.LogInformation(
                "Retrieved {PartCount} inventory items for {RequestedPartCount} part numbers",
                parts.Count,
                requestedPartNumbers.Length);

            return parts;
        }
        catch (CosmosException exception)
        {
            _logger.LogError(
                exception,
                "Cosmos DB failed while retrieving parts inventory");
            throw;
        }
    }

    public async Task<string> CreateWorkOrderAsync(
        WorkOrder workOrder,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(workOrder);
        ArgumentException.ThrowIfNullOrWhiteSpace(workOrder.Status);
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(workOrder.Id))
        {
            workOrder.Id = Guid.NewGuid().ToString();
        }

        try
        {
            var response = await _workOrdersContainer.CreateItemAsync(
                workOrder,
                new PartitionKey(workOrder.Status),
                cancellationToken: cancellationToken);

            _logger.LogInformation(
                "Created work order {WorkOrderNumber} with id {WorkOrderId}",
                workOrder.WorkOrderNumber,
                response.Resource.Id);

            return response.Resource.Id;
        }
        catch (CosmosException exception)
        {
            _logger.LogError(
                exception,
                "Cosmos DB failed while creating work order {WorkOrderNumber}",
                workOrder.WorkOrderNumber);
            throw;
        }
    }

    public void Dispose() => _client.Dispose();
}
