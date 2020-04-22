namespace HangFireServiceBusFacade.Consumers.Consumers
{
    using System.Diagnostics;
    using System.Threading.Tasks;
    using HangFireServiceBusFacade.Consumers.ServiceBus;
    using HangFireServiceBusFacade.Core.Events;

    public class WeatherForecastsRequestedEventConsumer : IMessageConsumer<WeatherForecastsRequestedEvent>
    {
        public Task Consume(IConsumeContext<WeatherForecastsRequestedEvent> context)
        {
            Debug.WriteLine($"********** WeatherForecasts called for '{context.Message.Index}' **********");
            return Task.CompletedTask;
        }
    }
}