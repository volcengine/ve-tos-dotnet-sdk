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
using TOS.Model;

namespace TOS.Common
{
    internal class HttpRequest : IDisposable
    {
        private bool _disposed;
        private IDictionary<string, string> _header;
        private IDictionary<string, string> _query;

        internal string Operation { get; set; }

        internal HttpMethodType Method { get; set; }

        internal string Bucket { get; set; }

        internal string Key { get; set; }

        internal IDictionary<string, string> Header
        {
            get { return _header ?? (_header = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)); }
            set { this._header = value; }
        }

        internal IDictionary<string, string> Query
        {
            get { return _query ?? (_query = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)); }
            set { this._query = value; }
        }

        internal bool IsWriteMethod
        {
            get
            {
                return this.Method == HttpMethodType.HttpMethodPut || this.Method == HttpMethodType.HttpMethodPost ||
                       this.Method == HttpMethodType.HttpMethodDelete;
            }
        }

        internal bool AutoClose { get; set; }

        internal Stream Body { get; set; }

        internal object AdditionalState { get; set; }

        public void Dispose()
        {
            try
            {
                if (_disposed)
                {
                    return;
                }

                if (this.Body != null)
                {
                    if (this.AutoClose)
                    {
                        this.Body.Close();
                    }

                    this.Body = null;
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