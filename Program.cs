using Context;
using Microsoft.EntityFrameworkCore;
using Repository;
using InterfaceRepository;
using Microsoft.OpenApi.Writers;

var builder = WebApplication.CreateBuilder(args);

if(builder.Environment.IsDevelopment())
{
    DotNetEnv.Env.Load();
}

var connectionString = builder.Configuration.GetConnectionString("PostgreSQL");

if(string.IsNullOrEmpty(connectionString))
{
    throw new Exception("ConnectionString is not configurated");
}

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

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

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.UseCors("AllowFrontend");
app.MapControllers();

app.Run();