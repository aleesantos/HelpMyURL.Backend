using Context;
using Microsoft.EntityFrameworkCore;
using Repository;
using InterfaceRepository;
using DotNetEnv;

var builder = WebApplication.CreateBuilder(args);

// ==================== CONFIGURA��ES INICIAIS ====================
if (builder.Environment.IsDevelopment())
{
    Env.Load(); // Carrega vari�veis do .env
}

// ==================== BANCO DE DADOS ====================
var connectionString = builder.Configuration.GetConnectionString("PostgreSQL")
    ?? Environment.GetEnvironmentVariable("ConnectionStrings__PostgreSQL");

if (string.IsNullOrEmpty(connectionString))
{
    throw new Exception("""
        FALHA: ConnectionString n�o configurada. Verifique:
        1. Vari�vel 'ConnectionStrings__PostgreSQL' no Railway
        2. Arquivo .env em desenvolvimento
        Formato esperado: Host=...;Port=...;Database=...;Username=...;Password=...
        """);
}

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

// ==================== SERVI�OS ====================
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

// ==================== APLICA��O ====================
var app = builder.Build();

// ==================== MIGRA��ES (AP�S o Build) ====================
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        logger.LogInformation("Verificando migra��es pendentes...");
        if (db.Database.GetPendingMigrations().Any())
        {
            logger.LogInformation("Aplicando migra��es...");
            db.Database.Migrate();
            logger.LogInformation("Migra��es conclu�das!");
        }
        else
        {
            logger.LogInformation("Nenhuma migra��o pendente.");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "ERRO: Falha ao aplicar migra��es. ConnectionString usada: {ConnectionString}",
            connectionString.Replace("Password=.*;", "Password=******;"));
        throw;
    }
}

// ==================== PIPELINE ====================
app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpsRedirection();
app.UseCors("AllowFrontend");
app.MapControllers();

app.Run();