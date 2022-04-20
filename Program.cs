using FuckTikTokYouTubeAndInstagram;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;


public class Service : IHostedService
{
    Bot Bot;
    public Service()
    {
    }
    public Task StartAsync(CancellationToken cancellationToken)
    {
        Bot = new Bot();
        return Task.CompletedTask;
    }
    public Task StopAsync(CancellationToken cancellationToken)
    {
        Bot.Cancel();
        return Task.CompletedTask;
    }
}
