using MediatR;
using Microsoft.AspNetCore.Mvc;
using TwseRevenue.Application.Contracts;
using TwseRevenue.Application.Queries.SearchCompanies;

namespace TwseRevenue.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class CompaniesController : ControllerBase
{
    private readonly IMediator _mediator;

    public CompaniesController(IMediator mediator) => _mediator = mediator;

    /// <summary>關鍵字搜尋公司（代號或名稱的一部分），供前端輸入時自動完成。</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<CompanySummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CompanySummaryDto>>> Search(
        [FromQuery] string q, CancellationToken ct)
        => Ok(await _mediator.Send(new SearchCompaniesQuery(q), ct));
}
