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
            var hostName = _configuration["RabbitMQ:HostName"];
            var userName = _configuration["RabbitMQ:UserName"];
            var password = _configuration["RabbitMQ:Password"];
            var queueName = _configuration["RabbitMQ:QueueName"];

            var factory = new ConnectionFactory
            {
                HostName = hostName ?? "localhost",
                UserName = userName ?? "guest",
                Password = password ?? "guest"
            };

            using var connection = await factory.CreateConnectionAsync();
            using var channel = await connection.CreateChannelAsync();

            await channel.QueueDeclareAsync(queue: queueName ?? "hello",
                                            durable: false,
                                            exclusive: false,
                                            autoDelete: false,
                                            arguments: null
            );

            var consumer = new AsyncEventingBasicConsumer(channel);

            consumer.ReceivedAsync += (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                Console.WriteLine($" [Orders] Received Message: {message}");
                return Task.CompletedTask;
            };

            await channel.BasicConsumeAsync(queue: queueName ?? "hello",
                autoAck: true,
                consumer: consumer
            );
            await Task.Delay(-1, stoppingToken);
        }
    }
}