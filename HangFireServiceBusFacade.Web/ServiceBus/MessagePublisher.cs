namespace HangFireServiceBusFacade.Web.ServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Hangfire;
    using HangFireServiceBusFacade.Consumers.ServiceBus;
    using HangFireServiceBusFacade.Core.ServiceBus;
    using Microsoft.Extensions.DependencyInjection;

    public interface IMessagePublisherConfigurator
    {
        IMessagePublisherConfigurator For<TMessage>(Action<IMessageTypeConfigurator<TMessage>> configure)
            where TMessage : class, new();
    }

    public interface IMessageTypeConfigurator<TMessage>
        where TMessage: class, new()
    {
        IMessageTypeConfigurator<TMessage> Consumer<TConsumer>()
            where TConsumer : IMessageConsumer<TMessage>;
    }

    public static class HangfireMessagePublisher
    {
        public static IServiceCollection UseHangfireMessagePublisher(
            this IServiceCollection services,
            Action<IMessagePublisherConfigurator> configure)
            => services.AddSingleton<IMessagePublisher>(
                o =>
                {
                    var publisher = new MessagePublisher(o.GetService<IBackgroundJobClient>());
                    configure?.Invoke(publisher);
                    return publisher;
                });

        public static IGlobalConfiguration UseMessagePublisherActivator(
            this IGlobalConfiguration configuration,
            IServiceProvider serviceProvider)
            => configuration.UseActivator(
                new ContextAwareJobActivator(serviceProvider.GetService<IServiceScopeFactory>()));

        private class MessagePublisher : IMessagePublisher, IMessagePublisherConfigurator
        {
            private readonly IBackgroundJobClient backgroundJobClient;

            private readonly IDictionary<Type, IMessageConsumerSet> consumerSetsByMessageType
                = new Dictionary<Type, IMessageConsumerSet>();

            public MessagePublisher(IBackgroundJobClient backgroundJobClient)
            {
                this.backgroundJobClient = backgroundJobClient;
            }

            public IMessagePublisherConfigurator For<TMessage>(Action<IMessageTypeConfigurator<TMessage>> configure)
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
                        backgroundJobClient.Enqueue<MessageConsumerWithContext<TMessage, TConsumer>>(
                            c => c.Consume(message));
                    }
                }
            }
        }

        public class MessageConsumerWithContext<TMessage, TConsumer>
            where TMessage : class, new()
            where TConsumer : IMessageConsumer<TMessage>
        {
            private readonly IMessagePublisher messagePublisher;
            private readonly TConsumer messageConsumer;

            public MessageConsumerWithContext(IMessagePublisher messagePublisher, TConsumer messageConsumer)
            {
                this.messagePublisher = messagePublisher;
                this.messageConsumer = messageConsumer;
            }

            public Task Consume(TMessage message)
                => this.messageConsumer.Consume(new Context(this.messagePublisher, message));

            private class Context : IConsumeContext<TMessage>
            {
                private readonly IMessagePublisher messagePublisher;

                public Context(IMessagePublisher messagePublisher, TMessage message)
                {
                    this.messagePublisher = messagePublisher;
                    this.Message = message;
                }

                public TMessage Message { get; }

                public Task Publish<TPublishMessage>(TPublishMessage message)
                    where TPublishMessage : class, new()
                    => this.messagePublisher.Publish(message);
            }
        }

        private class ContextAwareJobActivator : JobActivator
        {
            private readonly IServiceScopeFactory serviceScopeFactory;

            public ContextAwareJobActivator(IServiceScopeFactory serviceScopeFactory)
                => this.serviceScopeFactory =
                    serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));

            public override JobActivatorScope BeginScope(JobActivatorContext context) => this.BeginScope();

            public override JobActivatorScope BeginScope() => new Scope(this.serviceScopeFactory.CreateScope());

            private class Scope : JobActivatorScope
            {
                private static readonly Type MessageConsumerWithContext = typeof(MessageConsumerWithContext<,>);
                private readonly IServiceScope serviceScope;

                public Scope(IServiceScope serviceScope)
                    => this.serviceScope = serviceScope ?? throw new ArgumentNullException(nameof(serviceScope));

                public override object Resolve(Type type)
                {
                    // MessageConsumerWithContext is a special type for us to create,
                    // delegating the creation of the actual consumer to the normal
                    // HangFire behaviour for ASP.NET Core which allows the consumer
                    // types to not need to be directly registered with the DI container.
                    if (type.IsGenericType && type.GetGenericTypeDefinition() == MessageConsumerWithContext)
                    {
                        return Activator.CreateInstance(
                            type,
                            this.serviceScope.ServiceProvider.GetService<IMessagePublisher>(),
                            ActivatorUtilities.GetServiceOrCreateInstance(
                                this.serviceScope.ServiceProvider,
                                type.GenericTypeArguments[1]));
                    }

                    // If this is not a request for MessageConsumerWithContext then imediately default
                    // to the default HangFire behaviour.
                    return ActivatorUtilities.GetServiceOrCreateInstance(this.serviceScope.ServiceProvider, type);
                }

                public override void DisposeScope() => this.serviceScope.Dispose();
            }
        }
    }
}