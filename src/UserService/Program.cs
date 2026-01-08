using System.Collections.Concurrent;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "User Service API",
        Version = "v1",
        Description = "User Service API for managing users and generating JWT tokens"
    });
});
builder.Services.AddHealthChecks();

// In-memory data store (replace with actual database in production)
var users = new ConcurrentDictionary<int, User>();

// Seed some initial data
users.TryAdd(1, new User { Id = 1, Name = "John Doe", Email = "john.doe@example.com" });
users.TryAdd(2, new User { Id = 2, Name = "Jane Smith", Email = "jane.smith@example.com" });

var app = builder.Build();

// Configure the HTTP request pipeline
// Always enable Swagger JSON endpoint for gateway integration
app.UseSwagger();

// Only show Swagger UI in Development
if (app.Environment.IsDevelopment())
{
    app.UseSwaggerUI();
}

app.UseHealthChecks("/health");

// User endpoints
app.MapGet("/api/users", () =>
{
    return Results.Ok(users.Values);
})
.WithName("GetAllUsers")
.WithTags("Users")
.Produces<IEnumerable<User>>(StatusCodes.Status200OK);

app.MapGet("/api/users/{id}", (int id) =>
{
    if (users.TryGetValue(id, out var user))
    {
        return Results.Ok(user);
    }
    return Results.NotFound($"User with ID {id} not found");
})
.WithName("GetUserById")
.WithTags("Users")
.Produces<User>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status404NotFound);

app.MapPost("/api/users", (User user) =>
{
    if (user.Id == 0)
    {
        user.Id = users.Count > 0 ? users.Keys.Max() + 1 : 1;
    }
    
    if (users.TryAdd(user.Id, user))
    {
        return Results.Created($"/api/users/{user.Id}", user);
    }
    return Results.Conflict($"User with ID {user.Id} already exists");
})
.WithName("CreateUser")
.WithTags("Users")
.Accepts<User>("application/json")
.Produces<User>(StatusCodes.Status201Created)
.Produces(StatusCodes.Status409Conflict);

app.MapPut("/api/users/{id}", (int id, User user) =>
{
    if (user.Id != id)
    {
        return Results.BadRequest("User ID mismatch");
    }
    
    if (users.TryGetValue(id, out var existingUser))
    {
        users[id] = user;
        return Results.Ok(user);
    }
    return Results.NotFound($"User with ID {id} not found");
})
.WithName("UpdateUser")
.WithTags("Users")
.Accepts<User>("application/json")
.Produces<User>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status404NotFound)
.Produces(StatusCodes.Status400BadRequest);

app.MapDelete("/api/users/{id}", (int id) =>
{
    if (users.TryRemove(id, out var user))
    {
        return Results.NoContent();
    }
    return Results.NotFound($"User with ID {id} not found");
})
.WithName("DeleteUser")
.WithTags("Users")
.Produces(StatusCodes.Status204NoContent)
.Produces(StatusCodes.Status404NotFound);

app.MapGet("/", () => "UserService is running!");

// Token endpoints
app.MapPost("/api/token/generate", (TokenRequest request, IConfiguration configuration, ILogger<Program> logger) =>
{
    if (request == null || string.IsNullOrWhiteSpace(request.Username))
    {
        return Results.BadRequest(new { error = "Username is required" });
    }

    try
    {
        var jwtSettings = configuration.GetSection("JwtSettings");
        var secretKey = jwtSettings["SecretKey"] 
            ?? throw new InvalidOperationException("JWT SecretKey is not configured");
        
        var issuer = jwtSettings["Issuer"] ?? "UserService";
        var audience = jwtSettings["Audience"] ?? "Microservices";
        var expirationMinutes = int.Parse(jwtSettings["ExpirationMinutes"] ?? "60");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, request.UserId ?? Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Name, request.Username),
            new Claim(JwtRegisteredClaimNames.Sub, request.Username),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        // Add email claim if provided
        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            claims.Add(new Claim(ClaimTypes.Email, request.Email));
            claims.Add(new Claim(JwtRegisteredClaimNames.Email, request.Email));
        }

        // Add roles if provided
        if (request.Roles != null && request.Roles.Any())
        {
            foreach (var role in request.Roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }
        }
        else
        {
            // Default role if none provided
            claims.Add(new Claim(ClaimTypes.Role, "User"));
        }

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expirationMinutes),
            signingCredentials: credentials
        );

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        logger.LogInformation("JWT token generated for user: {Username}", request.Username);

        return Results.Ok(new TokenResponse
        {
            Token = tokenString,
            TokenType = "Bearer",
            ExpiresIn = expirationMinutes * 60, // Convert to seconds
            ExpiresAt = token.ValidTo
        });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error generating JWT token for user: {Username}", request.Username);
        return Results.StatusCode(500);
    }
})
.WithName("GenerateToken")
.WithTags("Token")
.Accepts<TokenRequest>("application/json")
.Produces<TokenResponse>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status400BadRequest)
.Produces(StatusCodes.Status500InternalServerError);

app.MapPost("/api/token/validate", (TokenValidationRequest request, IConfiguration configuration, ILogger<Program> logger) =>
{
    if (request == null || string.IsNullOrWhiteSpace(request.Token))
    {
        return Results.BadRequest(new { error = "Token is required" });
    }

    try
    {
        var jwtSettings = configuration.GetSection("JwtSettings");
        var secretKey = jwtSettings["SecretKey"] 
            ?? throw new InvalidOperationException("JWT SecretKey is not configured");
        
        var issuer = jwtSettings["Issuer"] ?? "UserService";
        var audience = jwtSettings["Audience"] ?? "Microservices";

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        
        var tokenHandler = new JwtSecurityTokenHandler();
        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = issuer,
            ValidAudience = audience,
            IssuerSigningKey = key,
            ClockSkew = TimeSpan.Zero
        };

        var principal = tokenHandler.ValidateToken(request.Token, validationParameters, out SecurityToken validatedToken);
        
        var jwtToken = validatedToken as JwtSecurityToken;
        var claims = principal.Claims.Select(c => new { c.Type, c.Value }).Cast<object>().ToList();

        return Results.Ok(new TokenValidationResponse
        {
            IsValid = true,
            Claims = claims,
            ExpiresAt = jwtToken?.ValidTo
        });
    }
    catch (SecurityTokenExpiredException)
    {
        return Results.Unauthorized();
    }
    catch (SecurityTokenInvalidSignatureException)
    {
        return Results.Unauthorized();
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error validating JWT token");
        return Results.Unauthorized();
    }
})
.WithName("ValidateToken")
.WithTags("Token")
.Accepts<TokenValidationRequest>("application/json")
.Produces<TokenValidationResponse>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status400BadRequest)
.Produces(StatusCodes.Status401Unauthorized);

app.Run();

public record User
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

// Token DTOs
public record TokenRequest
{
    public string Username { get; set; } = string.Empty;
    public string? UserId { get; set; } = Guid.NewGuid().ToString();
    public string? Email { get; set; }
    public string[]? Roles { get; set; }
}

public record TokenResponse
{
    public string Token { get; set; } = string.Empty;
    public string TokenType { get; set; } = "Bearer";
    public int ExpiresIn { get; set; }
    public DateTime ExpiresAt { get; set; }
}

public record TokenValidationRequest
{
    public string Token { get; set; } = string.Empty;
}

public record TokenValidationResponse
{
    public bool IsValid { get; set; }
    public List<object>? Claims { get; set; }
    public DateTime? ExpiresAt { get; set; }
}
