using Serilog;
using TwseRevenue.Api.Middleware;
using TwseRevenue.Application.Abstractions;
using TwseRevenue.Application.Behaviors;
using TwseRevenue.Application.Commands.CreateRevenue;
using TwseRevenue.Application.Commands.CreateQuote;
using TwseRevenue.Application.Queries.GetRevenueByCompanyCode;
using TwseRevenue.Application.Queries.RankQuotes;
using TwseRevenue.Application.Queries.SearchCompanies;
using TwseRevenue.Application.Mapping;
using MediatR;
using TwseRevenue.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// 結構化日誌（以 Serilog 為主）
builder.Host.UseSerilog((context, configuration) => configuration
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .WriteTo.Console());

// 例外處理改由 ExceptionHandlingMiddleware 負責（見下方 pipeline），不再用 MVC ExceptionFilter。
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 前後端分離：開發時前端（Vite dev server）與 API 不同來源，需放行 CORS。
// 來源清單由設定檔控制，未設定時預設只放行本機 Vite。
const string DevCorsPolicy = "DevCors";
var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? new[] { "http://localhost:5173" };
builder.Services.AddCors(options => options.AddPolicy(DevCorsPolicy, policy => policy
    .WithOrigins(corsOrigins)
    .AllowAnyHeader()
    .AllowAnyMethod()));

// MediatR：自 Application 組件註冊所有 Query/Command Handler，並掛上驗證 pipeline 行為。
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(RevenueMapping).Assembly);
    cfg.AddOpenBehavior(typeof(ValidationBehavior<,>)); // 驗證接縫：所有 Request 進 handler 前先過驗證
});

// 輸入驗證器（每個需驗證的 Request 註冊一個 IValidator；未驗證的 Request 自動跳過）。
builder.Services.AddScoped<IValidator<CreateRevenueCommand>, CreateRevenueValidator>();
builder.Services.AddScoped<IValidator<GetRevenueByCompanyCodeQuery>, GetRevenueByCompanyCodeValidator>();
builder.Services.AddScoped<IValidator<SearchCompaniesQuery>, SearchCompaniesValidator>();
builder.Services.AddScoped<IValidator<CreateQuoteCommand>, CreateQuoteValidator>();
builder.Services.AddScoped<IValidator<RankQuotesQuery>, RankQuotesValidator>();

// 資料存取。連線字串不入庫：本機開發由 ./dev.sh 從 .env 注入環境變數
// ConnectionStrings__TwseRevenue；SIT/UAT/PROD 由 CI/CD 或 Secret Manager 注入同名變數。
var connectionString = builder.Configuration.GetConnectionString("TwseRevenue")
    ?? throw new InvalidOperationException(
        "缺少連線字串 ConnectionStrings:TwseRevenue。\n" +
        "  本機開發：cp .env.example .env 填入密碼後，用 ./dev.sh 啟動（會自動注入）。\n" +
        "  其他環境：以環境變數 ConnectionStrings__TwseRevenue 注入。");
builder.Services.AddSingleton<ISqlConnectionFactory>(new SqlConnectionFactory(connectionString));
builder.Services.AddScoped<IRevenueRepository, RevenueRepository>();
builder.Services.AddScoped<IQuoteRepository, QuoteRepository>();

var app = builder.Build();

// Pipeline 順序（皆為無狀態 middleware，可水平擴充）：
// 1) CorrelationId：最外層，指派/沿用追蹤 ID 並推入 LogContext，讓後續所有日誌帶同一 ID
// 2) ExceptionHandling：包住下游，統一捕捉並以 log code 記錄、回傳 traceId
// 3) SecurityHeaders：補上基礎安全回應標頭
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<SecurityHeadersMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseCors(DevCorsPolicy); // 僅開發環境放行跨來源前端
}

app.UseSerilogRequestLogging();
app.MapControllers();

app.Run();

// 供整合測試（WebApplicationFactory）使用
public partial class Program
{
}
