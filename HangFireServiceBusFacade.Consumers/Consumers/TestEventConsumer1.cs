namespace HangFireServiceBusFacade.Consumers.Consumers
{
    using System.Diagnostics;
    using System.Threading.Tasks;
    using HangFireServiceBusFacade.Consumers.ServiceBus;
    using HangFireServiceBusFacade.Core.Events;

    public class TestEventConsumer1 : IMessageConsumer<TestEvent>
    {
        public Task Consume(TestEvent message)
        {
            Debug.WriteLine($"Test Event (Consumer 1): '{message.Text}'");
            return Task.CompletedTask;
        }
    }
}