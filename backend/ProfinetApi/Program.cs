using ProfinetApi.Application.Features.Projects.Commands.CreateProject;
using ProfinetApi.Application.Services;
using ProfinetApi.Domain.RepoInterfaces;
using ProfinetApi.Infrastructure.Repositories;
using ProfinetApi.Infrastructure.Services;
using ProfinetApi.Infrastructure.Hubs;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers()
     .AddJsonOptions(options =>
     {
         options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
         options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
     });
builder.Services.AddSignalR();
builder.Services.AddEndpointsApiExplorer();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen();

// Подключаем MediatR
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(CreateProjectCommand).Assembly));

builder.Services.AddSingleton<IProjectRepository, InMemoryProjectRepository>();

builder.Services.AddSingleton<IIec104RuntimeService, Iec104RuntimeService>();
builder.Services.AddHostedService<Iec104RuntimeService>(provider =>
    (Iec104RuntimeService)provider.GetRequiredService<IIec104RuntimeService>());

builder.Services.AddSingleton<IProfinetRuntimeService, ProfinetRuntimeService>();

//CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowVueApp", policy =>
    {
        policy.WithOrigins("http://localhost:5173", "http://localhost:3000")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});


// Application Services
builder.Services.AddScoped<IConfigurationService, ConfigurationService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowVueApp");

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();
app.MapHub<RuntimeHub>("/runtimeHub");
app.Run("http://localhost:5000");
