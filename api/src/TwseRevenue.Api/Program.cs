using Serilog;
using TwseRevenue.Api.Filters;
using TwseRevenue.Application.Abstractions;
using TwseRevenue.Application.Mapping;
using TwseRevenue.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// 結構化日誌（比照 團隊 以 Serilog 為主）
builder.Host.UseSerilog((context, configuration) => configuration
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .WriteTo.Console());

builder.Services.AddControllers(options => options.Filters.Add<GlobalExceptionFilter>());
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

// MediatR：自 Application 組件註冊所有 Query/Command Handler
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(RevenueMapping).Assembly));

// 資料存取。連線字串不入庫：本機開發由 ./dev.sh 從 .env 注入環境變數
// ConnectionStrings__TwseRevenue；SIT/UAT/PROD 由 CI/CD 或 Secret Manager 注入同名變數。
var connectionString = builder.Configuration.GetConnectionString("TwseRevenue")
    ?? throw new InvalidOperationException(
        "缺少連線字串 ConnectionStrings:TwseRevenue。\n" +
        "  本機開發：cp .env.example .env 填入密碼後，用 ./dev.sh 啟動（會自動注入）。\n" +
        "  其他環境：以環境變數 ConnectionStrings__TwseRevenue 注入。");
builder.Services.AddSingleton<ISqlConnectionFactory>(new SqlConnectionFactory(connectionString));
builder.Services.AddScoped<IRevenueRepository, RevenueRepository>();

var app = builder.Build();

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
