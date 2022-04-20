
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;

namespace FuckTikTokYouTubeAndInstagram
{
    class Youtube
    {
        private string _sourceLink;
        private YoutubeClient youtube;
        public Youtube(string sourceLink)
        {
            
            youtube = new YoutubeClient();
            _sourceLink = sourceLink;
        }
        async public Task<List<dynamic>> GetVideoInfo() {
            try
            {
                var video = await youtube.Videos.GetAsync(_sourceLink);
                var title = video.Title; 
                TimeSpan duration = (TimeSpan)video.Duration;  
                return new List<dynamic>()
                {
                    title,
                    duration.TotalSeconds,
                };
            }
            catch (Exception)
            {
                return null;
            }

        }
        async public Task<Stream> GetStream()
        {
            StreamManifest streamManifest = await youtube.Videos.Streams.GetManifestAsync(_sourceLink);
            IVideoStreamInfo streamInfo = SelectStream(streamManifest);
            Stream stream = await youtube.Videos.Streams.GetAsync(streamInfo);
            return stream;
        }
        private IVideoStreamInfo SelectStream(StreamManifest streamManifest)
        {
            if (streamManifest.GetMuxedStreams().Where(s => s.Size.MegaBytes <= 50).Any())
            {
                return streamManifest.GetMuxedStreams().Where(s => s.Size.MegaBytes <= 50).Last();
            }
            return streamManifest.GetMuxedStreams().GetWithHighestVideoQuality();
        }
    }
}
