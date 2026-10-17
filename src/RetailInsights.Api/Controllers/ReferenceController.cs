using Microsoft.AspNetCore.Mvc;
using RetailInsights.Core.Kpis;

namespace RetailInsights.Api.Controllers;

/// <summary>Reference data used by the dashboard filters.</summary>
[ApiController]
[Route("api")]
[Produces("application/json")]
public sealed class ReferenceController(ISalesAggregates aggregates) : ControllerBase
{
    /// <summary>Store network.</summary>
    /// <param name="cancellationToken">Request cancellation.</param>
    [HttpGet("stores")]
    public Task<IReadOnlyList<StoreInfo>> Stores(CancellationToken cancellationToken) => aggregates.StoresAsync(cancellationToken);

    /// <summary>First and last business days with sales.</summary>
    /// <param name="cancellationToken">Request cancellation.</param>
    [HttpGet("data-range")]
    public Task<DataRangeInfo> DataRange(CancellationToken cancellationToken) => aggregates.DataRangeAsync(cancellationToken);
}
