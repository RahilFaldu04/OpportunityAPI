using Microsoft.AspNetCore.Mvc;
using MyWeatherAgent;
using MyWeatherAgent.Plugins;
using OpportunitiesApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Load Dataverse settings from appsettings.json
builder.Services.Configure<DataverseSettings>(builder.Configuration.GetSection("DataverseSettings"));
builder.Services.AddHttpClient();

// Register helpers/services
builder.Services.AddScoped<AuthHelper>();
builder.Services.AddScoped<Dataverse>();

// Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Enable CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod());
});

var app = builder.Build();

app.UseCors();
app.UseSwagger();
app.UseSwaggerUI();

// GET /opportunities
app.MapGet("/opportunities", async (
    [FromQuery] string? attributes,
    Dataverse svc) =>
{
    var attrs = string.IsNullOrEmpty(attributes)
        ? new List<string> { "name", "estimatedvalue", "estimatedclosedate" }
        : attributes.Split(',').ToList();

    var records = await svc.QueryEntityAsync("opportunity", attrs);

    var flattened = records.Select(r => new OpportunityDto
    {
        Name = r.Attributes.ContainsKey("name") ? r.Attributes["name"]?.ToString() : null,
        EstimatedValue = r.Attributes.ContainsKey("estimatedvalue") ? Convert.ToDecimal(r.Attributes["estimatedvalue"]) : (decimal?)null,
        EstimatedCloseDate = r.Attributes.ContainsKey("estimatedclosedate") ? DateTime.Parse(r.Attributes["estimatedclosedate"].ToString()) : (DateTime?)null
    });
    return Results.Ok(flattened);
});

// GET /opportunities/card
app.MapGet("/opportunities/card", async (
    [FromQuery] string? attributes,
    Dataverse svc) =>
{
    var attrs = string.IsNullOrEmpty(attributes)
        ? new List<string> { "name", "estimatedvalue", "estimatedclosedate" }
        : attributes.Split(',').ToList();

    var records = await svc.QueryEntityAsync("opportunity", attrs);
    var card = svc.CreateRecordCard(records);
    return Results.Text(card, "application/json");
});

app.Run();
