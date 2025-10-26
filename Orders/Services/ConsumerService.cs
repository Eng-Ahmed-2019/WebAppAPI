using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Orders.Services
{
    public class ConsumerService : BackgroundService
    {
        private readonly IConfiguration _configuration;

        public ConsumerService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var hostName = _configuration["RabbitMQ:HostName"] ?? "localhost";
            var userName = _configuration["RabbitMQ:UserName"] ?? "guest";
            var password = _configuration["RabbitMQ:Password"] ?? "guest";
            var queueName = _configuration["RabbitMQ:QueueName"] ?? "hello";

            var factory = new ConnectionFactory
            {
                HostName = hostName,
                UserName = userName,
                Password = password
            };

            using var connection = await factory.CreateConnectionAsync();
            using var channel = await connection.CreateChannelAsync();

            await channel.QueueDeclareAsync(
                queue: queueName,
                durable: false,
                exclusive: false,
                autoDelete: false,
                arguments: null
            );

            Console.WriteLine($" [*] Waiting for messages from queue \"{queueName}\"...");

            var consumer = new AsyncEventingBasicConsumer(channel);
            consumer.ReceivedAsync += async (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                Console.WriteLine($" [Orders] Received: {message}");
                await Task.Yield();
            };

            await channel.BasicConsumeAsync(
                queue: queueName,
                autoAck: true,
                consumer: consumer
            );

            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
    }
}