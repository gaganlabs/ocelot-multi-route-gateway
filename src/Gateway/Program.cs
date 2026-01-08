using Gateway.Configurators;
using Gateway.Extensions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MMLib.SwaggerForOcelot.DependencyInjection;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Ocelot.Provider.Polly;
using Serilog;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
// Note: SwaggerGen is configured later for JWT security - SwaggerForOcelot handles the main Swagger setup

// Configure JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"] ?? "YourSuperSecretKeyThatShouldBeAtLeast32CharactersLong!";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"] ?? "Gateway",
        ValidAudience = jwtSettings["Audience"] ?? "Microservices",
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
    };
});

var routes = "routes";

builder.Configuration.AddOcelotWithSwaggerSupport(options =>
{
    options.Folder = routes;
});

// Add Ocelot services after configuration is loaded
builder.Services.AddOcelot(builder.Configuration)
    .AddPolly();

builder.Services.AddSwaggerForOcelot(builder.Configuration);

// Add Ocelot - Load all configuration files first
builder.Configuration.SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("ocelot.json", optional: false, reloadOnChange: true)
    .AddOcelot("routes", builder.Environment, mergeTo: MergeOcelotJson.ToMemory, optional: false, reloadOnChange: true)
    .ResolveDownstreamHostPlaceholders(builder.Configuration, builder.Services)
    .AddEnvironmentVariables();

// Add CORS
builder.Services.AddCors(options =>     
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Add Health Checks
builder.Services.AddHealthChecks();

var app = builder.Build();

// Configure the HTTP request pipeline
// Note: UseSwaggerForOcelotUI replaces the standard Swagger UI
// Do not use app.UseSwagger() and app.UseSwaggerUI() when using SwaggerForOcelot

app.UseSerilogRequestLogging();

app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

app.UseHealthChecks("/health");

// Configure Ocelot middleware - must be before MapControllers and MapGet
app.UseSwaggerForOcelotUI(options =>
{
    options.PathToSwaggerGenerator = "/swagger/docs";
    options.ReConfigureUpstreamSwaggerJson = AlterUpstream.ConfigureUpstreamSecurity;
}, uiOptions =>
{
    uiOptions.DefaultModelsExpandDepth(-1);
});

app.UseOcelot(OcelotPipelineConfigurator.CreatePipelineConfiguration()).Wait();

app.MapControllers();

app.MapGet("/", () => "Ocelot API Gateway is running!");

try
{
    Log.Information("Starting Ocelot API Gateway");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application start-up failed");
}
finally
{
    Log.CloseAndFlush();
}

