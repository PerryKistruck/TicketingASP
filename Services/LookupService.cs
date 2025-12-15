using TicketingASP.Models.DTOs;
using TicketingASP.Repositories;

namespace TicketingASP.Services;

/// <summary>
/// Service interface for lookup data operations
/// </summary>
public interface ILookupService
{
    Task<List<LookupItemDto>> GetPrioritiesAsync();
    Task<List<LookupItemDto>> GetStatusesAsync();
    Task<List<CategoryLookupDto>> GetCategoriesAsync();
    Task<List<LookupItemDto>> GetTeamsAsync();
    Task<List<AgentLookupDto>> GetAgentsAsync();
    Task<TicketFormLookupsDto> GetAllLookupsAsync();
}

/// <summary>
/// Implementation of lookup service
/// </summary>
public class LookupService : ILookupService
{
    private readonly ILookupRepository _lookupRepository;
    private readonly ILogger<LookupService> _logger;

    public LookupService(ILookupRepository lookupRepository, ILogger<LookupService> logger)
    {
        _lookupRepository = lookupRepository;
        _logger = logger;
    }

    public async Task<List<LookupItemDto>> GetPrioritiesAsync()
    {
        return await _lookupRepository.GetPrioritiesAsync();
    }

    public async Task<List<LookupItemDto>> GetStatusesAsync()
    {
        return await _lookupRepository.GetStatusesAsync();
    }

    public async Task<List<CategoryLookupDto>> GetCategoriesAsync()
    {
        return await _lookupRepository.GetCategoriesAsync();
    }

    public async Task<List<LookupItemDto>> GetTeamsAsync()
    {
        return await _lookupRepository.GetTeamsAsync();
    }

    public async Task<List<AgentLookupDto>> GetAgentsAsync()
    {
        return await _lookupRepository.GetAgentsAsync();
    }

    public async Task<TicketFormLookupsDto> GetAllLookupsAsync()
    {
        _logger.LogDebug("Fetching all lookups for ticket form");

        var prioritiesTask = GetPrioritiesAsync();
        var statusesTask = GetStatusesAsync();
        var categoriesTask = GetCategoriesAsync();
        var teamsTask = GetTeamsAsync();
        var agentsTask = GetAgentsAsync();

        await Task.WhenAll(prioritiesTask, statusesTask, categoriesTask, teamsTask, agentsTask);

        return new TicketFormLookupsDto
        {
            Priorities = await prioritiesTask,
            Statuses = await statusesTask,
            Categories = await categoriesTask,
            Teams = await teamsTask,
            Agents = await agentsTask
        };
    }
}
