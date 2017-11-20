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

        }

        public void Listen(CancellationToken token)
        {

        }
    }
}