
using InstaSharper.API;
using InstaSharper.API.Builder;
using InstaSharper.Classes;
using InstaSharper.Logger;
using System;
using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot.Types;

namespace FuckTikTokYouTubeAndInstagram
{
    class Instagram
    {
        private IInstaApi _instaApi;
        public bool LoginSuccess { get; private set; }
        public InputMediaPhoto[] GetPhotoUrls { get; private set; }
        public InputMediaVideo[] GetVideoUrls { get; private set; }
        public Instagram()
        {
            LoginSuccess = false;
        }
        async public Task Login(string log, string pass)
        {
            if (LoginSuccess)
                return;
            try
            {
                _instaApi = InstaApiBuilder.CreateBuilder()
                .SetUser(new UserSessionData
                {
                    UserName = log,
                    Password = pass
                })
                //.UseLogger(new DebugLogger(LogLevel.All))
                .SetRequestDelay(RequestDelay.FromSeconds(0,3))
                .Build();
                var login = await _instaApi.LoginAsync();
                if (login.Succeeded)
                {
                    LoginSuccess = true;
                }
            }
            catch
            {

                LoginSuccess = false;

            }
        }
        async public Task GetData(string uri)
        {
            GetVideoUrls = default;
            GetPhotoUrls = default;
            try
            {
                var mediaIdFromUrl = await _instaApi.GetMediaIdFromUrlAsync(new Uri(uri));
                var mediaItem = await _instaApi.GetMediaByIdAsync(mediaIdFromUrl.Value);
                if (mediaItem.Succeeded)
                {
                    if(mediaItem?.Value?.Carousel?.Count > 0)
                    {
                        GetPhotoUrls = mediaItem.Value.Carousel.Where(u => u.Videos.Count == 0).Select(i => new InputMediaPhoto(i.Images[0].URI)).ToArray();
                        GetVideoUrls = mediaItem.Value.Carousel.Where(u => u.Videos.Count > 0).Select(i => new InputMediaVideo(i.Videos[0].Url)).ToArray();
                    }
                    else
                    {
                        if (mediaItem?.Value?.Videos?.Count > 0)
                        {
                            //GetVideoUrls.ToArray().SetValue(new InputMediaVideo(mediaItem.Value.Videos[0].Url), 0);
                            GetVideoUrls = mediaItem.Value.Videos.Take(1).Select(i => new InputMediaVideo(i.Url)).ToArray();
                        }
                        else if(mediaItem?.Value?.Images?.Count > 0)
                        {
                            GetPhotoUrls = mediaItem.Value.Images.Take(1).Select(i => new InputMediaPhoto(i.URI)).ToArray();
                        }
                    }
                }
                mediaItem = default;
            }
            catch (Exception e)
            {
                throw e;
            }

        }

    }
}