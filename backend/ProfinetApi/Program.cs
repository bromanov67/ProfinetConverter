using Microsoft.AspNetCore.Server.Kestrel.Core;
using ProfinetApi.Application.Features.Projects.Commands.CreateProject;
using ProfinetApi.Application.Interfaces;
using ProfinetApi.Application.Services;
using ProfinetApi.Domain.RepoInterfaces;
using ProfinetApi.Infrastructure.Hubs;
using ProfinetApi.Infrastructure.Repositories;
using ProfinetApi.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
     .AddJsonOptions(options =>
     {
         options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
         options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
     });

builder.Services.AddSignalR();
// 1. ƒŒ¡¿¬Àﬂ≈Ã gRPC
builder.Services.AddGrpc();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen();

builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(CreateProjectCommand).Assembly));


builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(5000, listenOptions =>
    {
        listenOptions.Protocols = HttpProtocols.Http1; // ƒÎˇ Postman
    });
    options.ListenAnyIP(5002, listenOptions =>
    {
        listenOptions.Protocols = HttpProtocols.Http2; // ƒÎˇ Python gRPC-ÒÚËÏ‡
    });
});

builder.Services.AddSingleton<IProjectRepository, InMemoryProjectRepository>();

// IEC104 Runtime
builder.Services.AddSingleton<Iec104RuntimeService>();
builder.Services.AddSingleton<IIec104RuntimeService>(x => x.GetRequiredService<Iec104RuntimeService>());
builder.Services.AddHostedService(x => x.GetRequiredService<Iec104RuntimeService>());

// 2. –≈√»—“–»–”≈Ã —≈–¬»— ”œ–¿¬À≈Õ»ﬂ PYTHON œ–Œ÷≈——ŒÃ
builder.Services.AddSingleton<IProfinetRuntimeService, ProfinetRuntimeService>();

builder.Services.AddScoped<IProfinetScannerService, ProfinetScannerService>();
builder.Services.AddScoped<IConfigurationService, ConfigurationService>();

// CORS
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

var app = builder.Build();

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

app.MapGrpcService<ProfinetGrpcService>();

app.Run();