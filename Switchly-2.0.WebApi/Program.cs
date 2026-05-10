using System.Reflection;
using Carter;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Switchly_2._0.WebApi.Behaviors;
using Switchly_2._0.WebApi.Context;
using Switchly_2._0.WebApi.Extensions;


var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("Database");

builder.Services.AddOpenTelemetry().WithTracing(configure =>
{
    configure
        .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("Switchly-2.0.WebApi"))
        .AddAspNetCoreInstrumentation()
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

var app = builder.Build();


app.UseRouting();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();

app.MapCarter(); 

app.Run();