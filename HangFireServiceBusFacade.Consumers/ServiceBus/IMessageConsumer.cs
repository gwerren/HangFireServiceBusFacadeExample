namespace HangFireServiceBusFacade.Consumers.ServiceBus
{
    using System.Threading.Tasks;

    public interface IMessageConsumer<TMessage>
        where TMessage: class, new()
    {
        // HangFire does support async execution of tasks (as do most frameworks
        // we might switch to) so this method should be async capable (return Task).
        Task Consume(IConsumeContext<TMessage> context);
    }

    public interface IConsumeContext<TMessage>
    {
        TMessage Message { get; }

        Task Publish<TPublishMessage>(TPublishMessage message)
            where TPublishMessage: class, new();
    }
}