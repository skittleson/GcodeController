using EmbedIO.WebSockets;
using GcodeController.Channels;
using GcodeController.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace GcodeController {
    public class WebSocketModuleChannel : WebSocketModule {
        private readonly IServiceProvider _serviceProvider;
        private readonly IJobService _jobService;
        private readonly IEventHubService _hubService;

        public WebSocketModuleChannel(string urlPath, IServiceProvider serviceProvider)
            : base(urlPath, true) {
            _serviceProvider = serviceProvider;
            _jobService = serviceProvider.GetService<IJobService>();
            _hubService = serviceProvider.GetService<IEventHubService>();
            _hubService.Subscribe<JobInfo>(this, async jobInfo => {
                await BroadcastAsync(new EventResponse<JobInfo>(jobInfo).ToString());
            });
        }

        protected override Task OnMessageReceivedAsync(
            IWebSocketContext context,
            byte[] rxBuffer,
            IWebSocketReceiveResult rxResult) {
            //using var request = _serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope();
            //var file = request.ServiceProvider.GetService<IFilesHandler>();
            //return SendToOthersAsync(context, Encoding.GetString(rxBuffer));
            return Task.CompletedTask;
        }


        protected async override Task OnClientConnectedAsync(IWebSocketContext context) {
            await SendAsync(context, new EventResponse<JobInfo>(_jobService.GetJobs()).ToString());
            await base.OnClientConnectedAsync(context);
        }


        protected override Task OnClientDisconnectedAsync(IWebSocketContext context) {
            //=> SendToOthersAsync(context, "Someone left the chat room.");
            return Task.CompletedTask;
        }


        private Task SendToOthersAsync(IWebSocketContext context, string payload) {
            //return BroadcastAsync(payload, c => c != context);
            return Task.CompletedTask;
        }

    }
}
