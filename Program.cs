using EAV_Draft.Dto_s;
using EAV_Draft;
using EAV_Draft.Models;
using Microsoft.EntityFrameworkCore;
using EAV_Draft.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<DynamicAttributeService>();
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    try
    {
        await dbContext.Database.MigrateAsync();
        Console.WriteLine("Database migrated successfully.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"An error occurred while migrating the database: {ex.Message}");
        throw;
    }
}

app.MapPost("/users/{tenantId}/products", async (int tenantId, AddProductDto productDto, AppDbContext _context, DynamicAttributeService dynamicAttributeService) =>
{
    using var transaction = await _context.Database.BeginTransactionAsync();

    try
    {
        var product = new Product
        {
            TenantId = tenantId,
            ProductName = productDto.ProductName,
            Price = productDto.Price,
            CreatedAt = DateTime.UtcNow
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        await dynamicAttributeService.AddOrValidateDynamicAttributesAsync(tenantId, product.ProductId, productDto.DynamicAttributes);

        await transaction.CommitAsync();
        return Results.Ok(product);
    }
    catch (ArgumentException ex)
    {
        //validation error
        await transaction.RollbackAsync();
        return Results.BadRequest(ex.Message);
    }
    catch
    {
        //other errors
        await transaction.RollbackAsync();
        throw;
    }
});

app.MapGet("/products/{productId}", async (int productId, AppDbContext _context) =>
{
    var product = await _context.Products
        .FirstOrDefaultAsync(p => p.ProductId == productId);

    if (product == null)
        return Results.NotFound($"Product with ID {productId} not found.");

  
    var dynamicAttributes = await (from av in _context.AttributeValues
                                   join a in _context.Attributes on av.AttributeId equals a.AttributeId
                                   where av.ProductId == productId
                                   select new
                                   {
                                       a.Name,        
                                       a.DataType,    
                                       av.Value       
                                   }).ToListAsync();

    var response = new
    {
        product.ProductId,
        product.ProductName,
        product.Price,
        product.CreatedAt,
        DynamicAttributes = dynamicAttributes
    };

    return Results.Ok(response);
});


app.MapGet("/products", async (AppDbContext _context) =>
{
    var products = await _context.Products.ToListAsync();

    if (!products.Any())
        return Results.NotFound("No products found.");

    var productAttributes = await (from av in _context.AttributeValues
                                   join a in _context.Attributes on av.AttributeId equals a.AttributeId
                                   select new
                                   {
                                       av.ProductId,
                                       a.Name,
                                       a.DataType,
                                       av.Value
                                   }).ToListAsync();

    var result = products.Select(product => new
    {
        product.ProductId,
        product.ProductName,
        product.Price,
        product.CreatedAt,
        DynamicAttributes = productAttributes
            .Where(attr => attr.ProductId == product.ProductId)
            .Select(attr => new
            {
                attr.Name,
                attr.DataType,
                attr.Value
            }).ToList()
    });

    return Results.Ok(result);
});

app.MapPut("/products/{productId}", async (int productId, UpdateProductDto productDto, AppDbContext _context, DynamicAttributeService dynamicAttributeService) =>
{
    using var transaction = await _context.Database.BeginTransactionAsync();

    try
    {
        var product = await _context.Products.FirstOrDefaultAsync(p => p.ProductId == productId);

        if (product == null)
            return Results.NotFound($"Product with ID {productId} not found.");

        product.ProductName = productDto.ProductName ?? product.ProductName;
        product.Price = productDto.Price ?? product.Price;
        product.CreatedAt = DateTime.UtcNow; 

        _context.Products.Update(product);
        await _context.SaveChangesAsync();

        if (productDto.DynamicAttributes != null && productDto.DynamicAttributes.Any())
        {
            await dynamicAttributeService.UpdateDynamicAttributesAsync(productId, productDto.DynamicAttributes);
        }

        await transaction.CommitAsync();
        return Results.Ok(product);
    }
    catch (ArgumentException ex)
    {
        await transaction.RollbackAsync();
        return Results.BadRequest(ex.Message);
    }
    catch
    {
        await transaction.RollbackAsync();
        throw;
    }
});


app.MapDelete("/products/{productId}", async (int productId, AppDbContext _context) =>
{
    using var transaction = await _context.Database.BeginTransactionAsync();

    try
    {   
        var product = await _context.Products.FirstOrDefaultAsync(p => p.ProductId == productId);

        if (product == null)
            return Results.NotFound($"Product with ID {productId} not found.");
       
        var attributeValues = await _context.AttributeValues
            .Where(av => av.ProductId == productId)
            .ToListAsync();

        _context.AttributeValues.RemoveRange(attributeValues);
       
        _context.Products.Remove(product);
      
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return Results.Ok($"Product with ID {productId} and associated attributes were deleted successfully.");
    }
    catch
    {   
        await transaction.RollbackAsync();
        throw; 
    }
});

app.Run();

internal record WeatherForecast(DateTime Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}