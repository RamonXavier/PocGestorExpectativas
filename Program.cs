using Microsoft.EntityFrameworkCore;
using PocGestorExpectativas.Data;
using PocGestorExpectativas.Models;
using PocGestorExpectativas.Services;
using PocGestorExpectativas.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// Configurar Entity Framework
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configurar RabbitMQ
builder.Services.Configure<RabbitMQSettings>(builder.Configuration.GetSection("RabbitMQ"));

// Registrar o Consumer como Background Service
builder.Services.AddHostedService<PaymentConsumer>();

#region Configurar HttpClient
// Configurar HttpClient para OpenAI
builder.Services.AddHttpClient<OpenAiClient>();
// Configurar HttpClient para Groq
builder.Services.AddHttpClient<GroqClient>();
#endregion

// Configurar CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

#region injeção de dependências	
// Registrar serviços de IA - USANDO GROQ (comentar/descomentar para trocar)
builder.Services.AddScoped<ILlmClient, GroqClient>();
// builder.Services.AddScoped<ILlmClient, OpenAiClient>(); // Alternativa OpenAI

builder.Services.AddScoped<PaymentHistoryService>();
builder.Services.AddScoped<ExpectationAnalyzer>();
#endregion

var app = builder.Build();

// Configure the HTTP request pipeline.
// Swagger sempre habilitado (para desenvolvimento e demonstrações)
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

// Habilitar CORS
app.UseCors("AllowAll");

app.UseAuthorization();

app.MapControllers();

app.Run();
