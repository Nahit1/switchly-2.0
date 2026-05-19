using System.Reflection;
using System.Threading.RateLimiting;
using Carter;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Switchly_2._0.WebApi.Behaviors;
using Switchly_2._0.WebApi.Context;
using Switchly_2._0.WebApi.Extensions;
using Switchly_2._0.WebApi.Observability;
using Switchly_2._0.WebApi.Services;


var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("Database");

builder.Services.AddOpenTelemetry().WithTracing(configure =>
{
    configure
        .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("Switchly-2.0.WebApi"))
        .AddAspNetCoreInstrumentation()
        // SwitchlyActivitySources.Name'i de izle — handler-level custom span'ler buradan akar.
        .AddSource(SwitchlyActivitySources.Name)
        .AddConsoleExporter()
        .AddOtlpExporter();
});

builder.Services.AddAuth(builder.Configuration);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy
            .AllowAnyOrigin() // Geliştirme aşamasında açıyoruz, production'da kısıtlaman gerekebilir
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddCarter();

builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));

builder.Services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

builder.Services.AddDbContext<SwitchlyDbContext>(opt =>
{
    opt.UseNpgsql(connectionString);
});

// Health check: orchestrator (Docker/K8s) "API ayakta mı" diye sorabilsin.
// MVP'de sadece liveness — process up + middleware pipeline çalışıyorsa Healthy döner.
// İleride DB readiness check eklenebilir (extra package: Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore).
builder.Services.AddHealthChecks();

// Rate limiting: public /api/track/* endpoint'leri kötü niyetli flood'a karşı.
// Per-IP fixed window — IP başına dakikada en fazla 120 istek (~2/sn ortalama).
// Normal SDK trafiği için fazlasıyla yeterli; abuse durumunda devre kesici.
builder.Services.AddRateLimiter(opts =>
{
    opts.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    opts.AddPolicy("track", httpContext =>
    {
        var partitionKey = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 120,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0
        });
    });
});

// 90 günden eski exposure + conversion event'lerini gece yarısı temizleyen background job.
builder.Services.AddHostedService<RetentionPolicyService>();

// Aktif rollout schedule'larını dakikada bir tarayıp süresi dolan adımları promote eden job.
builder.Services.AddHostedService<RolloutPromoterService>();

// Auto-rollback guardrail (Seviye 2): error eşiği aşılınca schedule'ı geri alır.
builder.Services.AddHostedService<RolloutGuardrailService>();

var app = builder.Build();


app.UseRouting();
app.UseCors("AllowAll");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

// Liveness/readiness endpoint — auth'suz, hızlı, DB de kontrol ediyor.
app.MapHealthChecks("/healthz");

app.MapCarter();

app.Run();
