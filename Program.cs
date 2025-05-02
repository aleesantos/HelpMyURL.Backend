using Context;
using Microsoft.EntityFrameworkCore;
using Repository;
using InterfaceRepository;
using Microsoft.OpenApi.Writers;

var builder = WebApplication.CreateBuilder(args);

// =============================================
// 1. CONFIGURAÇÃO INICIAL
// =============================================
if (builder.Environment.IsDevelopment())
{
    DotNetEnv.Env.Load();
}

// =============================================
// 2. CONFIGURAÇÃO DO BANCO DE DADOS (REVISADO 3x)
// =============================================
var connectionString = builder.Configuration.GetConnectionString("PostgreSQL");

// Validação EXTRA robusta da connection string
if (string.IsNullOrEmpty(connectionString))
{
    // Tentativa alternativa de obter a string
    connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__PostgreSQL");

    if (string.IsNullOrEmpty(connectionString))
    {
        throw new Exception("""
            ConnectionString não configurada. Verifique:
            1. Variável 'ConnectionStrings__PostgreSQL' no Railway
            2. Arquivo .env em desenvolvimento
            3. Formato: Host=...;Port=...;Database=...;Username=...;Password=...;SSL Mode=Require;Trust Server Certificate=true
            """);
    }
}

// Configuração do DbContext com tratamento de erro
builder.Services.AddDbContext<AppDbContext>(options =>
{
    try
    {
        options.UseNpgsql(connectionString, npgsqlOptions =>
        {
            npgsqlOptions.EnableRetryOnFailure(3, TimeSpan.FromSeconds(5));
        });
    }
    catch (ArgumentException ex)
    {
        Console.WriteLine($"ERRO: String de conexão inválida. Valor: {connectionString}");
        throw new Exception("Formato inválido da string de conexão", ex);
    }
});

// =============================================
// 3. CONFIGURAÇÃO DOS SERVIÇOS (ANTES do Build)
// =============================================
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

// =============================================
// 4. CONSTRUÇÃO DA APLICAÇÃO
// =============================================
var app = builder.Build();

// =============================================
// 5. MIGRAÇÕES (COM LOGS DETALHADOS)
// =============================================
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        logger.LogInformation("""
            Iniciando migrações...
            String de conexão: {ConnectionString}
            """, connectionString);

        if (db.Database.GetPendingMigrations().Any())
        {
            db.Database.Migrate();
            logger.LogInformation("Migrações aplicadas com sucesso");
        }
        else
        {
            logger.LogInformation("Nenhuma migração pendente");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, """
            FALHA NA MIGRAÇÃO
            String usada: {ConnectionString}
            Detalhes: {ErrorMessage}
            """, connectionString, ex.Message);
        throw;
    }
}

// =============================================
// 6. CONFIGURAÇÃO DO PIPELINE
// =============================================
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.UseCors("AllowFrontend");
app.MapControllers();

app.Run();