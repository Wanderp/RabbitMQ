using System;
using System.IO;
using System.Text;
using System.Threading;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace MessageProcessor
{
    public static class Program
    {
        private static IConfiguration _configuration;
        private static readonly AutoResetEvent _waitHandle = new AutoResetEvent(false);

        public static void Main()
        {
            var builder = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile($"appsettings.json");
            _configuration = builder.Build();

            var rabbitMQConfigurations = new RabbitMQConfigurations();
            new ConfigureFromConfigurationOptions<RabbitMQConfigurations>(
                _configuration.GetSection("RabbitMQConfigurations"))
                    .Configure(rabbitMQConfigurations);

            var factory = GenerateConnectionFactory(rabbitMQConfigurations);

            using var connection = factory.CreateConnection();
            using var channel = connection.CreateModel();
            DeclareQueue(channel);

            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += ConsumerReceived;
            channel.BasicConsume(queue: "Messages",
                 autoAck: true,
                 consumer: consumer);

            Console.WriteLine("Waiting new messages for processment");

            Console.CancelKeyPress += (o, e) =>
            {
                Console.WriteLine("Going out");

                _waitHandle.Set();
                e.Cancel = true;
            };

            _waitHandle.WaitOne();
        }

        private static ConnectionFactory GenerateConnectionFactory(RabbitMQConfigurations configurations)
        {
            return new ConnectionFactory()
            {
                HostName = configurations.HostName,
                Port = configurations.Port,
                UserName = configurations.UserName,
                Password = configurations.Password
            };
        }

        private static void DeclareQueue(IModel channel)
        {
            channel.QueueDeclare(queue: "Messages",
                                         durable: false,
                                         exclusive: false,
                                         autoDelete: false,
                                         arguments: null);
        }

        private static void ConsumerReceived(
            object sender, BasicDeliverEventArgs e)
        {
            var message = Encoding.UTF8.GetString(e.Body);
            Console.WriteLine(Environment.NewLine +
                "[New message received] " + message);
        }
    }
}