/*
 * Copyright 2019 FUJITSU SOCIAL SCIENCE LABORATORY LIMITED
 * クラス名　：ZinraiTranslatorModel
 * 概要      ：Zinrai Translation Service APIと連携
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
        private const string UrlString = "<<<<<Zinrai Translation Service エンドポイント>>>>>"
        private const string OAuthUrlString = "<<<<<Zinrai Translation Service エンドポイント>>>>>"
        private string ClientID = "<<<<<client_id>>>>>";
        private string ClientPassword = "<<<<<client_secret>>>>>";
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
                        request.RequestUri = new Uri(string.Format(UrlString + "?langFrom={0}&langTo={1}&profile=default", orignalLangCode, langCode));
                        request.Headers.Add("X-Access-Token", this.AccessToken);
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
                                    json.Close();
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
                        var authBodyJsonString = "";
                        {
                            using (var ms = new MemoryStream())
                            {
                                var serializer = new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(TAuthReq));
                                var body = new TAuthReq()
                                {
                                    clientId = this.ClientID,
                                    clientSecret = this.ClientPassword
                                };
                                using (var sr = new StreamReader(ms))
                                {
                                    serializer.WriteObject(ms, body);
                                    ms.Position = 0;
                                    authBodyJsonString = sr.ReadToEnd();
                                }
                            }
                        }
                        request.Method = HttpMethod.Post;
                        request.RequestUri = new Uri(OAuthUrlString);
                        request.Headers.Add("Connection", "close");
                        client.Timeout = TimeSpan.FromSeconds(10);
                        request.Content = new ByteArrayContent(Encoding.UTF8.GetBytes(authBodyJsonString)); // Content-Typeを無理やりつける
                        request.Content.Headers.TryAddWithoutValidation(@"Content-Type", @"application/json");
                        request.Headers.Add("Accept", "application/json");
                        var response = await client.SendAsync(request);
                        var jsonString = await response.Content.ReadAsStringAsync();
                        using (var json = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(jsonString)))
                        {
                            var ser = new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(TAuthRes));
                            {
                                var result = ser.ReadObject(json) as TAuthRes;
                                token = result.accessToken;
                                this.TokenExpireUtcTime = DateTime.UtcNow.AddSeconds(result.expiration - 60);
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

        #region "ZinraiTranslationService認証"
        public class TAuthReq
        {
            [DataMember]
            public string clientId { get; set; }
            [DataMember]
            public string clientSecret { get; set; }
        }

        [DataContract]
        public class TAuthRes
        {
            [DataMember]
            public string accessToken { get; set; }
            [DataMember]
            public string refreshToken { get; set; }
            [DataMember]
            public int expiration { get; set; }
            [DataMember]
            public string message { get; set; }
            [DataMember]
            public string code { get; set; }
        }
        #endregion

        #region "ZinraiTranslationService"
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

