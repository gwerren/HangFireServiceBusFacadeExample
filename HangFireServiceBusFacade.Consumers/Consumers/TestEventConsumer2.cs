namespace HangFireServiceBusFacade.Consumers.Consumers
{
    using System.Diagnostics;
    using System.Threading.Tasks;
    using HangFireServiceBusFacade.Consumers.ServiceBus;
    using HangFireServiceBusFacade.Core.Events;

    public class TestEventConsumer2 : IMessageConsumer<TestEvent>
    {
        public Task Consume(IConsumeContext<TestEvent> context)
        {
            Debug.WriteLine($"Test Event (Consumer 2): '{context.Message.Text}'");
            return Task.CompletedTask;
        }
    }
}