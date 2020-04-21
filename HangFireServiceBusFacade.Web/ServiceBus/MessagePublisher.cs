namespace HangFireServiceBusFacade.Web.ServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Hangfire;
    using HangFireServiceBusFacade.Consumers.ServiceBus;
    using HangFireServiceBusFacade.Core.ServiceBus;

    public interface IMessageTypeConfigurator<TMessage>
        where TMessage: class, new()
    {
        IMessageTypeConfigurator<TMessage> Consumer<TConsumer>()
            where TConsumer : IMessageConsumer<TMessage>;
    }

    public class MessagePublisher : IMessagePublisher
    {
        private readonly IBackgroundJobClient backgroundJobClient;

        private readonly IDictionary<Type, IMessageConsumerSet> consumerSetsByMessageType
            = new Dictionary<Type, IMessageConsumerSet>();

        public MessagePublisher(IBackgroundJobClient backgroundJobClient)
        {
            this.backgroundJobClient = backgroundJobClient;
        }

        public MessagePublisher For<TMessage>(Action<IMessageTypeConfigurator<TMessage>> configure)
            where TMessage : class, new()
        {
            if (!this.consumerSetsByMessageType.TryGetValue(typeof(TMessage), out var consumerSet))
            {
                consumerSet = new MessageConsumerSet<TMessage>();
                this.consumerSetsByMessageType.Add(typeof(TMessage), consumerSet);
            }

            configure((MessageConsumerSet<TMessage>)consumerSet);
            return this;
        }

        public Task Publish<TMessage>(TMessage message) where TMessage : class, new()
        {
            if (this.consumerSetsByMessageType.TryGetValue(typeof(TMessage), out var consumerSet))
                ((MessageConsumerSet<TMessage>)consumerSet).Publish(this.backgroundJobClient, message);

            return Task.CompletedTask;
        }

        private interface IMessageConsumerSet { }

        private class MessageConsumerSet<TMessage> : IMessageConsumerSet, IMessageTypeConfigurator<TMessage>
            where TMessage : class, new()
        {
            private readonly IList<IConsumerWrapper> consumers = new List<IConsumerWrapper>();

            public IMessageTypeConfigurator<TMessage> Consumer<TConsumer>()
                where TConsumer : IMessageConsumer<TMessage>
            {
                this.consumers.Add(new ConsumerWrapper<TConsumer>());
                return this;
            }

            public void Publish(IBackgroundJobClient backgroundJobClient, TMessage message)
            {
                foreach (var consumer in this.consumers)
                    consumer.Publish(backgroundJobClient, message);
            }

            private interface IConsumerWrapper
            {
                void Publish(IBackgroundJobClient backgroundJobClient, TMessage message);
            }

            private class ConsumerWrapper<TConsumer> : IConsumerWrapper
                where TConsumer : IMessageConsumer<TMessage>
            {
                public void Publish(IBackgroundJobClient backgroundJobClient, TMessage message)
                {
                    // Here we create the HangFire jobs for the registered consumers
                    backgroundJobClient.Enqueue<TConsumer>(c => c.Consume(message));
                }
            }
        }
    }
}