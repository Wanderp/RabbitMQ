using System;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using RabbitMq.Test.Api.Models;
using RabbitMQ.Client;

namespace RabbitMq.Test.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MessagesController : ControllerBase
    {
        private static readonly Counter _counter = new Counter();

        [HttpGet]
        public object Get()
        {
            return new
            {
                MessagesSended = _counter.ActualValue
            };
        }

        [HttpPost]
        public object Post(
            [FromServices]RabbitMqConfigurations configurations,
            [FromBody]Content content)
        {
            lock (_counter)
            {
                _counter.Increment();

                var factory = GenerateConnectionFactory(configurations);

                using (var connection = factory.CreateConnection())
                using (var channel = connection.CreateModel())
                {
                    DeclareQueue(channel);
                    var body = SetBodyRequest(content);
                    SendMessage(channel, body);
                }

                return new
                {
                    MessageSuccess = "Message sent"
                };
            }
        }

        private ConnectionFactory GenerateConnectionFactory(RabbitMqConfigurations configurations)
        {
            return new ConnectionFactory()
            {
                HostName = configurations.HostName,
                Port = configurations.Port,
                UserName = configurations.UserName,
                Password = configurations.Password
            };
        }

        private void DeclareQueue(IModel channel)
        {
            channel.QueueDeclare(queue: "Messages",
                                         durable: false,
                                         exclusive: false,
                                         autoDelete: false,
                                         arguments: null);
        }

        private byte[] SetBodyRequest(Content content)
        {
            string message =
                        $"{DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss")} - " +
                        $"Message: {content.Message}";

            return Encoding.UTF8.GetBytes(message);
        }

        private void SendMessage(IModel channel, byte[] body)
        {
            channel.BasicPublish(exchange: "",
                                         routingKey: "Messages",
                                         basicProperties: null,
                                         body: body);
        }
    }
}