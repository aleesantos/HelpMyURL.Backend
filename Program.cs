using Context;
using Microsoft.EntityFrameworkCore;
using Repository;
using InterfaceRepository;
using DotNetEnv;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Http;
using System.Collections;

var builder = WebApplication.CreateBuilder(args);

// ==================== CONFIGURA��O DE AMBIENTE ====================
if (builder.Environment.IsDevelopment())
{
    Console.WriteLine("[MODO DESENVOLVIMENTO] Carregando vari�veis do .env");
    try
    {
        Env.Load();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[AVISO] Erro ao carregar .env: {ex.Message}");
    }
}

// ==================== CONFIGURA��O DO BANCO DE DADOS ====================
var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__PostgreSQL")
    ?? throw new InvalidOperationException("""
        ConnectionString n�o configurada. Verifique:
        1. Vari�vel 'ConnectionStrings__PostgreSQL' no Railway
        2. Formato esperado: Host=...;Port=...;Database=...;Username=...;Password=...;SSL Mode=Require;Trust Server Certificate=true
        """);

// Valida��o adicional da connection string
if (!connectionString.Contains("Host=") || !connectionString.Contains("Password="))
{
    throw new FormatException($"Formato inv�lido da ConnectionString: {connectionString[..50]}...");
}

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorCodesToAdd: null);
    });
});

// ==================== CONFIGURA��O DA API KEY ====================
var apiKey = Environment.GetEnvironmentVariable("API_KEY")
    ?? throw new InvalidOperationException("API_KEY n�o configurada");

builder.Services.AddSingleton(new ApiKeyConfig { Key = apiKey });

// ==================== CONFIGURA��O DE SERVI�OS ====================
builder.Services.AddScoped<IUrlRepository, UrlRepository>();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "HelpMyURL API", Version = "v1" });

    c.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
    {
        Name = "X-API-KEY",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        In = ParameterLocation.Header,
        Description = "Insira a API Key fornecida"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "ApiKey"
                }
            },
            Array.Empty<string>()
        }
    });
});

// ==================== CONFIGURA��O CORS ====================
builder.Services.AddCors(options =>
{
    options.AddPolicy("RailwayPolicy", policy =>
    {
        policy.WithOrigins(
                "http://localhost:3000",
                "https://*.railway.app")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// ==================== CONSTRU��O DA APLICA��O ====================
var app = builder.Build();

// ==================== MIGRA��ES DO BANCO DE DADOS ====================
ApplyDatabaseMigrations(app);

// ==================== MIDDLEWARE DE AUTENTICA��O ====================
app.Use(async (context, next) =>
{
    if (!context.Request.Path.StartsWithSegments("/swagger"))
    {
        if (!context.Request.Headers.TryGetValue("X-API-KEY", out var extractedApiKey))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync("API Key n�o fornecida");
            return;
        }

        if (!apiKey.Equals(extractedApiKey))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync("API Key inv�lida");
            return;
        }
    }

    await next();
});

// ==================== CONFIGURA��O DO PIPELINE ====================
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "HelpMyURL API v1");
    c.ConfigObject.DisplayRequestDuration = true;
});

app.UseHttpsRedirection();
app.UseCors("RailwayPolicy");
app.MapControllers();

app.Run();

// ==================== M�TODOS AUXILIARES ====================
void ApplyDatabaseMigrations(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        logger.LogInformation("Verificando migra��es pendentes...");

        var pendingMigrations = db.Database.GetPendingMigrations().ToList();
        if (pendingMigrations.Any())
        {
            logger.LogInformation($"Aplicando {pendingMigrations.Count} migra��es...");
            db.Database.Migrate();
            logger.LogInformation("Migra��es aplicadas com sucesso!");
        }
        else
        {
            logger.LogInformation("Nenhuma migra��o pendente.");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "ERRO CR�TICO: Falha ao aplicar migra��es");
        throw;
    }
}

public class ApiKeyConfig
{
    public string Key { get; set; } = null!;
}