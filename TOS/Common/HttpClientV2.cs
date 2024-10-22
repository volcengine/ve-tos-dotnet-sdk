/*
 * Copyright (2023) Volcengine
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */


#if HTTPCLIENT
using System;
using System.IO;
using TOS.Auth;
using TOS.Config;
using TOS.Model;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Threading;
#endif

namespace TOS.Common
{
#if HTTPCLIENT

    internal class HttpClientV2 : HttpClient
    {
        private readonly System.Net.Http.HttpClient _httpClient;

        internal HttpClientV2(Signer signer, ConfigHolder configHolder) : base(signer, configHolder)
        {
            var socketsHandler = new SocketsHttpHandler();
            socketsHandler.AllowAutoRedirect = false;
            socketsHandler.AutomaticDecompression = DecompressionMethods.None;
            socketsHandler.MaxAutomaticRedirections = 1;
            socketsHandler.MaxConnectionsPerServer = configHolder.MaxConnections;
            socketsHandler.ConnectTimeout = TimeSpan.FromMilliseconds(configHolder.ConnectionTimeout);
            socketsHandler.PooledConnectionIdleTimeout = TimeSpan.FromMilliseconds(configHolder.IdleConnectionTime);
            socketsHandler.UseProxy = false;
            if (!configHolder.EnableVerifySSL)
                socketsHandler.SslOptions = new SslClientAuthenticationOptions
                {
                    RemoteCertificateValidationCallback = delegate { return true; }
                };

            var _httpClient = new System.Net.Http.HttpClient(socketsHandler);
            if (configHolder.RequestTimeout > 0)
                _httpClient.Timeout = TimeSpan.FromMilliseconds(configHolder.RequestTimeout);
            else
                _httpClient.Timeout = new TimeSpan(0, 0, 0, 0, Timeout.Infinite);
            this._httpClient = _httpClient;
        }

        public override HttpResponse DoRequest(HttpRequest request)
        {
            _signer.SignHeader(request, _configHolder.Credential, _configHolder.RegionEndpoint);
            var requestUrl = new Uri(GetRequestUrl(request));
            var method = new HttpMethod(Enums.TransEnum(request.Method));
            var req = new HttpRequestMessage(method, requestUrl);

            var body = request.Body;
            string contentLengthString = null;
            if (body == null)
            {
                body = new MemoryStream();
                contentLengthString = "0";
            }

            body = new SocketTimeoutStream(body, _configHolder.SocketTimeout);
            req.Content =
                new StreamContent(body);

            foreach (var entry in request.Header)
            {
                // Authorization 头域必须通过这个方式设置，否则会校验不通过
                if (entry.Key.Equals(Constants.HeaderAuthorization, StringComparison.OrdinalIgnoreCase))
                {
                    req.Headers.Authorization =
                        new AuthenticationHeaderValue(Signer.Algorithm,
                            entry.Value.Substring(Signer.Algorithm.Length));
                    continue;
                }

                // HttpBasic 头域必须设置到 req.Content.Headers 里面
                if (entry.Key.Equals(Constants.HeaderContentLength, StringComparison.OrdinalIgnoreCase))
                {
                    contentLengthString = entry.Value;
                    continue;
                }

                if (entry.Key.Equals(Constants.HeaderContentMD5, StringComparison.OrdinalIgnoreCase))
                {
                    req.Content.Headers.Add(entry.Key, entry.Value);
                    continue;
                }

                if (entry.Key.Equals(Constants.HeaderExpires, StringComparison.OrdinalIgnoreCase))
                {
                    req.Content.Headers.Add(entry.Key, entry.Value);
                    continue;
                }

                if (entry.Key.Equals(Constants.HeaderContentType, StringComparison.OrdinalIgnoreCase))
                {
                    req.Content.Headers.Add(entry.Key, entry.Value);
                    continue;
                }

                if (entry.Key.Equals(Constants.HeaderContentDisposition, StringComparison.OrdinalIgnoreCase))
                {
                    req.Content.Headers.Add(entry.Key, entry.Value);
                    continue;
                }

                if (entry.Key.Equals(Constants.HeaderContentEncoding, StringComparison.OrdinalIgnoreCase))
                {
                    req.Content.Headers.Add(entry.Key, entry.Value);
                    continue;
                }

                if (entry.Key.Equals(Constants.HeaderContentLanguage, StringComparison.OrdinalIgnoreCase))
                {
                    req.Content.Headers.Add(entry.Key, entry.Value);
                    continue;
                }
                if (entry.Key.Equals(Constants.HeaderIfMatch, StringComparison.OrdinalIgnoreCase) || entry.Key.Equals(Constants.HeaderIfNoneMatch, StringComparison.OrdinalIgnoreCase))
                {
                    var tmpValue = entry.Value;
                    // .Net Core 对 IfMatch 和 HeaderIfNoneMatch 会强制校验是否有引号 
                    if (!tmpValue.StartsWith(Constants.QuotationMarkStr) && !tmpValue.EndsWith(Constants.QuotationMarkStr))
                    {
                        tmpValue = Constants.QuotationMarkStr + tmpValue + Constants.QuotationMarkStr;
                    }
                    req.Headers.Add(entry.Key,tmpValue);
                    continue;
                }

                req.Headers.Add(entry.Key, entry.Value);
            }

            if (string.IsNullOrEmpty(contentLengthString))
            {
                req.Headers.TransferEncodingChunked = true;
            }
            else
            {
                // 如果设置了 ContentLength，必须设置 Stream 边界，否则会导致复用的链接有脏数据
                body.SetLength(long.Parse(contentLengthString));
                req.Content.Headers.Add(Constants.HeaderContentLength, contentLengthString);
            }

            req.Headers.Add(Constants.HeaderUserAgent, _configHolder.UserAgent);
            var token = request.Source?.Token ?? CancellationToken.None;
            var resp = _httpClient.Send(req, HttpCompletionOption.ResponseHeadersRead, token);
            var response = new HttpResponse(Convert.ToInt32(resp.StatusCode));
            foreach (var entry in resp.Headers)
            {
                string value;
                if (!string.IsNullOrEmpty(value = response.HandleValue(entry.Key, entry.Value)))
                    response.Header[entry.Key] = value;
            }

            // HttpBasic 头域必须从 resp.Content.Headers 里面获取
            foreach (var entry in resp.Content.Headers)
            {
                string value;
                if (!string.IsNullOrEmpty(value = response.HandleValue(entry.Key, entry.Value)))
                    response.Header[entry.Key] = value;
            }

            response.Body = new SocketTimeoutStream(resp.Content.ReadAsStream(), _configHolder.SocketTimeout);
            return response;
        }

        public override void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
#endif
}