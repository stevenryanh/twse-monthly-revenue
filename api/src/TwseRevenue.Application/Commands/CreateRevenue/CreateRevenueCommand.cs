using MediatR;
using TwseRevenue.Application.Contracts;

namespace TwseRevenue.Application.Commands.CreateRevenue;

/// <summary>新增（或依主鍵覆寫）一筆營收資料。</summary>
public sealed record CreateRevenueCommand(CreateRevenueRequest Data) : IRequest<Unit>;
