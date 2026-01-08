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
var orders = new ConcurrentDictionary<int, Order>();

// Seed some initial data
orders.TryAdd(1, new Order { Id = 1, UserId = 1, ProductId = 1, Quantity = 2, TotalAmount = 199.98m, Status = "Pending" });
orders.TryAdd(2, new Order { Id = 2, UserId = 2, ProductId = 2, Quantity = 1, TotalAmount = 299.99m, Status = "Completed" });

var app = builder.Build();

// Configure the HTTP request pipeline
// Always enable Swagger JSON endpoint for gateway integration
app.UseSwagger();

app.UseHealthChecks("/health");

// Order endpoints
app.MapGet("/api/orders", () =>
{
    return Results.Ok(orders.Values);
})
.WithName("GetAllOrders")
.WithTags("Orders")
.Produces<IEnumerable<Order>>(StatusCodes.Status200OK);

app.MapGet("/api/orders/{id}", (int id) =>
{
    if (orders.TryGetValue(id, out var order))
    {
        return Results.Ok(order);
    }
    return Results.NotFound($"Order with ID {id} not found");
})
.WithName("GetOrderById")
.WithTags("Orders")
.Produces<Order>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status404NotFound);

app.MapGet("/api/orders/user/{userId}", (int userId) =>
{
    var userOrders = orders.Values.Where(o => o.UserId == userId).ToList();
    return Results.Ok(userOrders);
})
.WithName("GetOrdersByUserId")
.WithTags("Orders")
.Produces<IEnumerable<Order>>(StatusCodes.Status200OK);

app.MapPost("/api/orders", (Order order) =>
{
    if (order.Id == 0)
    {
        order.Id = orders.Count > 0 ? orders.Keys.Max() + 1 : 1;
    }
    
    if (orders.TryAdd(order.Id, order))
    {
        return Results.Created($"/api/orders/{order.Id}", order);
    }
    return Results.Conflict($"Order with ID {order.Id} already exists");
})
.WithName("CreateOrder")
.WithTags("Orders")
.Accepts<Order>("application/json")
.Produces<Order>(StatusCodes.Status201Created)
.Produces(StatusCodes.Status409Conflict);

app.MapPut("/api/orders/{id}", (int id, Order order) =>
{
    if (order.Id != id)
    {
        return Results.BadRequest("Order ID mismatch");
    }
    
    if (orders.TryGetValue(id, out var existingOrder))
    {
        orders[id] = order;
        return Results.Ok(order);
    }
    return Results.NotFound($"Order with ID {id} not found");
})
.WithName("UpdateOrder")
.WithTags("Orders")
.Accepts<Order>("application/json")
.Produces<Order>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status404NotFound)
.Produces(StatusCodes.Status400BadRequest);

app.MapDelete("/api/orders/{id}", (int id) =>
{
    if (orders.TryRemove(id, out var order))
    {
        return Results.NoContent();
    }
    return Results.NotFound($"Order with ID {id} not found");
})
.WithName("DeleteOrder")
.WithTags("Orders")
.Produces(StatusCodes.Status204NoContent)
.Produces(StatusCodes.Status404NotFound);

app.MapGet("/", () => "OrderService is running!");

app.Run();

public record Order
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

