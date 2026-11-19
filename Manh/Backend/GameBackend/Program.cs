
using GameBackend.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Controllers
builder.Services.AddControllers();

// PostgreSQL + Entity Framework Core
builder.Services.AddDbContext<GameDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString(
            "DefaultConnection")));

// OpenAPI
builder.Services.AddOpenApi();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Development
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    app.UseSwagger();
    app.UseSwaggerUI();
}

// Database migrations
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider
        .GetRequiredService<GameDbContext>();

    await db.Database.MigrateAsync();

    // Thêm lại ItemDataSeeder.SeedAsync(db)
    // sau khi xác nhận namespace và chữ ký hàm.
}

app.UseHttpsRedirection();

// Đăng ký các API Controller
app.MapControllers();

app.Run();
