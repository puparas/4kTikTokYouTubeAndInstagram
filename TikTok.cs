using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FuckTikTokYouTubeAndInstagram
{
    public class TikTok
    {
        private string _sourceLink;
        private static HttpWebRequest client;
        public TikTok(string sourceLink)
        {
            _sourceLink = sourceLink;
        }

        async public Task<string> GetUrl()
        {

            string linkToVideo ="";
                try
                {
                    Tuple<string, string> idsTupl = GetVideoAndUserId(_sourceLink);
                    if(idsTupl.Item1.Length == 0 && idsTupl.Item2.Length == 0)
                    {
                        string newUrl = GetFinalRedirect(_sourceLink);
                        idsTupl = GetVideoAndUserId(newUrl);
                    }
                    
                    client = (HttpWebRequest)WebRequest.Create("https://www.tiktok.com/node/share/video/" + idsTupl.Item2 + "/" + idsTupl.Item1);
                    client.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/99.0.4844.74 Safari/537.36";
                    client.Method = "GET";
                    HttpWebResponse response = (HttpWebResponse)client.GetResponse();
                    using (Stream stream = response.GetResponseStream())
                    using (StreamReader reader = new StreamReader(stream))
                    {

                        string html = reader.ReadToEnd();
                        JToken jsonData = JToken.Parse(html);
                        linkToVideo = (string)jsonData.SelectToken("itemInfo.itemStruct.video.downloadAddr");
                    }


                }
                catch (Exception e)
                {
                    throw e;
                }

            return linkToVideo;
        }

        private static string GetFinalRedirect(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return url;
            string newUrl = url;
                HttpWebRequest req;
                HttpWebResponse resp = null;
                try
                {
                    req = (HttpWebRequest)HttpWebRequest.Create(url);
                    req.Method = "HEAD";
                    req.AllowAutoRedirect = false;
                    resp = (HttpWebResponse)req.GetResponse();
                    switch (resp.StatusCode)
                    {
                        case HttpStatusCode.OK:
                            return newUrl;
                        case HttpStatusCode.Redirect:
                        case HttpStatusCode.MovedPermanently:
                        case HttpStatusCode.RedirectKeepVerb:
                        case HttpStatusCode.RedirectMethod:
                            newUrl = resp.Headers["Location"];
                            if (newUrl == null)
                                return url;

                            if (newUrl.IndexOf("://", System.StringComparison.Ordinal) == -1)
                            {
                                Uri u = new Uri(new Uri(url), newUrl);
                                newUrl = u.ToString();
                            }
                            break;
                        default:
                            return newUrl;
                    }
                    url = newUrl;
                }
                catch (WebException)
                {
                    // Return the last known good URL
                    return newUrl;
                }
                catch (Exception ex)
                {
                    return null;
                }
                finally
                {
                    if (resp != null)
                        resp.Close();
                }

            return newUrl;
        }
        public Stream GetStream(string tikTokUrl)
        {
            WebClient myWebClient = new WebClient();
            Stream myStream = myWebClient.OpenRead(tikTokUrl);
            return myStream;
        }
        private Tuple<string,string> GetVideoAndUserId(string url)
        {
            string pattern = @"https:\/\/w{0,3}\.?tiktok\.com/(.*)/video/([0-9]*)";
            Regex rgx = new Regex(pattern);
            string videoId = rgx.Match(url).Groups[2].Value;
            string userId = rgx.Match(url).Groups[1].Value;
            return Tuple.Create(videoId, userId);
        }

    }
    
}
