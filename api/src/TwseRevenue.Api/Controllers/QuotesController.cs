using MediatR;
using Microsoft.AspNetCore.Mvc;
using TwseRevenue.Application.Commands.CreateQuote;
using TwseRevenue.Application.Contracts;
using TwseRevenue.Application.Queries.GetSwingAnalysis;
using TwseRevenue.Application.Queries.RankQuotes;

namespace TwseRevenue.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class QuotesController : ControllerBase
{
    private readonly IMediator _mediator;

    public QuotesController(IMediator mediator) => _mediator = mediator;

    /// <summary>寫入一筆每日行情（依主鍵 upsert，供匯入）。</summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateQuoteRequest request, CancellationToken ct)
    {
        await _mediator.Send(new CreateQuoteCommand(request), ct);
        return Created($"/api/quotes/{request.CompanyCode}/{request.TradeDate}", null);
    }

    /// <summary>
    /// 買賣投報排行：把（條件中的）股票依每元當日報酬在近期間的指標排序。
    /// q=代號/名稱片段；codes=逗號分隔代碼清單；sort=return|volatility|avg|daily；top=筆數上限；
    /// maxPrice=小資可負擔的每股價上限（最近收盤）。
    /// </summary>
    [HttpGet("ranking")]
    [ProducesResponseType(typeof(IReadOnlyList<QuoteRankingDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<QuoteRankingDto>>> Ranking(
        [FromQuery] string? q,
        [FromQuery] string? codes,
        [FromQuery] string? sort,
        [FromQuery] int top,
        [FromQuery] decimal? maxPrice,
        [FromQuery] string? dir,
        CancellationToken ct)
        => Ok(await _mediator.Send(new RankQuotesQuery(q, codes, sort, top, maxPrice, dir), ct));

    /// <summary>個股波段分析：波峰/波谷、平均週期與振幅，推估下一轉折的時間與目標價（僅供參考）。</summary>
    [HttpGet("{companyCode}/swing")]
    [ProducesResponseType(typeof(SwingAnalysisDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<SwingAnalysisDto>> Swing(string companyCode, CancellationToken ct)
        => Ok(await _mediator.Send(new GetSwingAnalysisQuery(companyCode), ct));
}
