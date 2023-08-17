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
using System.Text;

namespace TOS.Common
{
    internal class HttpResponse : IDisposable
    {
        private static IDictionary<string, string> _transEncodingKeys;
        private bool _disposed;
        private int _statusCode;
        private IDictionary<string, string> _header;
        private bool _autoClose = true;
        private Stream _body;


        static HttpResponse()
        {
            _transEncodingKeys = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            _transEncodingKeys[Constants.HeaderContentDisposition] = string.Empty;
        }

        internal HttpResponse(HttpWebResponse resp, int socketTimeout)
        {
            if (resp != null)
            {
                _statusCode = Convert.ToInt32(resp.StatusCode);
                string[] values;
                foreach (string key in resp.Headers)
                {
                    values = resp.Headers.GetValues(key);
                    if (values != null && values.Length > 0)
                    {
                        this.Header[key] = HandleValue(key, values[0]);
                    }
                }

                this.Body = new SocketTimeoutStream(resp.GetResponseStream(), socketTimeout);
            }
        }

        private string HandleValue(string key, string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }

            if (!_transEncodingKeys.ContainsKey(key))
            {
                return value;
            }

            try
            {
                return Encoding.UTF8.GetString(Encoding.GetEncoding(Constants.Iso8859Encoding).GetBytes(value));
            }
            catch (Exception ex)
            {
                return value;
            }
        }

        internal IDictionary<string, string> Header
        {
            get { return _header ?? (_header = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)); }
            set { this._header = value; }
        }

        internal int StatusCode
        {
            get { return _statusCode; }
        }

        internal Stream Body
        {
            get { return _body ?? (_body = new MemoryStream()); }
            set { this._body = value; }
        }

        internal bool AutoClose
        {
            get { return this._autoClose; }
            set { this._autoClose = value; }
        }

        public void Dispose()
        {
            try
            {
                if (_disposed)
                {
                    return;
                }

                if (this._body != null)
                {
                    if (this.AutoClose)
                    {
                        try
                        {
                            Utils.WriteTo(this._body, Stream.Null, Constants.MaxDiscardSize,
                                Constants.DefaultBufferSize);
                        }
                        catch (Exception ex)
                        {
                            // ignore exception
                        }

                        this._body.Close();
                    }

                    this._body = null;
                }

                _disposed = true;
            }
            finally
            {
                GC.SuppressFinalize(this);
            }
        }
    }
}