using System.Collections.Concurrent;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "User Service API",
        Version = "v1",
        Description = "User Service API for managing users"
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

app.Run();

public record User
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

