using Context;
using Microsoft.EntityFrameworkCore;
using Repository;
using InterfaceRepository;
using Service;
using InterfaceService;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("PostgreSQL");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("https://hmyurl.vercel.app")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.AddScoped<IUrlRepository, UrlRepository>();
builder.Services.AddScoped<UrlService, UrlService>();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.Use(async (context, next) =>
{
    context.Request.Scheme = "https"; // For�a HTTPS
    await next();
});

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

app.UseRouting();
app.UseSwagger();
app.UseSwaggerUI();
app.UseCors("AllowFrontend");
app.UseAuthorization();
app.UseHttpsRedirection();
app.MapControllers();

app.Run();