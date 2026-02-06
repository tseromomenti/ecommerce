using InventoryService.Business.Interfaces;

namespace InventoryService.Api.Contracts;

internal sealed class InventorySearchRequest
{
    public string Query { get; set; } = string.Empty;
    public int? MaxResults { get; set; } = 10;
    public SearchFilters? Filters { get; set; }
}
