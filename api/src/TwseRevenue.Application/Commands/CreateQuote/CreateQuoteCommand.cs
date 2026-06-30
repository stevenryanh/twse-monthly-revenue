using MediatR;
using TwseRevenue.Application.Contracts;

namespace TwseRevenue.Application.Commands.CreateQuote;

/// <summary>寫入（或依主鍵覆寫）一筆每日行情。</summary>
public sealed record CreateQuoteCommand(CreateQuoteRequest Data) : IRequest<Unit>;
