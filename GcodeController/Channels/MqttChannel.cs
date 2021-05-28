using GcodeController.Handlers;
using GcodeController.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Net;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;

namespace GcodeController.Channels {
    public class MqttChannel : IDisposable {
        private MqttClient _client;
        private readonly ILogger<MqttChannel> _logger;
        private readonly IEventHubService _hubService;

        public MqttChannel(IEventHubService hubService, ILoggerFactory loggerFactory) {
            _logger = loggerFactory.CreateLogger<MqttChannel>();
            _hubService = hubService;
            _hubService.Subscribe<JobInfo>(this, jobInfo => {
                if (_client != null && _client.IsConnected) {
                    var topic = TopicFactory(JobService.PREFIX);
                    var eventData = new EventResponse<JobInfo>(jobInfo);
                    _client.Publish(topic, eventData.ToBytes());
                    _logger.LogInformation($"Published {topic}");
                    _logger.LogDebug($"{topic} with data: {eventData}");
                }
            });
        }

        private string TopicFactory(string topic, bool isCommand = false) {
            return isCommand ? "cmnd" : "stat" + $"/{Environment.MachineName}/{topic}";
        }

        public void Connect(IPAddress iPAddress) {
#pragma warning disable CS0618 // Type or member is obsolete
            _client = new MqttClient(iPAddress);
#pragma warning restore CS0618 // Type or member is obsolete
            _client.Connect(Guid.NewGuid().ToString());
            _client.MqttMsgPublishReceived += _client_MqttMsgPublishReceived;
            _client.Subscribe(new[] {
                TopicFactory(JobService.PREFIX, true),
                TopicFactory(CommandHandler.PREFIX, true) //TODO avoid double command!
            }, new byte[] { MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE });
        }

        private void _client_MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e) {
            _logger.LogInformation($"Subscribed topic incoming {e.Topic}:{e.Message}");
        }

        public void Dispose() {
            _client?.Disconnect();
        }
    }
}
