using MediatR;
using TwseRevenue.Application.Contracts;

namespace TwseRevenue.Application.Queries.GetSwingAnalysis;

/// <summary>個股波段分析（波峰/波谷、週期、出手時機與目標價推估）。</summary>
public sealed record GetSwingAnalysisQuery(string CompanyCode) : IRequest<SwingAnalysisDto>;
