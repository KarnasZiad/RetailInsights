using Microsoft.AspNetCore.Mvc;
using RetailInsights.Core.Sales;

namespace RetailInsights.Api.Controllers;

/// <summary>Sales ingestion (tills, e-commerce).</summary>
[ApiController]
[Route("api/sales")]
[Produces("application/json")]
public sealed class SalesController(SalesIngestionService ingestion) : ControllerBase
{
    /// <summary>Records a ticket and invalidates the cached indicators.</summary>
    /// <param name="input">Ticket: store, date, channel and lines (prices come from the product catalogue).</param>
    /// <param name="cancellationToken">Request cancellation.</param>
    /// <response code="201">Ticket recorded.</response>
    /// <response code="422">Unknown store or product, invalid quantity or discount.</response>
    [HttpPost]
    [ProducesResponseType<SaleReceipt>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Post(SaleInput input, CancellationToken cancellationToken)
    {
        var sale = await ingestion.IngestAsync(input, cancellationToken);
        return Created($"/api/sales/{sale.Id}", new SaleReceipt(sale.Id, sale.TicketNumber, sale.BusinessDate, sale.TotalAmount, sale.TotalUnits));
    }
}

/// <summary>Recorded ticket.</summary>
public sealed record SaleReceipt(long Id, string TicketNumber, DateOnly BusinessDate, decimal TotalAmount, int TotalUnits);
