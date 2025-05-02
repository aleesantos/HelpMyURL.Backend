using Context;
using Microsoft.EntityFrameworkCore;
using Repository;
using InterfaceRepository;
using DotNetEnv;
using System.Collections;

var builder = WebApplication.CreateBuilder(args);

// ==================== CONFIGURA��ES INICIAIS ====================
if (builder.Environment.IsDevelopment())
{
    Console.WriteLine("Modo desenvolvimento - Carregando .env");
    Env.Load();
}

// ==================== CONFIGURA��O DO BANCO DE DADOS ====================
var connectionString = builder.Configuration.GetConnectionString("PostgreSQL")
    ?? Environment.GetEnvironmentVariable("POSTGRESQL_CONNECTION_STRING")
    ?? Environment.GetEnvironmentVariable("ConnectionStrings__PostgreSQL");

// Valida��o robusta da connection string
if (string.IsNullOrEmpty(connectionString))
{
    Console.WriteLine("Vari�veis de ambiente dispon�veis:");
    foreach (DictionaryEntry env in Environment.GetEnvironmentVariables())
    {
        Console.WriteLine($"{env.Key} = {env.Value}");
    }

    throw new Exception("""
        FALHA: ConnectionString n�o configurada corretamente. Verifique:
        1. Vari�vel 'POSTGRESQL_CONNECTION_STRING' no Railway
        2. Arquivo .env em desenvolvimento (formato: POSTGRESQL_CONNECTION_STRING=Host=...;Port=...;...)
        3. Formato esperado: Host=servidor;Port=5432;Database=nome_db;Username=usuario;Password=senha
        String usada: """ + connectionString);
}

// Configura��o do DbContext com tratamento de erros
builder.Services.AddDbContext<AppDbContext>(options =>
{
    try
    {
        Console.WriteLine($"Configurando banco de dados com: {connectionString.Replace("Password=.*;", "Password=******;")}");
        options.UseNpgsql(connectionString, npgsqlOptions =>
        {
            npgsqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(10),
                errorCodesToAdd: null);
        });
    }
    catch (Exception ex)
    {
        Console.WriteLine($"ERRO: Falha ao configurar o banco de dados. Detalhes: {ex.Message}");
        throw;
    }
});

// ==================== CONFIGURA��O DOS SERVI�OS ====================
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

// ==================== CONSTRU��O DA APLICA��O ====================
var app = builder.Build();

// ==================== APLICA��O DE MIGRA��ES ====================
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        logger.LogInformation("Verificando migra��es pendentes...");
        var pendingMigrations = db.Database.GetPendingMigrations().ToList();

        if (pendingMigrations.Any())
        {
            logger.LogInformation($"Migra��es pendentes: {string.Join(", ", pendingMigrations)}");
            logger.LogInformation("Aplicando migra��es...");
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
        logger.LogError(ex, """
            FALHA NA MIGRA��O
            ConnectionString usada: {ConnectionString}
            Detalhes do erro: {ErrorMessage}
            """,
            connectionString.Replace("Password=.*;", "Password=******;"),
            ex.Message);
        throw;
    }
}

// ==================== CONFIGURA��O DO PIPELINE ====================
app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpsRedirection();
app.UseCors("AllowFrontend");
app.MapControllers();

app.Run();