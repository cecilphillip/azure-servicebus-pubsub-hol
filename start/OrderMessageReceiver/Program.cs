using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace OrderMessageReceiver
{
    class Program
    {
        static void Main(string[] args)
        {
            var configuration = new ConfigurationBuilder()
              .SetBasePath(Directory.GetCurrentDirectory())
              .AddJsonFile("appsettings.json")
              .Build();

            var servicebusSettings = configuration.GetSection("ServicebusConfig");
            Console.WriteLine("**************************");
            Console.WriteLine("--- Order Receiver ---");
            Console.WriteLine("**************************\n");
            Console.Write("Enter subscription name: ");

            string subscriptionName = Console.ReadLine();
            var listener = new OrderListener(servicebusSettings["ConnectionString"], servicebusSettings["TopicName"], subscriptionName);

            CancellationTokenSource cts = new CancellationTokenSource();

            Task.Run(() => listener.Listen(cts.Token), cts.Token);
            string quit = string.Empty;

            do
            {
                quit = Console.ReadLine();

            } while (!quit.Equals("q", StringComparison.OrdinalIgnoreCase));

            cts.Cancel();
            cts.Dispose();
            Console.WriteLine("Shutting down... \n");
        }
    }
}
