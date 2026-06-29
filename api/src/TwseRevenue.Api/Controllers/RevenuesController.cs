using MediatR;
using Microsoft.AspNetCore.Mvc;
using TwseRevenue.Application.Commands.CreateRevenue;
using TwseRevenue.Application.Contracts;
using TwseRevenue.Application.Queries.GetRevenueByCompanyCode;

namespace TwseRevenue.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class RevenuesController : ControllerBase
{
    private readonly IMediator _mediator;

    public RevenuesController(IMediator mediator) => _mediator = mediator;

    /// <summary>以公司代號查詢各月營收（最新月份在前）。</summary>
    [HttpGet("{companyCode}")]
    [ProducesResponseType(typeof(IReadOnlyList<MonthlyRevenueDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<MonthlyRevenueDto>>> Get(string companyCode, CancellationToken ct)
        => Ok(await _mediator.Send(new GetRevenueByCompanyCodeQuery(companyCode), ct));

    /// <summary>新增一筆營收資料（依主鍵 upsert）。</summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateRevenueRequest request, CancellationToken ct)
    {
        await _mediator.Send(new CreateRevenueCommand(request), ct);
        return CreatedAtAction(nameof(Get), new { companyCode = request.CompanyCode }, null);
    }
}
