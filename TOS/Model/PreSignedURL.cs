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
using TOS.Common;

namespace TOS.Model
{
    public class PreSignedURLInput
    {
        private IDictionary<string, string> _header;
        private IDictionary<string, string> _query;
        private int _expires = Constants.DefaultExpires;
        public HttpMethodType? HttpMethod { get; set; }

        public string Bucket { get; set; }

        public string Key { get; set; }

        public int Expires
        {
            get { return _expires; }
            set
            {
                if (value > 0)
                {
                    _expires = value;
                }
            }
        }

        public IDictionary<string, string> Header
        {
            get { return _header ?? (_header = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)); }
            set { this._header = value; }
        }

        public IDictionary<string, string> Query
        {
            get { return _query ?? (_query = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)); }
            set { this._query = value; }
        }

        public string AlternativeEndpoint { get; set; }

        internal HttpRequest Trans()
        {
            HttpRequest request = new HttpRequest();
            request.Operation = "PreSignedURL";
            request.Method = HttpMethod ?? HttpMethodType.HttpMethodGet;

            if (!string.IsNullOrEmpty(Bucket))
            {
                Utils.CheckBucket(Bucket);
                request.Bucket = Bucket;
            }

            if (!string.IsNullOrEmpty(Key))
            {
                Utils.CheckKey(Key);
                request.Key = Key;
            }

            foreach (KeyValuePair<string, string> entry in Query)
            {
                request.Query.Add(entry.Key, entry.Value);
            }

            foreach (KeyValuePair<string, string> entry in Header)
            {
                request.Header.Add(entry.Key, entry.Value);
            }

            request.AdditionalState = AlternativeEndpoint;

            return request;
        }
    }

    public class PreSignedURLOutput
    {
        private IDictionary<string, string> _signedHeader;
        public string SignedUrl { get; internal set; }

        public IDictionary<string, string> SignedHeader
        {
            get
            {
                return _signedHeader ??
                       (_signedHeader = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));
            }
            internal set { this._signedHeader = value; }
        }
    }
}