using TwseRevenue.Domain.Entities;

namespace TwseRevenue.Application.Abstractions;

/// <summary>
/// 營收資料存取介面。實作位於 Infrastructure 層（透過參數化預存程序存取）。
/// Application 只依賴此抽象，不認識任何資料庫細節。
/// </summary>
public interface IRevenueRepository
{
    Task<IReadOnlyList<MonthlyRevenue>> GetByCompanyCodeAsync(string companyCode, CancellationToken ct);

    Task UpsertAsync(MonthlyRevenue revenue, CancellationToken ct);
}
