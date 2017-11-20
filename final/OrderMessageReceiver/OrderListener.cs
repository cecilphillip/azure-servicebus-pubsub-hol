using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Threading;

namespace OrderMessageReceiver
{
    public class OrderListener
    {       
        private readonly string _topicName;
        private readonly string _subscriptionName;
        private readonly MessagingFactory _factory;

        public OrderListener(string connectionString, string topicName, string subscriptionName)
        {
            NamespaceManager manager = NamespaceManager.CreateFromConnectionString(connectionString);
            _topicName = topicName;
            _subscriptionName = subscriptionName;

            if (!manager.SubscriptionExists(topicName, subscriptionName))
            {
                // Create subscription
                SubscriptionDescription description = new SubscriptionDescription(topicName, subscriptionName)
                {
                    AutoDeleteOnIdle = TimeSpan.FromMinutes(25),
                    MaxDeliveryCount = 3
                };

                manager.CreateSubscription(description);
            }

             _factory = MessagingFactory.CreateFromConnectionString(connectionString);                     
        }

        public void Listen(CancellationToken token)
        {           
            SubscriptionClient subscriptionClient = _factory.CreateSubscriptionClient(_topicName, _subscriptionName);

            OnMessageOptions options = new OnMessageOptions
            {                
                MaxConcurrentCalls = Environment.ProcessorCount,
                AutoComplete = false
            };

            Console.WriteLine("Waiting for new orders... \n");

            subscriptionClient.OnMessageAsync(async message =>
            {
                // We only support JSON payloads. Anything else will be moved to the dead letter queue 
                // to be handled by another process 
                if (message.ContentType != "application/json")
                {
                    await message.DeadLetterAsync("Invalid Content Type", $"Unable to process a message with a Content Type of {message.ContentType}");
                    return;
                }

                Console.WriteLine($"--------------------");
                Console.WriteLine($"New Order Received!");
                Console.WriteLine($"--------------------");
                Console.WriteLine($"Label : {message.Label}");
                Console.WriteLine($"Content Type : {message.ContentType}");
                Console.WriteLine($"Time to Live : {message.TimeToLive.TotalMinutes} minutes\n");

                // Retrieve order from message body
                Stream messageBodyStream = message.GetBody<Stream>();
                string messageBodyContent = await new StreamReader(messageBodyStream).ReadToEndAsync();
                Order newOrder = JsonConvert.DeserializeObject<Order>(messageBodyContent);

                Console.WriteLine($"Customer Name: {newOrder.CustomerName}");
                Console.WriteLine($"Item : {newOrder.ItemName}");
                Console.WriteLine($"Unit Price: {newOrder.UnitPrice}");
                Console.WriteLine($"Quantity: {newOrder.Quantity}");
                Console.WriteLine($"----------------");

                // Mark message as comeplete so it can be removed from the subscription
                await message.CompleteAsync();
            }, options);

            token.Register(() => subscriptionClient.CloseAsync());
        }
    }
}