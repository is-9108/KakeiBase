using FluentValidation;
using KakeiBase.WebApi.Endpoints;
using KakeiBase.WebApi.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.AddDbContext<KakeiBaseDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapHealthEndpoint();

app.Run();

public partial class Program { }
