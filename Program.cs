using Context;
using Microsoft.EntityFrameworkCore;
using Repository;
using InterfaceRepository;
using DotNetEnv;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Http;
using System.Collections;

var builder = WebApplication.CreateBuilder(args);

// ==================== CONFIGURAÇÃO DE AMBIENTE ====================
if (builder.Environment.IsDevelopment())
{
    Console.WriteLine("[MODO DESENVOLVIMENTO] Carregando variáveis do .env");
    try
    {
        Env.Load();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[AVISO] Erro ao carregar .env: {ex.Message}");
    }
}

// ==================== CONFIGURAÇÃO DO BANCO DE DADOS ====================
var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__PostgreSQL")
    ?? throw new InvalidOperationException("""
        ConnectionString não configurada. Verifique:
        1. Variável 'ConnectionStrings__PostgreSQL' no Railway
        2. Formato esperado: Host=...;Port=...;Database=...;Username=...;Password=...;SSL Mode=Require;Trust Server Certificate=true
        """);

// Validação adicional da connection string
if (!connectionString.Contains("Host=") || !connectionString.Contains("Password="))
{
    throw new FormatException($"Formato inválido da ConnectionString: {connectionString[..50]}...");
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

// ==================== CONFIGURAÇÃO DA API KEY ====================
var apiKey = Environment.GetEnvironmentVariable("API_KEY")
    ?? throw new InvalidOperationException("API_KEY não configurada");

builder.Services.AddSingleton(new ApiKeyConfig { Key = apiKey });

// ==================== CONFIGURAÇÃO DE SERVIÇOS ====================
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

// ==================== CONFIGURAÇÃO CORS ====================
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

// ==================== CONSTRUÇÃO DA APLICAÇÃO ====================
var app = builder.Build();

// ==================== MIGRAÇÕES DO BANCO DE DADOS ====================
ApplyDatabaseMigrations(app);

// ==================== MIDDLEWARE DE AUTENTICAÇÃO ====================
app.Use(async (context, next) =>
{
    if (!context.Request.Path.StartsWithSegments("/swagger"))
    {
        if (!context.Request.Headers.TryGetValue("X-API-KEY", out var extractedApiKey))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync("API Key não fornecida");
            return;
        }

        if (!apiKey.Equals(extractedApiKey))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync("API Key inválida");
            return;
        }
    }

    await next();
});

// ==================== CONFIGURAÇÃO DO PIPELINE ====================
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

// ==================== MÉTODOS AUXILIARES ====================
void ApplyDatabaseMigrations(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        logger.LogInformation("Verificando migrações pendentes...");

        var pendingMigrations = db.Database.GetPendingMigrations().ToList();
        if (pendingMigrations.Any())
        {
            logger.LogInformation($"Aplicando {pendingMigrations.Count} migrações...");
            db.Database.Migrate();
            logger.LogInformation("Migrações aplicadas com sucesso!");
        }
        else
        {
            logger.LogInformation("Nenhuma migração pendente.");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "ERRO CRÍTICO: Falha ao aplicar migrações");
        throw;
    }
}

public class ApiKeyConfig
{
    public string Key { get; set; } = null!;
}