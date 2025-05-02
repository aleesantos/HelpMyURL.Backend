using Context;
using Microsoft.EntityFrameworkCore;
using Repository;
using InterfaceRepository;
using DotNetEnv;
using System.Collections;

var builder = WebApplication.CreateBuilder(args);

// Configurações iniciais
if (builder.Environment.IsDevelopment())
{
    Console.WriteLine("Modo desenvolvimento - Carregando .env");
    Env.Load();
}

// Configuração do Banco de Dados
var connectionString = builder.Configuration.GetConnectionString("PostgreSQL")
    ?? Environment.GetEnvironmentVariable("POSTGRESQL_CONNECTION_STRING")
    ?? Environment.GetEnvironmentVariable("ConnectionStrings__PostgreSQL");

if (string.IsNullOrEmpty(connectionString))
{
    Console.WriteLine("Variáveis de ambiente disponíveis:");
    foreach (DictionaryEntry env in Environment.GetEnvironmentVariables())
    {
        Console.WriteLine($"{env.Key} = {env.Value}");
    }

    throw new Exception(
        "FALHA: ConnectionString não configurada. Verifique:\n" +
        "1. Variável 'POSTGRESQL_CONNECTION_STRING' no Railway\n" +
        "2. Arquivo .env em desenvolvimento\n" +
        "3. Formato: Host=...;Port=...;Database=...;Username=...;Password=...");
}

// Configuração do DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
{
    Console.WriteLine($"Configurando banco de dados com: {connectionString[..50]}...");
    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        npgsqlOptions.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null);
    });
});

// Serviços
builder.Services.AddScoped<IUrlRepository, UrlRepository>();
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

// Migrações
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        logger.LogInformation("Verificando migrações pendentes...");
        if (db.Database.GetPendingMigrations().Any())
        {
            logger.LogInformation("Aplicando migrações...");
            db.Database.Migrate();
            logger.LogInformation("Migrações concluídas!");
        }
        else
        {
            logger.LogInformation("Nenhuma migração pendente.");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "ERRO: Falha ao aplicar migrações");
        throw;
    }
}

// Pipeline
app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpsRedirection();
app.UseCors("AllowFrontend");
app.MapControllers();

app.Run();