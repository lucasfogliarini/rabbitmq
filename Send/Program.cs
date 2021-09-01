using RabbitMQ.Client;
using System;
using System.Text;

namespace Send
{
    static class Send
    {
        public static void Main()
        {
            var factory = new ConnectionFactory() { HostName = "localhost" };
            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                channel.QueueDeclare(queue: "hello", durable: false, exclusive: false, autoDelete: false, arguments: null);

                while (true)
                {
                    Console.WriteLine("Type message or q");
                    string message = Console.ReadLine();
                    if (message == "q")
                    {
                        Console.WriteLine("Exiting");
                        break;
                    }
                    
                    channel.Publish(message);
                }
            }
        }

        static void Publish(this IModel channel, string message)
        {
            var body = Encoding.UTF8.GetBytes(message);
            channel.BasicPublish(exchange: "", routingKey: "hello", basicProperties: null, body: body);            
            Console.WriteLine(" [x] Sent {0}", message);
        }
    }
}