using System.Reflection;
using Carter;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Switchly_2._0.WebApi.Behaviors;
using Switchly_2._0.WebApi.Context;
using Switchly_2._0.WebApi.Extensions;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("Database");

builder.Services.AddAuth(builder.Configuration);

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
app.MapCarter();

app.Run();