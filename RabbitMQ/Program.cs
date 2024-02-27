using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Diagnostics;
using System.Text;

namespace RabbitMQ
{
    static class Program
    {
        public static string? Hostname { get { return Environment.GetEnvironmentVariable("hostname"); } }
        public static string? Port { get { return Environment.GetEnvironmentVariable("port"); } }
        public static string? ManagementPort { get { return Environment.GetEnvironmentVariable("management_port"); } }
        public static string? Username { get { return Environment.GetEnvironmentVariable("username"); } }
        public static string? Password { get { return Environment.GetEnvironmentVariable("password"); } }
        public static string? Queue { get { return Environment.GetEnvironmentVariable("queue"); } }
        public static string? Exchange { get { return Environment.GetEnvironmentVariable("exchange"); } }
        public static ConnectionFactory? ConnectionFactory { get; set; }

        public static void Main()
        {
            DotNetEnv.Env.Load();
            Console.WriteLine("Configuração carregadas do arquivo .env:");
            Console.WriteLine($"    Hostname: {Hostname}");
            Console.WriteLine($"    Port: {Port}");
            Console.WriteLine($"    ManagementPort: {ManagementPort}");
            Console.WriteLine($"    Username: {Username}");
            Console.WriteLine($"    Password: {Password}");
            Console.WriteLine($"    Queue: {Queue}");
            Console.WriteLine($"    Exchange: {Exchange}");
            Console.WriteLine();
            Console.WriteLine($"Inicie um container Docker do RabbitMQ digitando 's' ou qualquer tecla com Rabbit já iniciado.");

            if (Console.ReadKey().Key == ConsoleKey.S)
            {
                RunDockerContainer();
            }

            ConnectionFactory = new ConnectionFactory
            {
                HostName = Hostname,
                Port = int.Parse(Port!),
                UserName = Username,
                Password = Password,
            };

            Console.Clear();
            using var connection = ConnectionFactory.TryCreateConnection();
            using var channel = connection.CreateModel();

            channel.CreateQueue();
            channel.ConfigureConsumer();
            Console.Clear();
            while (true)
            {
                Console.WriteLine("Digite uma mensagem ou digite 's' para sair.");
                string message = Console.ReadLine()!;
                if (message == "s")
                {
                    Console.WriteLine("Saindo ...");
                    break;
                }

                channel.Publish(message);
            }
        }

        static void Publish(this IModel channel, string message)
        {
            var body = Encoding.UTF8.GetBytes(message);
            channel.BasicPublish(exchange: Exchange ?? "",
                                routingKey: Queue,
                                basicProperties: null,
                                body: body);
            Console.WriteLine($"Enviado {message}");
        }
        public static void ConfigureConsumer(this IModel channel)
        {
            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                Console.WriteLine("Recebido {0}", message);
                channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
            };
            channel.BasicConsume(queue: Queue, autoAck: false, consumer: consumer);
        }
        public static void CreateQueue(this IModel channel)
        {
            channel.QueueDeclare(queue: Queue,
                                durable: false,
                                exclusive: false,
                                autoDelete: false,
                                arguments: null);
        }

        public static IConnection TryCreateConnection(this ConnectionFactory connectionFactory)
        {
            int maxRetries = 3;
            int retryIntervalSec = 5;
            int attempts = 0;
            while (true)
            {
                try
                {
                    return connectionFactory.CreateConnection();
                }
                catch (Exception ex)
                {
                    attempts++;
                    Console.WriteLine($"Erro ao criar conexão: {ex.Message}");
                    if (attempts >= maxRetries)
                    {
                        Console.WriteLine($"Número máximo de tentativas excedido ({maxRetries}).");
                        throw ex;
                    }

                    Console.WriteLine($"Tentando novamente em {retryIntervalSec} segundos...");
                    Thread.Sleep(retryIntervalSec * 1000);
                }
            }
        }
        public static void RunDockerContainer()
        {
            Console.Clear();
            var hostname = "rabbitmq-host";
            var containerName = "rabbitmq";
            var dockerImage = "rabbitmq:3-management";
            var envVars = $" -e RABBITMQ_DEFAULT_USER={Username} -e RABBITMQ_DEFAULT_PASS={Password}";

            using Process process = new();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = "docker",
                Arguments = $"run -d --hostname {hostname} --name {containerName} -p {ManagementPort}:15672 -p {Port}:5672 {envVars} {dockerImage}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
            process.Start();
            
            process.WaitForExit();

            string output = process.StandardOutput.ReadToEnd();
            Console.WriteLine(output);

            string error = process.StandardError.ReadToEnd();
            Console.WriteLine(error);
        }
    }
}