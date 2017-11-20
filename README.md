Implementing Publish-Subscribe with Azure Service Bus and .NET Core
==================================================================

## Introduction
Azure Service Bus is a hosted messaging service that allows for reliable and secure communication between two or more parties at cloud scale. 
The main goal of the service is to make it easier to exchange information by acting as the message broker. The producers and consumers of messages
will know very little about each other by design. Instead of communicating directly with each other, all communication will be mediated through
the broker. This type of brokered messaging is sometimes described as temporal decoupling, since neither side is required to be online at the same time.
This flexibility makes it relatively easy to implement messaging patterns such as Publish-Subscribe.

The brokered messaging support in Azure Service Bus are formed around two entities; queues and topics. Queues offer First-in First-out (FIFO)
message delivery to one or more competing consumers. This means that messages are expected to be retrieved and processed by consumers in the order
in which they were added to the queue. Also, each message is received and processed by only a single message consumer. Topics, on the other hand,
provide a one-to-many form of messaging. Messages sent to topics will be delivered to one or more associated subscriptions, which may have optional
filters applied to restrict the incomming messages. Unlike queues, producer send messages to topics but consumers receiv messages from subscriptions.

In this lab, we will walk you through the process of implementing the Publish-Subscribe pattern in Azure Service Bus using topics. You will create 
a producers and consumer(s) using C#, the [.NET Framework](https://www.microsoft.com/net/download/windows) and the [Azure Service Bus .NET Client](https://www.nuget.org/packages/WindowsAzure.ServiceBus). 
The lab also assumes that you will be using the [Visual Studio IDE](https://www.visualstudio.com/vs) on Windows, and that you have already signed up for a
[Microsoft Azure account](https://azure.microsoft.com/en-us/free).

Included with this lab, you will also find a starter solution in the **start** folder, which has the basic setup required for you to get stareted. Also,
if you ever get stuck, feel free to review the compelted solution inside of the **final** folder.

## Creating a namespace 
Before you are able to create topics or queues in Azure Service Bus, you first have to create a namespace. Namespaces are essentially containers that are used to
scope your messaging components. Within a namespace, you can create a many topics or queues as you need.

### Steps

1. Go to the [Azure portal](https://portal.azure.com), and login with your account.
2. In the left navigation, click **New** to open up the market place.
3. Now choose **Enterprise Integration** and then **Service Bus**
4. When the **Create namespace** blade opens, enter a name for your namespace.
5. Next, choose the Azure subscription and resource group you want use.
6. Click the **Create** button. Your new namespace should be ready within a minute or so.

![](media/create_namespace.PNG)


## Creating a topic
There are a variety of way you can create topics for Azure Service Bus. Powershell Cmdlets, management SDKs for variously languages, and the UI
inside of the Azure portal are all viable options. In this lab, we'll focus on using the Azure portal.

### Steps
1. Go to your newly created namespace, and make sure you are in the **Overview** section.
2. Click on the **Topic** button in the hortizontal menu. This should open the **Create topic** section.
3. Enter a name for your topic. In the sceenshot below, I choose *order-topc* since we're going to be working with orders. The other options can be left with their default values.
4. Click the **Create** button to provision your topic.

![](media/create_topic.PNG)

## Working with Shared Access Signatues
[Share Access Signatures](https://docs.microsoft.com/azure/service-bus-messaging/service-bus-sas) (or SAS keys)
is a way to control who has acceess to your Service Bus instance. SAS keys in
Service Bus are SHA-256 secure hashes, and are paired with Shared Access policies.
Together, they enable applications to be authorized on the namespace level or on the individual queue/topic
with specific permissions. The available permissions today are:
* Send
* Listen
* Manage 
By default, every Service Bus namespace will have a root policy created that has all permissions enabled.
What you'll do now is create two polices; one for the publishing side and another for the subcription side.

### Steps
1. In your Service Bus instance inside the Azure Portal, select the **Shared access policies** section in the left menu.
2. Click on the **Add** button in the Shared access policies section.
3. Enter **SenderPolicy** in the text box and only select the **Send** permissions.
4. Click the **Create** button to add this policy to your namespace.
5. Add a second policy, but this time give it a name of **ReceiverPolicy** and only select the **Listen** permissions.
6. After creating the policies, click on one of them in the list. 
You should now see the a pair of SAS keys as well as their associated connection strings. We will use these shortly.


## Creating the publisher
In this lab, we will be creating a simple Publish-Subscribe system around place orders. If you haven't already done so,
open up the folder that contains the resources for this lab, navgiate into the **start** folder
and open the **HOL_AzureServiceBus_PubSub_Starter.lsn** file in Visual Studio. Now we're going to create
a console application that wil be used to publish orders.

### Steps
1. Navigate to the **OrderMessagePublisher** project.
2. Right click on the project, and choose **Manage NuGet packages**. The two packages you'll need to installed are
[Microsoft.Extensions.Configuration.Json](https://www.nuget.org/packages/Microsoft.Extensions.Configuration.Json)
and [WindowsAzure.ServiceBus](https://www.nuget.org/packages/WindowsAzure.ServiceBus).
3. Open the **appsettings.json**. This is where the configuration information is being store. You should see a section
for ServicebusConifg containing placeholders for TopicName and ConnectionString.
4. Copy and paste the connection string for the **SenderPolicy** policy next to ConnectionString key, and also enter 
the name of your topic next to the TopiceName key.
5. Open the **ServicebusPublisher.cs** file.
6. First we will implement the construtor. To send messages to a topic in Azure Service Bus, we will need an instance of
the [TopicClient](https://docs.microsoft.com/dotnet/api/microsoft.servicebus.messaging.topicclient?view=azure-dotnet).
To create the the TopicClient, we will use the MessagingFactory class along with the topic name and the connection string that
was generate in the portal.

```
public ServicebusPublisher(string connectionString, string topicName)
{
    MessagingFactory factory = MessagingFactory.CreateFromConnectionString(connectionString);
    _topicClient = factory.CreateTopicClient(topicName);
}
```

7. Next we will implement the **Publish** method. To send messages into Azure Service Bus, we will need to populate 
an instance of the [BrokerdMessage](https://docs.microsoft.com/dotnet/api/microsoft.servicebus.messaging.brokeredmessage?view=azure-dotnet)
class. We can use this class to set things like the payload in the message body and metadata properties in the message header.

The recommend way to create to the message payload is for you to take control of serializing your data and creating a stream
that can be supplied to the message. In the implementation below, we're using the [Newtonsoft.Json](https://www.nuget.org/packages/Newtonsoft.Json/)
package to serialize the order into a JSON string. We then turn that string into a btye array which it used to create a MemoryStream.
Also, notice that we're setting message properties like ContentType and MessageId. Receivers of the message can inspect
this metatadata to learn about the message playload before actually reading it.

```
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
```

## Creating subscribers
The subscribers will be responsible for listening to messages on the topic, and processing them as they come in. Each
subscriber will need to have its own subscription for the respective topic. As messages come into the topic, each subscription
will get a copy of the message.

### Steps
1. Navigate to the **OrderMessageReceiver** project.
2. Right click on the project, and choose **Manage NuGet packages**. The two packages you'll need to installed are
[Microsoft.Extensions.Configuration.Json](https://www.nuget.org/packages/Microsoft.Extensions.Configuration.Json)
and [WindowsAzure.ServiceBus](https://www.nuget.org/packages/WindowsAzure.ServiceBus).
3. Open the **appsettings.json**. This is where the configuration information is being store. You should see a section
for ServicebusConifg containing placeholders for TopicName and ConnectionString.
4. Copy and paste the connection string for the **ReceiverPolicy** policy next to ConnectionString key, and also enter 
the name of your topic next to the TopiceName key.
5. Open the **OrderListener.cs** file.
6. Now we will implement the construtor. First, we will use the NamespaceManager to create a subscription. An exception will get
thrown if you try to create an already existing subscription, so we'll just do a simple check to see if it's there before creating.

To receive messages on a topic in Azure Service Bus, we will need an instance of
the [SubscriptionClient](https://docs.microsoft.com/dotnet/api/microsoft.servicebus.messaging.SubscriptionClient?view=azure-dotnet).
To create the the SubscriptionClient, we will use the MessagingFactory class along with the topic name and the connection string that
was generated in the portal.

```
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

    MessagingFactory factory = MessagingFactory.CreateFromConnectionString(connectionString);
}
```

7. The **Listen** method is responsible for retrieving messages from the topic subscriptions and processing them. There are a few
options for retrieving messages. We will be using the OnMessage method, which acts like a message pump. It will invoke the
callback that it's provided to handle the message processing. OnMessage also has an overload that takes an instance of
[OnMessageOptions](https://docs.microsoft.com/dotnet/api/microsoft.servicebus.messaging.onmessageoptions?view=azure-dotnet).
We can use this class to set various options associated with how the message pump processes messages. In the code below,
we're setting the number of concurrent calls and also turning off auto-completion.

```
 public void Listen(CancellationToken token)
 {
     
  SubscriptionClient subscriptionClient = _factory.CreateSubscriptionClient(_topicName, _subscriptionName);
 
     OnMessageOptions options = new OnMessageOptions
     {                
         MaxConcurrentCalls = Environment.ProcessorCount,
         AutoComplete = false
     };
 
     Console.WriteLine("Waiting for new orders... \n");    
     
     _subscriptionClient.OnMessageAsync(async message =>
     {     
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

     token.Register(() => _subscriptionClient.CloseAsync());
 }
```
## Sending your first messages
At this point, a fair amount of code has been written. Let's execute what's been built so far and see how things work.

### Steps
1. Bulid your solution in Visual Studio. Hopefully everything has been built successfully.
2. Right click on the **OrderMessageReceiver** project and select **Open Folder in File Explorer**
3. Navigate through the following folders bin-> Debug-> net47. Within the final directory, you should see a collections of build artifacts for the project.
4. Run **OrderMessageReceiver.exe** executable 
5. Provide it with a subscription name and hit the enter key. The receiver should now be waiting for new messages to be send to the topic subscription.
6. Repeat steps 2 - 4 for the **OrderMessagePublisher**.
7. Type the **add** command and then hit enter to start creating an order. Enter the requested information and submit the new order.
8. The receiver application should have retrieve the new order and printed a summary to the console.
9. Try running multiple receivers and submitting another order. 

![](media/multiple_receivers.PNG)


## Sending messages to a dead-letter queue
There will be times when messages sent to the topic will not be able to be delivered to or processed by one of the available receivers.
This could happen for any number of reads; the message might be serialized using an unsupported format for example. Azure Service Bus provides a
sub-queue called the [dead-letter queue ](https://docs.microsoft.com/azure/service-bus-messaging/service-bus-dead-letter-queues)
to hold these types of messages. Instead of disgarding the message, it can be please in the dead-letter queue where it can be inspected
by some other means and processed accordingly.The dead-letter queue is not something you have to manage. It will be explicitly created for each topic or subscription respectively.

In the code we've created, the **OrderListenter** class only knows how to process JSON serialized orders. What would happen if another format was
used or if the message did not contain a valid order? Let's update the code to handle unsupported content types.

### Steps
1. Stop any running instances of the **OrderMessageReceiver**.
2. Go to the **OrderMessageReceiver** project in Visual Studio and open the **OrderListenter.cs** file.
3. Update the code at the beginning of the **OnMessageAsync** callback operation so that it resembles the following

```
subscriptionClient.OnMessageAsync(async message =>
{
    // We only support JSON payloads. Anything else will be moved to the dead letter queue 
    // to be handled by another process 
    if (message.ContentType != "application/json")
    {
        await message.DeadLetterAsync("Invalid Content Type", $"Unable to process a message with a Content Type of {message.ContentType}");
        return;
    }
```

If you were to run the **OrderMessageReceiver** application again, any message sent to the topic that doesn't have a content type set to application/json
will be sent to the dead-letter queue.

## Going forward
What we've created here is far from a production ready implementation of the Publish-Subscribe pattern using Azure Service Bus. Here is a list of items
that you should try implementing for yourself.

* Processing messages in the dead-letter queue.
* Adding support for other serialization formats.
* Implementing order validation.
* Adding logging/telemetry with Application Insights.
* Add unit tests for each project.

## References
In the lab, you've gained some hands on experience with working with Azure Service Bus, the .NET SDK,  and implementing the Publish-Subscribe pattern.
Please take a look at the links below for documentation and samples that you can go through to learn more about what you can
do with Azure service bus.

* [Azure Service Bus Messaging Documentation](https://docs.microsoft.com/azure/service-bus-messaging/)
* [Getting started with Service Bus topics](https://docs.microsoft.com/azure/service-bus-messaging/service-bus-dotnet-how-to-use-topics-subscriptions)
* [Azure Service Bus .NET Framework samples](https://github.com/Azure/azure-service-bus/tree/master/samples/DotNet/Microsoft.ServiceBus.Messaging)