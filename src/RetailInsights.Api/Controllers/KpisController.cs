using Microsoft.AspNetCore.Mvc;
using RetailInsights.Core.Kpis;

namespace RetailInsights.Api.Controllers;

/// <summary>Performance indicators of the store network.</summary>
/// <remarks>
/// Every endpoint accepts an inclusive period (<c>from</c>, <c>to</c>, ISO dates). Without a period,
/// the last 30 days of available data are used. Responses carry an <c>X-Cache</c> header (HIT or MISS).
/// </remarks>
[ApiController]
[Route("api/kpis")]
[Produces("application/json")]
public sealed class KpisController(KpiService kpis) : ControllerBase
{
    /// <summary>Headline figures and their evolution versus the previous period of the same length.</summary>
    /// <param name="from">First business day (inclusive), e.g. 2026-09-01.</param>
    /// <param name="to">Last business day (inclusive).</param>
    /// <param name="storeId">Restrict to one store; whole network when omitted.</param>
    /// <param name="cancellationToken">Request cancellation.</param>
    /// <response code="200">Revenue (CA), tickets, average basket (panier moyen), units per ticket (indice de vente), average selling price, growth.</response>
    /// <response code="400">Invalid period.</response>
    [HttpGet("summary")]
    [ProducesResponseType<KpiSummary>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<KpiSummary> Summary(DateOnly? from, DateOnly? to, int? storeId, CancellationToken cancellationToken)
    {
        var filter = new KpiFilter(await kpis.ResolvePeriodAsync(from, to, cancellationToken), storeId);
        return Cached(await kpis.SummaryAsync(filter, cancellationToken));
    }

    /// <summary>Revenue by store, ranked, with share of the network and growth.</summary>
    /// <param name="from">First business day (inclusive).</param>
    /// <param name="to">Last business day (inclusive).</param>
    /// <param name="cancellationToken">Request cancellation.</param>
    [HttpGet("stores")]
    [ProducesResponseType<IReadOnlyList<StorePerformance>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IReadOnlyList<StorePerformance>> Stores(DateOnly? from, DateOnly? to, CancellationToken cancellationToken) =>
        Cached(await kpis.StoresAsync(await kpis.ResolvePeriodAsync(from, to, cancellationToken), cancellationToken));

    /// <summary>Revenue over time. Days without sales are returned as zeros.</summary>
    /// <param name="from">First business day (inclusive).</param>
    /// <param name="to">Last business day (inclusive).</param>
    /// <param name="storeId">Restrict to one store.</param>
    /// <param name="granularity">Day, Week (ISO, starting Monday) or Month. Chosen from the period length when omitted.</param>
    /// <param name="cancellationToken">Request cancellation.</param>
    [HttpGet("trend")]
    [ProducesResponseType<IReadOnlyList<TrendPoint>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IReadOnlyList<TrendPoint>> Trend(DateOnly? from, DateOnly? to, int? storeId, TrendGranularity? granularity, CancellationToken cancellationToken)
    {
        var filter = new KpiFilter(await kpis.ResolvePeriodAsync(from, to, cancellationToken), storeId);
        return Cached(await kpis.TrendAsync(filter, granularity, cancellationToken));
    }

    /// <summary>Best-selling products.</summary>
    /// <param name="from">First business day (inclusive).</param>
    /// <param name="to">Last business day (inclusive).</param>
    /// <param name="storeId">Restrict to one store.</param>
    /// <param name="by">Ranking criterion: Revenue (default) or Units.</param>
    /// <param name="top">Number of products, 1 to 50 (default 10).</param>
    /// <param name="cancellationToken">Request cancellation.</param>
    [HttpGet("top-products")]
    [ProducesResponseType<IReadOnlyList<ProductPerformance>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IReadOnlyList<ProductPerformance>> TopProducts(
        DateOnly? from, DateOnly? to, int? storeId, ProductRanking by = ProductRanking.Revenue, int top = 10, CancellationToken cancellationToken = default)
    {
        var filter = new KpiFilter(await kpis.ResolvePeriodAsync(from, to, cancellationToken), storeId);
        return Cached(await kpis.TopProductsAsync(filter, by, top, cancellationToken));
    }

    /// <summary>Revenue split by sales channel (in store, click and collect, online).</summary>
    /// <param name="from">First business day (inclusive).</param>
    /// <param name="to">Last business day (inclusive).</param>
    /// <param name="storeId">Restrict to one store.</param>
    /// <param name="cancellationToken">Request cancellation.</param>
    [HttpGet("channels")]
    [ProducesResponseType<IReadOnlyList<ChannelPerformance>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IReadOnlyList<ChannelPerformance>> Channels(DateOnly? from, DateOnly? to, int? storeId, CancellationToken cancellationToken)
    {
        var filter = new KpiFilter(await kpis.ResolvePeriodAsync(from, to, cancellationToken), storeId);
        return Cached(await kpis.ChannelsAsync(filter, cancellationToken));
    }

    private T Cached<T>(CachedResult<T> result)
    {
        Response.Headers["X-Cache"] = result.FromCache ? "HIT" : "MISS";
        return result.Value;
    }
}
