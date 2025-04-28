using Context;
using Microsoft.EntityFrameworkCore;
using Repository;
using InterfaceRepository;

var builder = WebApplication.CreateBuilder(args);

if(builder.Environment.IsDevelopment())
{
    DotNetEnv.Env.Load();
}

var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__PostgreSQL");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

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

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.UseCors("AllowFrontend");
app.MapControllers();

app.Run();