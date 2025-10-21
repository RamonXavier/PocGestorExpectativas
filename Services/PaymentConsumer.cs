using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PocGestorExpectativas.Data;
using PocGestorExpectativas.Models;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace PocGestorExpectativas.Services;

public class PaymentConsumer : BackgroundService
{
    private readonly ILogger<PaymentConsumer> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly RabbitMQSettings _rabbitMQSettings;
    private IConnection? _connection;
    private IModel? _channel;

    public PaymentConsumer(
        ILogger<PaymentConsumer> logger,
        IServiceProvider serviceProvider,
        IOptions<RabbitMQSettings> rabbitMQSettings)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _rabbitMQSettings = rabbitMQSettings.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("PaymentConsumer iniciado");

        try
        {
            await ConnectToRabbitMQ();
            await StartConsuming(stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro no PaymentConsumer");
        }
    }

    private Task ConnectToRabbitMQ()
    {
        try
        {
            var factory = new ConnectionFactory
            {
                Uri = new Uri(_rabbitMQSettings.ConnectionString)
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            // Declarar a fila
            _channel.QueueDeclare(
                queue: _rabbitMQSettings.QueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            _logger.LogInformation("Conectado ao RabbitMQ - Fila: {QueueName}", _rabbitMQSettings.QueueName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao conectar com RabbitMQ");
            throw;
        }
        
        return Task.CompletedTask;
    }

    private async Task StartConsuming(CancellationToken stoppingToken)
    {
        if (_channel == null)
        {
            _logger.LogError("Canal RabbitMQ não inicializado");
            return;
        }

        var consumer = new EventingBasicConsumer(_channel);
        
        consumer.Received += async (model, ea) =>
        {
            try
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                
                _logger.LogInformation("Mensagem recebida: {Message}", message);

                await ProcessPaymentMessage(message);
                
                // Confirmar processamento
                _channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar mensagem");
                
                // Rejeitar mensagem em caso de erro
                _channel.BasicNack(deliveryTag: ea.DeliveryTag, multiple: false, requeue: true);
            }
        };

        _channel.BasicConsume(
            queue: _rabbitMQSettings.QueueName,
            autoAck: false,
            consumer: consumer);

        _logger.LogInformation("Consumidor iniciado - Aguardando mensagens...");

        // Manter o serviço rodando
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }
    }

    private async Task ProcessPaymentMessage(string message)
    {
        try
        {
            var paymentMessage = JsonSerializer.Deserialize<PaymentMessage>(message);
            if (paymentMessage == null)
            {
                _logger.LogWarning("Mensagem inválida recebida: {Message}", message);
                return;
            }

            _logger.LogInformation("Processando pagamento: {Beneficiary} - R$ {Value}", 
                paymentMessage.BeneficiaryName, paymentMessage.Value);

            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            Payment processedPayment;

            // Verificar se já existe um pagamento com a mesma linha digitável
            var existingPayment = await context.Payments
                .FirstOrDefaultAsync(p => p.IdentificationField == paymentMessage.IdentificationField);

            if (existingPayment != null)
            {
                // Atualizar pagamento existente
                existingPayment.Value = paymentMessage.Value;
                existingPayment.DueDate = paymentMessage.DueDate;
                existingPayment.BeneficiaryName = paymentMessage.BeneficiaryName;
                existingPayment.Pago = true; // Marcar como pago
                existingPayment.PaidAt = DateTime.UtcNow;
                existingPayment.UpdatedAt = DateTime.UtcNow;

                processedPayment = existingPayment;
                _logger.LogInformation("Pagamento atualizado: {PaymentId}", existingPayment.Id);
            }
            else
            {
                // Criar novo pagamento
                var payment = new Payment
                {
                    Id = Guid.NewGuid(),
                    IdentificationField = paymentMessage.IdentificationField,
                    Value = paymentMessage.Value,
                    DueDate = paymentMessage.DueDate,
                    BeneficiaryName = paymentMessage.BeneficiaryName,
                    Pago = true, // Marcar como pago
                    PaidAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                context.Payments.Add(payment);
                processedPayment = payment;
                _logger.LogInformation("Novo pagamento criado: {PaymentId}", payment.Id);
            }

            await context.SaveChangesAsync();

            // Log de auditoria
            var auditLog = new AuditLog
            {
                Id = Guid.NewGuid(),
                PaymentId = processedPayment.Id,
                Action = "payment_received",
                Details = $"Pagamento processado via RabbitMQ: {paymentMessage.BeneficiaryName} - R$ {paymentMessage.Value:F2}",
                Timestamp = DateTime.UtcNow
            };

            context.AuditLogs.Add(auditLog);
            await context.SaveChangesAsync();

            // Analisar expectativa com IA
            var expectationAnalyzer = scope.ServiceProvider.GetRequiredService<ExpectationAnalyzer>();
            await expectationAnalyzer.AnalyzeAndCreateExpectationAsync(processedPayment);

            _logger.LogInformation("Pagamento processado com sucesso: {Beneficiary}", paymentMessage.BeneficiaryName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao processar mensagem de pagamento: {Message}", message);
            throw;
        }
    }

    public override void Dispose()
    {
        _channel?.Close();
        _connection?.Close();
        base.Dispose();
    }
}