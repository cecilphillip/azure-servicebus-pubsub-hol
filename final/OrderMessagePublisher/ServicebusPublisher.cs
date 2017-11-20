using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace OrderMessagePublisher
{
    public class ServicebusPublisher : IDisposable
    {
        private TopicClient _topicClient;

        public ServicebusPublisher(string connectionString, string topicName)
        {
            MessagingFactory factory = MessagingFactory.CreateFromConnectionString(connectionString);
            _topicClient = factory.CreateTopicClient(topicName);
        }

        public async Task PublishOrder(Order order)
        {
            string serializedOrder = JsonConvert.SerializeObject(order);
            byte[] messageData = Encoding.UTF8.GetBytes(serializedOrder);

            BrokeredMessage message = new BrokeredMessage(new MemoryStream(messageData), true)
            {
                ContentType = "application/json",
                Label = order.GetType().ToString(),
                MessageId = order.OrderID
            };

            await _topicClient.SendAsync(message);
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _topicClient.Close();
                }

                disposedValue = true;
            }
        }

        void IDisposable.Dispose()
        {
            Dispose(true);
        }
        #endregion
    }
}
