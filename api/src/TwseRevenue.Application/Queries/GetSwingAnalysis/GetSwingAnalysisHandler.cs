using MediatR;
using Microsoft.Extensions.Logging;
using TwseRevenue.Application.Abstractions;
using TwseRevenue.Application.Analysis;
using TwseRevenue.Application.Contracts;
using TwseRevenue.Application.Logging;

namespace TwseRevenue.Application.Queries.GetSwingAnalysis;

// 驗證已上移至 GetSwingAnalysisValidator。handler 取序列後交給純函式 SwingAnalyzer。
public sealed class GetSwingAnalysisHandler : IRequestHandler<GetSwingAnalysisQuery, SwingAnalysisDto>
{
    private readonly IQuoteRepository _repository;
    private readonly ILogger<GetSwingAnalysisHandler> _logger;

    public GetSwingAnalysisHandler(IQuoteRepository repository, ILogger<GetSwingAnalysisHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<SwingAnalysisDto> Handle(GetSwingAnalysisQuery request, CancellationToken cancellationToken)
    {
        var series = await _repository.GetSeriesAsync(request.CompanyCode, cancellationToken);
        var result = SwingAnalyzer.Analyze(request.CompanyCode, series);

        _logger.LogInformation(TwseLogCodes.Quote.SwingAnalyzed +
            " 波段分析完成 - CompanyCode={CompanyCode}, Days={Days}, Swings={Swings}",
            request.CompanyCode, result.Days, result.Swings.Count);

        return result;
    }
}
