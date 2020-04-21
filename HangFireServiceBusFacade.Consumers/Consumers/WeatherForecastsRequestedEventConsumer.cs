namespace HangFireServiceBusFacade.Consumers.Consumers
{
    using System.Diagnostics;
    using System.Threading.Tasks;
    using HangFireServiceBusFacade.Consumers.ServiceBus;
    using HangFireServiceBusFacade.Core.Events;

    public class WeatherForecastsRequestedEventConsumer : IMessageConsumer<WeatherForecastsRequestedEvent>
    {
        public Task Consume(WeatherForecastsRequestedEvent message)
        {
            Debug.WriteLine($"********** WeatherForecasts called for '{message.Index}' **********");
            return Task.CompletedTask;
        }
    }
}