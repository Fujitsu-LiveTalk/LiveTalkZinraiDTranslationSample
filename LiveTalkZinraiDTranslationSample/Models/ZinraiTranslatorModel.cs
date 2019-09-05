/*
 * Copyright 2019 FUJITSU SOCIAL SCIENCE LABORATORY LIMITED
 * クラス名　：ZinraiTranslatorModel
 * 概要      ：Zinrai 文書翻訳 APIと連携
*/
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace LiveTalkZinraiDTranslationSample.Models
{
    public class ZinraiTranslatorModel
    {
        private const string AuthBody = "grant_type=client_credentials&scope=service_contract&client_id={0}&client_secret={1}";
        private const string UrlString = "https://zinrai-pf.jp-east-1.paas.cloud.global.fujitsu.com/DocumentTranslation/v1/translations?langFrom={0}&langTo={1}&profile=default";
        private string ClientID = " <<<<<client_id>>>>>";
        private string ClientPassword = " <<<<<client_secret>>>>>";
        private string ProxyServer = "";    // PROXY経由なら proxy.hogehoge.jp:8080 のように指定
        private string ProxyId = "";        // 認証PROXYならIDを指定
        private string ProxyPassword = "";  // 認証PROXYならパスワードを指定
        private string AccessToken = string.Empty;
        private string LastAccessTokenError = null;
        private DateTime TokenExpireUtcTime = DateTime.MinValue;

        /// <summary>
        /// アクセストークンを取得する
        /// </summary>
        /// <returns></returns>
        public async Task GetToken()
        {
            this.AccessToken = await GetAccessTokenAsync().ConfigureAwait(false);
            Console.WriteLine("Successfully obtained an access token. \n");
        }

        /// <summary>
        /// テキストを翻訳する
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        internal async Task<string> GetTranslation(string message, string orignalLangCode, string langCode)
        {
            var translation = string.Empty;
            this.AccessToken = await GetAccessTokenAsync();

            try
            {
                // プロキシ設定
                var ch = new HttpClientHandler() { UseCookies = true };
                if (!string.IsNullOrEmpty(this.ProxyServer))
                {
                    var proxy = new System.Net.WebProxy(this.ProxyServer);
                    if (!string.IsNullOrEmpty(this.ProxyId) && !string.IsNullOrEmpty(this.ProxyPassword))
                    {
                        proxy.Credentials = new System.Net.NetworkCredential(this.ProxyId, this.ProxyPassword);
                    }
                    ch.Proxy = proxy;
                }
                else
                {
                    ch.Proxy = null;
                }

                // Web API呼び出し
                using (var client = new HttpClient(ch))
                {
                    using (var request = new HttpRequestMessage())
                    {
                        var questionJsonString = "";
                        {
                            using (var ms = new MemoryStream())
                            {
                                var serializer = new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(TTranslatorItem));
                                var body = new TTranslatorItem() { source = message };
                                using (var sr = new StreamReader(ms))
                                {
                                    serializer.WriteObject(ms, body);
                                    ms.Position = 0;
                                    questionJsonString = sr.ReadToEnd();
                                }
                            }
                        }
                        request.Method = HttpMethod.Post;
                        request.RequestUri = new Uri(string.Format(UrlString, orignalLangCode, langCode));
                        request.Headers.Add("X-Access-Token", this.AccessToken);
                        request.Headers.Add("X-Service-Code", "FJAI000015-00001");
                        request.Content = new StringContent(questionJsonString, Encoding.UTF8, "application/json");
                        request.Headers.Add("Connection", "close");
                        client.Timeout = TimeSpan.FromSeconds(10);
                        using (var response = await client.SendAsync(request))
                        {
                            response.EnsureSuccessStatusCode();
                            var jsonString = await response.Content.ReadAsStringAsync();
                            translation = jsonString;
                            using (var json = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(jsonString)))
                            {
                                var ser = new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(TResult));
                                {
                                    var result = ser.ReadObject(json) as TResult;
                                    translation = result.response.translation;
#if !WINDOWS_UWP
                                    json.Close();
#endif
                                }
                            }
                        }
                    }
                }
                this.LastAccessTokenError = "";
            }
            catch (Exception ex)
            {
                if (ex.Message != this.LastAccessTokenError)
                {
                    System.Diagnostics.Debug.WriteLine(ex.Message);
                    this.LastAccessTokenError = ex.Message;
                }
            }
            return translation;
        }

        /// <summary>
        /// K5のOAuth認証を行う
        /// </summary>
        /// <returns></returns>
        private async Task<string> GetAccessTokenAsync()
        {
            string token = null;

            if (this.TokenExpireUtcTime >= DateTime.UtcNow)
            {
                token = this.AccessToken;
                return token;
            }
            try
            {
                // プロキシ設定
                var ch = new HttpClientHandler() { UseCookies = true };
                if (!string.IsNullOrEmpty(this.ProxyServer))
                {
                    var proxy = new System.Net.WebProxy(this.ProxyServer);
                    if (!string.IsNullOrEmpty(this.ProxyId) && !string.IsNullOrEmpty(this.ProxyPassword))
                    {
                        proxy.Credentials = new System.Net.NetworkCredential(this.ProxyId, this.ProxyPassword);
                    }
                    ch.Proxy = proxy;
                }
                else
                {
                    ch.Proxy = null;
                }

                // 認証呼び出し
                using (var client = new HttpClient(ch))
                {
                    using (var request = new HttpRequestMessage())
                    {
                        request.Method = HttpMethod.Post;
                        request.RequestUri = new Uri("https://auth-api.jp-east-1.paas.cloud.global.fujitsu.com/API/oauth2/token?key=" + DateTime.Now.ToString("HHmmss"));
                        request.Content = new StringContent(string.Format(AuthBody, this.ClientID, this.ClientPassword), Encoding.UTF8, "application/x-www-form-urlencoded");
                        request.Headers.Add("Connection", "close");
                        client.Timeout = TimeSpan.FromSeconds(10);
                        var response = await client.SendAsync(request);
                        var jsonString = await response.Content.ReadAsStringAsync();
                        using (var json = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(jsonString)))
                        {
                            var ser = new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(TFujitsuK5Auth));
                            {
                                var result = ser.ReadObject(json) as TFujitsuK5Auth;
                                token = result.access_token;
                                this.TokenExpireUtcTime = DateTime.UtcNow.AddSeconds(result.expires_in - 60);
                            }
                        }
                    }
                }
                this.LastAccessTokenError = "";
            }
            catch (Exception ex)
            {
                if (ex.Message != this.LastAccessTokenError)
                {
                    System.Diagnostics.Debug.WriteLine(ex.Message);
                    this.LastAccessTokenError = ex.Message;
                }
                this.TokenExpireUtcTime = DateTime.MinValue;
            }
            return token;
        }

        #region "K5認証"
        [DataContract]
        public class TFujitsuK5Auth
        {
            [DataMember]
            public string access_token { get; set; }
            [DataMember]
            public string token_type { get; set; }
            [DataMember]
            public int expires_in { get; set; }
            [DataMember]
            public string scope { get; set; }
            [DataMember]
            public string client_id { get; set; }
            [DataMember]
            public TContract_Info contract_info { get; set; }
        }

        [DataContract]
        public class TContract_Info
        {
            [DataMember]
            public TContract_List[] contract_list { get; set; }
        }

        [DataContract]
        public class TContract_List
        {
            [DataMember]
            public string service_contract_id { get; set; }
            [DataMember]
            public string service_code { get; set; }
        }
        #endregion

        #region "Zinrai文書翻訳"
        [DataContract]
        public class TTranslatorItem
        {
            [DataMember]
            public string source { get; set; }
        }

        [DataContract]
        public class TResult
        {
            [DataMember]
            public Response response { get; set; }
        }

        [DataContract]
        public class Response
        {
            [DataMember]
            public string translation { get; set; }
        }
        #endregion
    }
}

