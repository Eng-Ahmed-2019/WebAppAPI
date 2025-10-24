using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Orders.Services
{
    public class RabbitMqConsumerService : BackgroundService
    {
        private readonly IConfiguration _configuration;
        private readonly IConnection _connection;
        private readonly IModel _channel;

        public RabbitMqConsumerService(IConfiguration configuration)
        {
            _configuration = configuration;
            InitializeRabbitMq();
        }

        private void InitializeRabbitMq()
        {
            var factory = new ConnectionFactory
            {
                HostName = _configuration["RabbitMQ:Host"] ?? "localhost",
                Port = int.Parse(_configuration["RabbitMQ:Port"] ?? "5672"),
                UserName = _configuration["RabbitMQ:Username"] ?? "guest",
                Password = _configuration["RabbitMQ:Password"] ?? "guest"
            };

            _connection = factory.CreateConnectionAsync().GetAwaiter().GetResult();
            _channel = _connection.CreateModel();

            _channel.ExchangeDeclare(
                exchange: _configuration["RabbitMQ:ExchangeName"],
                type: ExchangeType.Direct,
                durable: true
            );

            _channel.QueueDeclare(
                queue: _configuration["RabbitMQ:QueueName"],
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null
            );

            _channel.QueueBind(
                queue: _configuration["RabbitMQ:QueueName"],
                exchange: _configuration["RabbitMQ:ExchangeName"],
                routingKey: _configuration["RabbitMQ:RoutingKey"]
            );
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += (sender, eventArgs) =>
            {
                var body = eventArgs.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);

                System.Console.WriteLine($"Received message from product service: \"{message}\"");
            };

            _channel.BasicConsume(
                queue: _configuration["RabbitMQ:QueueName"],
                autoAck: true,
                consumer: consumer
            );

            return Task.CompletedTask;
        }

        public override void Dispose()
        {
            _channel?.Dispose();
            _connection?.Dispose();
            base.Dispose();
        }
    }
}