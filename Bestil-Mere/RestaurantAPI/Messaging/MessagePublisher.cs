using EasyNetQ;
using Models.Messages.Restaurant;

namespace RestaurantAPI.Messaging
{
    public class MessagePublisher
    {
        private IBus _bus;

        public MessagePublisher(IMessagingSettings messagingSettings)
        {
            _bus = RabbitHutch.CreateBus(messagingSettings.ConnectionString);
        }

        public async void PublishRestaurantOrderStatus(RestaurantOrderStatus ros)
        {
            await _bus.PublishAsync(ros);
        }

    }
}