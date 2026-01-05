using System.Collections.Concurrent;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();

// In-memory data store (replace with actual database in production)
var products = new ConcurrentDictionary<int, Product>();

// Seed some initial data
products.TryAdd(1, new Product { Id = 1, Name = "Laptop", Description = "High-performance laptop", Price = 99.99m, Stock = 50 });
products.TryAdd(2, new Product { Id = 2, Name = "Smartphone", Description = "Latest smartphone model", Price = 299.99m, Stock = 100 });
products.TryAdd(3, new Product { Id = 3, Name = "Tablet", Description = "10-inch tablet", Price = 199.99m, Stock = 75 });

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHealthChecks("/health");

// Product endpoints
app.MapGet("/api/products", () =>
{
    return Results.Ok(products.Values);
})
.WithName("GetAllProducts")
.WithTags("Products")
.Produces<IEnumerable<Product>>(StatusCodes.Status200OK);

app.MapGet("/api/products/{id}", (int id) =>
{
    if (products.TryGetValue(id, out var product))
    {
        return Results.Ok(product);
    }
    return Results.NotFound($"Product with ID {id} not found");
})
.WithName("GetProductById")
.WithTags("Products")
.Produces<Product>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status404NotFound);

app.MapPost("/api/products", (Product product) =>
{
    if (product.Id == 0)
    {
        product.Id = products.Count > 0 ? products.Keys.Max() + 1 : 1;
    }
    
    if (products.TryAdd(product.Id, product))
    {
        return Results.Created($"/api/products/{product.Id}", product);
    }
    return Results.Conflict($"Product with ID {product.Id} already exists");
})
.WithName("CreateProduct")
.WithTags("Products")
.Accepts<Product>("application/json")
.Produces<Product>(StatusCodes.Status201Created)
.Produces(StatusCodes.Status409Conflict);

app.MapPut("/api/products/{id}", (int id, Product product) =>
{
    if (product.Id != id)
    {
        return Results.BadRequest("Product ID mismatch");
    }
    
    if (products.TryGetValue(id, out var existingProduct))
    {
        products[id] = product;
        return Results.Ok(product);
    }
    return Results.NotFound($"Product with ID {id} not found");
})
.WithName("UpdateProduct")
.WithTags("Products")
.Accepts<Product>("application/json")
.Produces<Product>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status404NotFound)
.Produces(StatusCodes.Status400BadRequest);

app.MapDelete("/api/products/{id}", (int id) =>
{
    if (products.TryRemove(id, out var product))
    {
        return Results.NoContent();
    }
    return Results.NotFound($"Product with ID {id} not found");
})
.WithName("DeleteProduct")
.WithTags("Products")
.Produces(StatusCodes.Status204NoContent)
.Produces(StatusCodes.Status404NotFound);

app.MapGet("/", () => "ProductService is running!");

app.Run();

public record Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Stock { get; set; }
}

