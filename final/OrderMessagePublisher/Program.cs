using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace OrderMessagePublisher
{
    public class Program
    {
        static async Task Main(string[] args)
        {
            using (ServicebusPublisher publisher = SetupPublisher())
            {
                CancellationTokenSource cts = new CancellationTokenSource();
                await StartAppLoop(publisher);
            }
        }

        private static async Task StartAppLoop(ServicebusPublisher publisher)
        {
            Console.WriteLine("**************************");
            Console.WriteLine("--- Order Publisher ---");
            Console.WriteLine("**************************\n");
            Console.WriteLine("Commands: \n    add - Add Order\n    quit - Exit application");

            string choice = string.Empty;
            while (true)
            {
                Console.Write("\nEnter command> ");
                choice = Console.ReadLine();

                switch (choice)
                {
                    case "add":
                        await SubmitOrder(publisher);
                        break;
                    case "quit":
                        return;
                    default:
                        Console.WriteLine("Unknown command");
                        break;
                }
            }
        }

        private static ServicebusPublisher SetupPublisher()
        {
            var configuration = new ConfigurationBuilder()
              .SetBasePath(Directory.GetCurrentDirectory())
              .AddJsonFile("appsettings.json")
              .Build();

            var servicebusSettings = configuration.GetSection("ServicebusConfig");

            var publisher = new ServicebusPublisher(servicebusSettings["ConnectionString"], servicebusSettings["TopicName"]);
            return publisher;
        }

        private static async Task SubmitOrder(ServicebusPublisher publisher)
        {
            Console.Write("Customer Name: ");
            string customerName = Console.ReadLine();

            Console.Write("Item Name: ");
            string itemName = Console.ReadLine();

            Console.Write("Quantity: ");
            int.TryParse(Console.ReadLine(), out int quantity);

            Console.Write("Item Price: ");
            decimal.TryParse(Console.ReadLine(), out decimal price);

            Order newOrder = new Order
            {
                CustomerName = customerName,
                ItemName = itemName,
                UnitPrice = price,
                Quantity = quantity
            };

            try
            {
                await publisher.PublishOrder(newOrder);
                Console.WriteLine("The new order successfully submitted.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Sorry we were not able to processor your order at this time. Please try again later.");
            }
        }
    }
}
