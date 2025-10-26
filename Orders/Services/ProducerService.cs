using System.Text;
using RabbitMQ.Client;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace Orders.Services
{
    public class ProducerService
    {
        private readonly IConfiguration _configuration;

        public ProducerService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendOrderCreatedMessageAsync(object message)
        {
            var hostName = _configuration["RabbitMQ:HostName"] ?? "localhost";
            var userName = _configuration["RabbitMQ:UserName"] ?? "guest";
            var password = _configuration["RabbitMQ:Password"] ?? "guest";
            var queueName = _configuration["RabbitMQ:QueueName"] ?? "product_updates";

            var factory = new ConnectionFactory
            {
                HostName = hostName,
                UserName = userName,
                Password = password,
            };

            using var connection = await factory.CreateConnectionAsync();
            using var channel = await connection.CreateChannelAsync();

            await channel.QueueDeclareAsync(queue: queueName,
                                            durable: false,
                                            exclusive: false,
                                            autoDelete: false,
                                            arguments: null
            );

            var json = JsonSerializer.Serialize(message);
            var body = Encoding.UTF8.GetBytes(json);

            await channel.BasicPublishAsync(exchange: string.Empty,
                routingKey: queueName,
                body: body
            );

            Console.WriteLine($" [Orders] Sent message to queue \"{queueName}\": \"{json}\"");
        }
    }
}