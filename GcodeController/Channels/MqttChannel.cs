using GcodeController.Handlers;
using GcodeController.Services;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Options;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GcodeController.Channels {
    public class MqttChannel : IDisposable {
        private readonly IMqttClient _client;
        private readonly ILogger<MqttChannel> _logger;
        private readonly IEventHubService _hubService;

        public MqttChannel(IEventHubService hubService, ILoggerFactory loggerFactory) {
            _logger = loggerFactory.CreateLogger<MqttChannel>();
            _client = new MqttFactory().CreateMqttClient();
            _hubService = hubService;
            _hubService.Subscribe<JobInfo>(this, async jobInfo =>
                await ToMqttServiceAsync(TopicFactory(JobService.PREFIX), EventResponse<JobInfo>.Payload(jobInfo))
            );
            _hubService.Subscribe<CreateNewSerialRequest>(this, async jobInfo =>
                await ToMqttServiceAsync(TopicFactory(JobService.PREFIX), EventResponse<JobInfo>.New(jobInfo).ToString())
            );
        }

        private async Task ToMqttServiceAsync(string topic, string payload) {
            if (_client is null || !_client.IsConnected) {
                _logger.LogWarning("MQTT client is not connected");
                return;
            }
            var ct = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var message = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(payload)
                .WithExactlyOnceQoS()
                .WithRetainFlag()
                .Build();
            await _client.PublishAsync(message, ct.Token);
            _logger.LogInformation($"Published {topic}");
            _logger.LogDebug($"{topic} with data: {payload}");
        }

        private static string TopicFactory(string topic, bool isCommand = false) {
            return isCommand ? "cmnd" : "stat" + $"/{Environment.MachineName}/{topic}";
        }

        public async Task ConnectAsync(string iPAddress) {
            var options = new MqttClientOptionsBuilder()
                .WithClientId(Environment.MachineName)
                .WithTcpServer(iPAddress)
                .Build();
            var ct = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            await _client.ConnectAsync(options, ct.Token);
            _logger.LogInformation($"Connected to {iPAddress}");
            var topics = new MqttTopicFilterBuilder()
                .WithTopic(TopicFactory(SerialHandler.PREFIX, true))
                .WithTopic(TopicFactory(JobService.PREFIX, true))
                .WithTopic(TopicFactory(CommandHandler.PREFIX, true));
            await _client.SubscribeAsync(topics.Build());
            _client.UseApplicationMessageReceivedHandler(e => {

                //TODO Map to commands
                _logger.LogInformation("### RECEIVED APPLICATION MESSAGE ###");
                _logger.LogInformation($"+ Topic = {e.ApplicationMessage.Topic}");
                _logger.LogInformation($"+ Payload = {Encoding.UTF8.GetString(e.ApplicationMessage.Payload)}");
                _logger.LogInformation($"+ QoS = {e.ApplicationMessage.QualityOfServiceLevel}");
                _logger.LogInformation($"+ Retain = {e.ApplicationMessage.Retain}");
            });
        }

        public void Dispose() {
            _client?.DisconnectAsync().GetAwaiter().GetResult();
        }
    }
}
