using PubSub;
using System;

namespace GcodeController.Services {
    public interface IEventHubService {
        T Publish<T>(T data);
        void Subscribe<T>(object subscriber, Action<T> handler);
    }
    public class EventHubService : IEventHubService {
        private Hub _hub;
        public EventHubService(Hub hub) {
            _hub = hub;
        }

        public T Publish<T>(T data) {
            _hub.Publish(data);
            return data;
        }
        public void Subscribe<T>(object subscriber, Action<T> handler)
            => _hub.Subscribe(subscriber, handler);
    }
}
