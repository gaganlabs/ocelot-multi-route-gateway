using Gateway.Configurators;
using Gateway.Extensions;
using MMLib.SwaggerForOcelot.DependencyInjection;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Ocelot.Provider.Polly;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Configure Swagger for Ocelot
const string routesFolder = "routes";
builder.Configuration.AddOcelotWithSwaggerSupport(options =>
{
    options.Folder = routesFolder;
});

// Add Ocelot services after configuration is loaded
builder.Services.AddOcelot(builder.Configuration)
    .AddPolly();

builder.Services.AddSwaggerForOcelot(builder.Configuration);

// Load Ocelot configuration files first
// Required: Loads the main ocelot.json file and all route files from the "routes" folder
// The AddOcelot method merges all route files into memory
// ResolveDownstreamHostPlaceholders is required to resolve placeholders like "{OrderService}" 
// from the GlobalHosts configuration in appsettings.json
builder.Configuration
    .AddJsonFile("ocelot.json", optional: false, reloadOnChange: true)
    .AddOcelot(routesFolder, builder.Environment, mergeTo: MergeOcelotJson.ToMemory, optional: false, reloadOnChange: true)
    .ResolveDownstreamHostPlaceholders(builder.Configuration, builder.Services);
// Note: AddEnvironmentVariables() is already called by default in ASP.NET Core, so it's not needed here

    // Configure Multi-Authentication Schemes (JWT Bearer and OpenID Connect)
builder.Services.AddGatewayAuthentication(builder.Configuration, builder.Environment);

// Add CORS
builder.Services.AddCors(options =>     
{
    if (builder.Environment.IsDevelopment())
    {
        options.AddPolicy("AllowAll", policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        });
    }
    else
    {
        // Production CORS should be more restrictive
        var allowedOrigins = builder.Configuration.GetSection("CorsSettings:AllowedOrigins").Get<string[]>() 
            ?? [];
        
        options.AddPolicy("AllowAll", policy =>
        {
            if (allowedOrigins.Length > 0)
            {
                policy.WithOrigins(allowedOrigins)
                      .AllowAnyMethod()
                      .AllowAnyHeader()
                      .AllowCredentials();
            }
            else
            {
                // Fallback: allow all if not configured (should be configured in production)
                policy.AllowAnyOrigin()
                      .AllowAnyMethod()
                      .AllowAnyHeader();
            }
        });
    }
});

// Add Health Checks
builder.Services.AddHealthChecks();

var app = builder.Build();

// Configure the HTTP request pipeline
// Note: UseSwaggerForOcelotUI replaces the standard Swagger UI
// Do not use app.UseSwagger() and app.UseSwaggerUI() when using SwaggerForOcelot

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

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

// Use Ocelot middleware
#pragma warning disable CS4014 // UseOcelot is synchronous, not async
app.UseOcelot(OcelotPipelineConfigurator.CreatePipelineConfiguration());
#pragma warning restore CS4014

app.MapControllers();

app.MapGet("/", () => "Ocelot API Gateway is running!");

try
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("Starting Ocelot API Gateway on {Environment}", app.Environment.EnvironmentName);
    await app.RunAsync();
}
catch (Exception ex)
{
    // Don't use logger in catch block as it may be disposed - use console directly
    Console.Error.WriteLine("================================================");
    Console.Error.WriteLine("Application start-up failed!");
    Console.Error.WriteLine("================================================");
    Console.Error.WriteLine($"Exception: {ex.GetType().Name}");
    Console.Error.WriteLine($"Message: {ex.Message}");
    if (ex.InnerException != null)
    {
        Console.Error.WriteLine($"Inner Exception: {ex.InnerException.GetType().Name}");
        Console.Error.WriteLine($"Inner Message: {ex.InnerException.Message}");
    }
    Console.Error.WriteLine($"Stack Trace: {ex.StackTrace}");
    Console.Error.WriteLine("================================================");
    throw;
}

