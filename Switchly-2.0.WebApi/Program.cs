using Microsoft.EntityFrameworkCore;
using Switchly_2._0.WebApi.Context;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("Database");

builder.Services.AddDbContext<SwitchlyDbContext>(opt =>
{
    opt.UseNpgsql(connectionString);
});

var app = builder.Build();

app.Run();