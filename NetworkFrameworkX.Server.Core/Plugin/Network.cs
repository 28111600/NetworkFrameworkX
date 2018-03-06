using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using NetworkFrameworkX.Interface;
using NetworkFrameworkX.Share;

namespace NetworkFrameworkX.Server.Plugin
{
    internal class Response
    {
        public Uri ResponseUri { get; set; }

        public HttpStatusCode StatusCode { get; set; }

        public string StatusDescription { get; set; } = string.Empty;

        public string Content { get; set; } = string.Empty;

        public int ContentLength => this.Content.Length;

        public byte[] RawContent { get; set; }

        public int RawContentLength => this.RawContent.Length;

        public Dictionary<string, string> Headers = new Dictionary<string, string>();
        public string ContentType { get; set; } = string.Empty;

        public Encoding Encoding { get; set; } = null;

        public string ErrorMessage { get; set; } = string.Empty;
    }

    internal sealed class Network : IPlugin
    {
        private const string UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/58.0.3029.110 Safari/537.36";

        public string Name => "NetWork";

        public IServer Server { get; set; }

        public PluginConfig Config { get; private set; } = new PluginConfig();

        public string SerializeConfig() => null;

        public void DeserializeConfig(string text)
        {
        }

        public void OnLoad()
        {
            AddCommand_Ping();
            AddCommand_Get();
        }

        public void OnDestory()
        {
        }

        private static string strData = new string('A', 32);
        private static byte[] byteData = Encoding.ASCII.GetBytes(strData);

        public string Ping(string ipAddress, int ttl = 64)
        {
            const int Timeout = 5000;
            Ping ping = new Ping();
            PingOptions Option = new PingOptions() { Ttl = ttl };

            Stopwatch sw = new Stopwatch();
            sw.Start();

            string result = null;

            try {
                PingReply reply = ping.Send(ipAddress, Timeout, byteData, Option);

                switch (reply.Status) {
                    case IPStatus.Success:
                        result = $"{reply.Address.ToString()} : {reply.Status.ToString()} {sw.ElapsedMilliseconds}ms TTL={reply.Options.Ttl}";

                        break;

                    case IPStatus.TimedOut:
                        result = $"Timeout";
                        break;

                    default:
                        result = $"Error : {reply.Status.ToString()}";
                        break;
                }
            } catch (Exception e) {
                result = $"Error : {e.Message}";
            }

            sw.Start();

            return result;
        }

        private Response ReadFromHttp(string url)
        {
            const int Timeout = 2000;
            Response response = new Response();

            HttpWebRequest req = (HttpWebRequest)WebRequest.CreateDefault(new Uri(url));

            req.Method = WebRequestMethods.Http.Get;
            req.Timeout = Timeout;
            req.UserAgent = UserAgent;
            req.Referer = url;

            HttpWebResponse resp;
            try {
                resp = req.GetResponse() as HttpWebResponse;
            } catch (WebException e) {
                resp = e.Response as HttpWebResponse;
                if (resp == null) {
                    response.ErrorMessage = e.Message;
                }
            }
            if (resp != null) {
                using (Stream reader = resp.GetResponseStream()) {
                    MemoryStream ms = new MemoryStream();
                    reader.CopyTo(ms);
                    response.RawContent = ms.ToArray();
                    ms.Close();
                }
                resp.Close();
                response.StatusCode = resp.StatusCode;
                response.ContentType = resp.ContentType;
                response.ResponseUri = resp.ResponseUri;
                response.StatusDescription = resp.StatusDescription;
                if (!resp.CharacterSet.IsNullOrEmpty()) {
                    response.Encoding = Encoding.GetEncoding(resp.CharacterSet);
                    response.Content = response.Encoding.GetString(response.RawContent);
                }
                foreach (string key in resp.Headers.Keys) {
                    response.Headers.Add(key, resp.Headers.GetValues(key).Join(","));
                }
            }
            return response;
        }

        private void AddCommand_Get()
        {
            Function commandGet = new Function()
            {
                Name = "get",
                Comment = "Http get",
                Func = (args, caller) => {
                    if (args.ContainsKey("0")) {
                        string url = args.GetString("0");

                        caller.Logger.Info($"Http Get");
                        if (!Uri.TryCreate(url, UriKind.Absolute, out Uri requestUri)) {
                            requestUri = new Uri($"http://{url}");
                        }

                        if (requestUri.Scheme.In(Uri.UriSchemeHttp, Uri.UriSchemeHttps)) {
                            Response response = ReadFromHttp(requestUri.AbsoluteUri);

                            if (response.ErrorMessage.IsNullOrEmpty()) {
                                var dict = new Dictionary<string, string>();

                                dict.Add("Uri", response.ResponseUri.AbsoluteUri);
                                dict.Add("StatusCode", ((int)response.StatusCode).ToString());
                                dict.Add("StatusDescription", response.StatusDescription);

                                dict.Add("ContentType", response.ContentType);

                                if (response.Encoding != null) {
                                    dict.Add("Encoding", response.Encoding.EncodingName);
                                }

                                if (response.ContentLength > 0) {
                                    //  dict.Add("Content", response.ContentLength > 512 ? response.Content.Substring(512) : response.Content);
                                }
                                // dict.Add("RawContent", response.RawContent.Select(x => ((int)x).ToString()).Aggregate((x, y) => x + y));
                                dict.Add("RawContentLength", response.RawContentLength.ToString());

                                caller.Logger.Info(dict);

                                caller.Logger.Info("Headers");
                                caller.Logger.Info(response.Headers);
                            } else {
                                var dict = new Dictionary<string, string>();

                                dict.Add("Error", response.ErrorMessage);

                                caller.Logger.Error(dict);
                            }
                        }
                    }
                    return 0;
                }
            };

            this.Server.AddCommand(commandGet);
        }

        private void AddCommand_Ping()
        {
            Function commandPing = new Function()
            {
                Name = "ping",
                Comment = "Ping an address",
                Func = (args, caller) => {
                    if (args.ContainsKey("0")) {
                        string IPAddress = args.GetString("0");

                        caller.Logger.Info($"Ping - {IPAddress}");
                        for (int i = 0; i < 1; i++) {
                            caller.Logger.Info(Ping(IPAddress));
                        }
                    }
                    return 0;
                }
            };

            this.Server.AddCommand(commandPing);
        }
    }
}