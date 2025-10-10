namespace PocGestorExpectativas.Models;

public class RabbitMQSettings
{
    public string ConnectionString { get; set; } = string.Empty;
    public string QueueName { get; set; } = "faturas";
}


