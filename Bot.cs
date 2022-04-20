using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Extensions.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using urldetector;
using urldetector.detection;
using Microsoft.Extensions.Configuration;

namespace FuckTikTokYouTubeAndInstagram
{
    class Bot
    {
        private IConfigurationBuilder builder = new ConfigurationBuilder().AddJsonFile("appsettings.json", false, true);
        private IConfigurationRoot configuration;
        private TelegramBotClient botClient;
        CancellationTokenSource cts;
        ReceiverOptions receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = {}
        };
        private List<string> supportServicess = new List<string>()
        {
            "youtube.com",
            "www.youtube.com",
            "www.instagram.com",
            "instagram.com",
            "www.tiktok.com",
            "tiktok.com",
            "vm.tiktok.com"
        };

        Instagram inst = new Instagram();
        private static async Task Main()
        {
            var hostBuilder = new HostBuilder()
             .ConfigureServices((hostContext, services) =>
             {
                 services.AddHostedService<Service>();
             }
             );
            await hostBuilder.RunConsoleAsync().ConfigureAwait(false);
        }
        public Bot()
        {
            configuration = builder.Build();
            botClient = new TelegramBotClient(configuration["tg_key"]);
            cts = new CancellationTokenSource();
            botClient.StartReceiving(
                HandleUpdateAsync,
                HandleErrorAsync,
                receiverOptions,
                cancellationToken: cts.Token
            );
        }
        private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            if (update.Type != UpdateType.Message || update.Message!.Type != MessageType.Text)
                return;

            long chatId = update.Message.Chat.Id;
            string messageText = update.Message.Text;
            int messageId = update.Message.MessageId;

            Dictionary<string, string> link = ParseLink(messageText);
            if (!(link.Count > 0))
                return;

            if (supportServicess.Contains(link["host"]))
            {
                switch (link["host"])
                {
                    case "youtube.com":
                    case "www.youtube.com":
                        try
                        {
                            Youtube youtube = new Youtube(link["origUrl"]);
                            List<dynamic> videoInfo = await youtube.GetVideoInfo();
                            if (videoInfo is null)
                                return;
                            if (videoInfo[1] < 700)
                            {
                                SendVideoOnChat(await youtube.GetStream(), chatId, messageId, cancellationToken);
                            }
                        }
                        catch (Exception)
                        {
                            //Нет так нет, че орать то
                        }
                        break;
                    case "tiktok.com":
                    case "www.tiktok.com":
                    case "vm.tiktok.com":
                        try
                        {
                            TikTok tikTok = new TikTok(link["origUrl"]);
                            string url = tikTok.GetUrl().Result;
                            if (url?.Length > 0)
                            {
                                SendVideoOnChat(tikTok.GetStream(url), chatId, messageId, cancellationToken);
                            }
                        }
                        catch (Exception)
                        {
                            //Нет так нет, че орать то
                        }
                        break;
                    case "instagram.com":
                    case "www.instagram.com":
                        try
                        {   
                            await inst.Login(configuration["inst_login"], configuration["inst_pass"]);
                            if (inst.LoginSuccess)
                            {
                                await inst.GetData(link["origUrl"]);
                                if(inst.GetPhotoUrls?.Length > 0)
                                {
                                    SendGroupOnChat(inst.GetPhotoUrls, chatId, messageId, cancellationToken);
                                }
                                if (inst.GetVideoUrls?.Length > 0)
                                {
                                    SendGroupOnChat(inst.GetVideoUrls, chatId, messageId, cancellationToken);
                                }

                            }
                            else
                            {
                                SomethingGoesWrong(chatId, messageId, cancellationToken, "Инстаграм меня отверг ¯\\_(ツ)_/¯ ");
                            }
                        }
                        catch (Exception)
                        {
                            //Нет так нет, че орать то
                        }
                        break;
                    default:
                        break;
                }

            }

        }
        private Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            var ErrorMessage = exception switch
            {
                ApiRequestException apiRequestException
                    => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
                _ => exception.ToString()
            };

            Console.WriteLine(ErrorMessage);
            return Task.CompletedTask;
        }

        private Dictionary<string, string> ParseLink(string mess)
        {
            UrlDetector parser = new UrlDetector(mess, UrlDetectorOptions.Default);
            List<Url> found = parser.Detect();
            if (found.Count == 1)
            {
                foreach (Url url in found)
                {
                    return new Dictionary<string, string>()
                    {
                        {"origUrl", url.GetOriginalUrl()},
                        {"host",   url.GetHost()}
                    };
                }

            }
            return new Dictionary<string, string>() { };
        }

        private async void SendVideoOnChat(Stream videoStream, long chatId, int messageId, CancellationToken cancellationToken)
        {
            try
            {
                await botClient.SendVideoAsync(
                chatId: chatId,
                video: videoStream,
                supportsStreaming: true,
                disableNotification: true,
                replyToMessageId: messageId,
                allowSendingWithoutReply: true,
                cancellationToken: cancellationToken);
            }
            catch (Exception)
            {
                SomethingGoesWrong(chatId, messageId, cancellationToken);
            }
            finally
            {
                videoStream.Close();
            }
        }
        private async void SendGroupOnChat(IAlbumInputMedia[] photoCollection, long chatId, int messageId, CancellationToken cancellationToken)
        {
            try
            {
                await botClient.SendMediaGroupAsync(
                chatId: chatId,
                media: photoCollection,
                disableNotification: true,
                replyToMessageId: messageId,
                allowSendingWithoutReply: true,
                cancellationToken: cancellationToken);
            }
            catch (Exception)
            {
                SomethingGoesWrong(chatId, messageId, cancellationToken);
            }
        }
        private async void SomethingGoesWrong(long chatId, int messageId, CancellationToken cancellationToken, string messageTest = "Все, кина не будет! Электричество кончилось!")
        {
            try
            {
                await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: messageTest,
                disableNotification: true,
                replyToMessageId: messageId,
                allowSendingWithoutReply: true,
                cancellationToken: cancellationToken);
            }
            catch (Exception)
            {
                //Нет так нет, че орать то
            }

        }
        public void Cancel()
        {
            cts.Cancel();
        }
    }
}
