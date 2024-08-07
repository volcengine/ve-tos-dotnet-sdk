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
using System.IO;
using System.Net;
using System.Net.Security;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using TOS.Config;
using TOS.Model;

namespace TOS.Common
{
    internal partial class HttpClient
    {
        private static volatile MethodInfo _addHeaderInternal;
        private static readonly object _lock = new object();
        private static readonly string _addHeaderInternalMethodName = "AddInternal";
        private static readonly string _addHeaderMethodName = "Add";
        private static volatile ConfigHolder _lastConfigHolder;

        private HttpWebRequest CreateHttpWebRequest(HttpRequest request)
        {
            this._signer.SignHeader(request, this._configHolder.Credential, this._configHolder.RegionEndpoint);
            HttpWebRequest req =
                this.CreateHttpWebRequest(this.GetRequestUrl(request), Enums.TransEnum(request.Method));
            this.AddHeaders(req, request);
            return req;
        }

        private string GetRequestUrl(HttpRequest request)
        {
            string requestUrl =
                this._configHolder.RegionEndpoint.GetEndpoint(request.Bucket, request.Key);

            if (request.Query.Count > 0)
            {
                requestUrl += "?";
                foreach (KeyValuePair<string, string> entry in request.Query)
                {
                    requestUrl += entry.Key + "=" + entry.Value + "&";
                }

                requestUrl = requestUrl.Substring(0, requestUrl.Length - 1);
            }

            return requestUrl;
        }

        private HttpWebRequest CreateHttpWebRequest(string requestUrl, string method)
        {
            ConfigureServicePointManager(this._configHolder);
            HttpWebRequest req = WebRequest.Create(requestUrl) as HttpWebRequest;
            req.Timeout = Timeout.Infinite;
            req.ReadWriteTimeout = Timeout.Infinite;
            try
            {
                ServicePoint sp = req.ServicePoint;
                sp.ReceiveBufferSize = Constants.DefaultBufferSize;
                sp.ConnectionLimit = this._configHolder.MaxConnections;
                sp.MaxIdleTime = this._configHolder.IdleConnectionTime;
                sp.ConnectionLeaseTimeout = Timeout.Infinite;
            }
            catch (Exception ex)
            {
                // ignore exception
            }

            // no redirect
            req.AllowAutoRedirect = false;
            req.UserAgent = this._configHolder.UserAgent;
            req.Method = method;
            return req;
        }

        private void AddHeaders(HttpWebRequest req, HttpRequest request)
        {
            string contentLengthString = null;
            MethodInfo method = GetAddHeaderInternal();
            foreach (KeyValuePair<string, string> entry in request.Header)
            {
                if (entry.Key.Equals(Constants.HeaderContentLength, StringComparison.OrdinalIgnoreCase))
                {
                    contentLengthString = entry.Value;
                    continue;
                }

                method.Invoke(req.Headers, new object[] { entry.Key, entry.Value });
            }

            this.SetContentLength(req, request, contentLengthString);
        }

        private void SetContentLength(HttpWebRequest req, HttpRequest request, string contentLengthString)
        {
            if (request.IsWriteMethod)
            {
                long contentLength = request.Body == null ? 0 : -1;
                if (!string.IsNullOrEmpty(contentLengthString))
                {
                    // never failed
                    contentLength = long.Parse(contentLengthString);
                }

                if (contentLength >= 0)
                {
                    req.ContentLength = contentLength;
                    if (req.ContentLength > 0)
                    {
                        req.AllowWriteStreamBuffering = false;
                    }
                }
                else
                {
                    req.SendChunked = true;
                    req.AllowWriteStreamBuffering = false;
                }
            }
        }

        private static bool SkipConfigureServicePointManage(ConfigHolder lch, ConfigHolder configHolder)
        {
            if (configHolder == null)
            {
                return true;
            }

            if (lch == null)
            {
                return false;
            }

            return lch.MaxConnections == configHolder.MaxConnections &&
                   lch.IdleConnectionTime == configHolder.IdleConnectionTime
                   && lch.SecurityProtocolType == configHolder.SecurityProtocolType &&
                   lch.EnableVerifySSL == configHolder.EnableVerifySSL;
        }

        private static void ConfigureServicePointManager(ConfigHolder configHolder)
        {
            if (SkipConfigureServicePointManage(_lastConfigHolder, configHolder))
            {
                return;
            }

            lock (_lock)
            {
                if (SkipConfigureServicePointManage(_lastConfigHolder, configHolder))
                {
                    return;
                }

                ServicePointManager.DefaultConnectionLimit = configHolder.MaxConnections;
                ServicePointManager.MaxServicePoints = configHolder.MaxConnections;
                ServicePointManager.MaxServicePointIdleTime = configHolder.IdleConnectionTime;
                if (configHolder.SecurityProtocolType > 0)
                {
                    ServicePointManager.SecurityProtocol = configHolder.SecurityProtocolType;
                }

                if (!configHolder.EnableVerifySSL)
                {
                    ServicePointManager.ServerCertificateValidationCallback =
                        delegate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors errors)
                        {
                            return true;
                        };
                }

                _lastConfigHolder = configHolder;
            }
        }

        private static MethodInfo GetAddHeaderInternal()
        {
            if (_addHeaderInternal == null)
            {
                lock (_lock)
                {
                    if (_addHeaderInternal == null)
                    {
                        Type t = typeof(WebHeaderCollection);
                        Type[] types = new Type[] { typeof(string), typeof(string) };
                        _addHeaderInternal = t.GetMethod(_addHeaderInternalMethodName,
                            BindingFlags.NonPublic | BindingFlags.Instance,
                            null, types, null);
                        if (_addHeaderInternal == null)
                        {
                            _addHeaderInternal = t.GetMethod(_addHeaderMethodName,
                                BindingFlags.Public | BindingFlags.Instance,
                                null, types, null);
                        }
                    }
                }
            }

            return _addHeaderInternal;
        }
    }
}