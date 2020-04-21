namespace HangFireServiceBusFacade.Core.ServiceBus
{
    using System.Threading.Tasks;

    public interface IMessagePublisher
    {
        // HangFire does not support async creation of tasks so making
        // this method async is not really needed, however since one of our
        // aims is to enable fairly simple switching away from HangFire in
        // future (which could support async message publishing) we should
        // make this method async capable (return Task) to make switching easy.
        Task Publish<TMessage>(TMessage message)
            where TMessage: class, new();
    }
}