
//PEDIR PRA SLA OQ EXPLICAR TD DPS E REFATORAR LOGO



using Context;
using Microsoft.EntityFrameworkCore;
using Repository;
using InterfaceRepository;
using Service;
using InterfaceService;

var builder = WebApplication.CreateBuilder(args);

// Obtém a connection string APENAS do appsettings.json
var connectionString = builder.Configuration.GetConnectionString("PostgreSQL");

if (string.IsNullOrEmpty(connectionString))
{
    throw new Exception("""
        ConnectionString não configurada no appsettings.json. Verifique:
        Formato esperado: 
        "ConnectionStrings": {
          "PostgreSQL": "Host=...;Port=...;Database=...;Username=...;Password=...;SSL Mode=Require;Trust Server Certificate=true"
        }
        """);
}

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddScoped<UrlRepository>();
builder.Services.AddScoped<IUrlRepository, UrlRepository>();
builder.Services.AddScoped<IUrlService, UrlService>();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend",
        policy => policy
            .WithOrigins("http://localhost:3000")
            .AllowAnyHeader()
            .AllowAnyMethod());
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        logger.LogInformation("Applying migrations...");
        db.Database.Migrate();
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Migration Fail");
        throw;
    }
}

app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpsRedirection();
app.UseCors("AllowFrontend");
app.MapControllers();

app.Run();