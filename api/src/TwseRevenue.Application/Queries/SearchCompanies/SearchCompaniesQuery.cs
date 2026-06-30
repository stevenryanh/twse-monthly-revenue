using MediatR;
using TwseRevenue.Application.Contracts;

namespace TwseRevenue.Application.Queries.SearchCompanies;

/// <summary>關鍵字搜尋公司（代號前綴或名稱包含），供前端自動完成。</summary>
public sealed record SearchCompaniesQuery(string Keyword)
    : IRequest<IReadOnlyList<CompanySummaryDto>>;
