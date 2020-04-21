namespace HangFireServiceBusFacade.Consumers.ServiceBus
{
    using System.Threading.Tasks;

    public interface IMessageConsumer<TMessage>
        where TMessage: class, new()
    {
        // HangFire does support async execution of tasks (as do most frameworks
        // we might switch to) so this method should be async capable (return Task).
        Task Consume(TMessage message);
    }
}