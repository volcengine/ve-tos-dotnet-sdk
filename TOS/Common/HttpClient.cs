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

using System;
using System.Collections.Generic;
using System.Net;
using TOS.Auth;
using TOS.Config;

namespace TOS.Common
{
    internal partial class HttpClient : IHttpClient
    {
        protected Signer _signer;
        protected ConfigHolder _configHolder;

        internal HttpClient(Signer signer, ConfigHolder configHolder)
        {
            this._signer = signer;
            this._configHolder = configHolder;
        }

        internal HttpAsyncResult BeginDoRequest(HttpRequest request, AsyncCallback callback, object state)
        {
            HttpWebRequest req = this.CreateHttpWebRequest(request);
            HttpContext context = new HttpContext()
            {
                HttpWebRequest = req,
                HttpRequest = request,
                SocketTimeout = this._configHolder.SocketTimeout,
            };
            HttpAsyncResult result = new HttpAsyncResult(callback, state, context);
            if (request.Body != null && request.IsWriteMethod)
            {
                req.BeginGetRequestStream(result.GetRequestStreamCallback, null);
            }
            else
            {
                req.BeginGetResponse(result.GetResponseCallback, null);
            }

            return result;
        }

        internal HttpResponse EndDoRequest(HttpAsyncResult result)
        {
            DateTime start = DateTime.UtcNow;
            int requestTimeout = this._configHolder.RequestTimeout;
            result.CheckConnect(this._configHolder.ConnectionTimeout);
            if (requestTimeout > 0)
            {
                TimeSpan elapsedSpan = new TimeSpan(DateTime.UtcNow.Ticks - start.Ticks);
                requestTimeout -= (int)elapsedSpan.TotalMilliseconds;
                if (requestTimeout <= 0)
                {
                    result.Abort(new TimeoutException("request timeout"));
                }
            }

            return result.Get(requestTimeout);
        }

        public virtual HttpResponse DoRequest(HttpRequest request)
        {
            return this.EndDoRequest(this.BeginDoRequest(request, null, null));
        }

        public string GenPreSignedURL(HttpRequest request, int expires, out IDictionary<string, string> signedHeader)
        {
            string alternativeEndpoint = request.AdditionalState as string;
            SchemaDomain schemaDomain = new SchemaDomain();
            if (!string.IsNullOrEmpty(alternativeEndpoint))
            {
                schemaDomain = RegionEndpoint.SplitEndpoint(alternativeEndpoint);
            }

            var isCustomDomain = request.IsCustomDomain ?? false;

            signedHeader = this._signer.SignQuery(request, this._configHolder.Credential,
                this._configHolder.RegionEndpoint, expires, schemaDomain.Domain, isCustomDomain);

            var bucket = request.Bucket;
            if (isCustomDomain)
            {
                bucket = string.Empty;
            }

            string signedUrl = this._configHolder.RegionEndpoint.GetEndpoint(bucket, request.Key,
                schemaDomain.Schema, schemaDomain.Domain, true);

            if (request.Query.Count > 0)
            {
                signedUrl += "?";
                if (this._configHolder.Credential.IsAnonymous)
                {
                    foreach (KeyValuePair<string, string> entry in request.Query)
                    {
                        signedUrl += Utils.UrlEncode(entry.Key) + "=" + Utils.UrlEncode(entry.Value) + "&";
                    }
                }
                else
                {
                    foreach (KeyValuePair<string, string> entry in request.Query)
                    {
                        signedUrl += entry.Key + "=" + entry.Value + "&";
                    }
                }

                signedUrl = signedUrl.Substring(0, signedUrl.Length - 1);
            }

            return signedUrl;
        }

        public virtual void Dispose()
        {
        }
    }
}