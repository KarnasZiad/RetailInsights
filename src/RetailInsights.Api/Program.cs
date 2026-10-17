using System.Reflection;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.OpenApi.Models;
using RetailInsights.Api;
using RetailInsights.Core.Kpis;
using RetailInsights.Core.Sales;
using RetailInsights.Infrastructure;
using RetailInsights.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<KpiService>();
builder.Services.AddScoped<SalesIngestionService>();

builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "RetailInsights API",
        Version = "v1",
        Description = "Indicateurs de performance des ventes d'un reseau de magasins : chiffre d'affaires, panier moyen, "
            + "indice de vente, top produits, canaux. Les reponses sont mises en cache dans Redis (en-tete X-Cache: HIT | MISS).",
    });
    var xmlDocs = Path.Combine(AppContext.BaseDirectory, $"{Assembly.GetExecutingAssembly().GetName().Name}.xml");
    if (File.Exists(xmlDocs))
    {
        options.IncludeXmlComments(xmlDocs);
    }
    options.SupportNonNullableReferenceTypes();
});

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
    policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod().WithExposedHeaders("X-Cache")));

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseCors();
app.UseSwagger();
app.UseSwaggerUI(options => options.DocumentTitle = "RetailInsights API");

app.MapControllers();
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") });
app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();

await using (var scope = app.Services.CreateAsyncScope())
{
    await scope.ServiceProvider.GetRequiredService<DatabaseInitializer>().InitializeAsync();
}

await app.RunAsync();

/// <summary>Entry point, exposed for <c>WebApplicationFactory</c>.</summary>
public partial class Program
{
}
