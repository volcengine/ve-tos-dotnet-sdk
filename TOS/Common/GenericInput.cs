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

using TOS.Error;

namespace TOS.Common
{
    public abstract class GenericInput
    {
        internal virtual string GetOperation()
        {
            return "Generic";
        }

        internal virtual HttpRequest Trans()
        {
            throw new TosClientException("invoke GenericInput.Trans");
        }
        
#if HTTPCLIENT
        public System.Threading.CancellationTokenSource Source
        {
            set;
            internal get;
        }
#endif
    }

    public abstract class GenericBucketInput : GenericInput
    {
        private string _bucket;

        public string Bucket
        {
            get
            {
                if (string.IsNullOrEmpty(this._bucket))
                {
                    return string.Empty;
                }

                return this._bucket;
            }
            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    this._bucket = value.Trim();
                }
            }
        }

        internal override HttpRequest Trans()
        {
            HttpRequest request = new HttpRequest();
            request.Operation = this.GetOperation();
            request.Bucket = this.Bucket;
            return request;
        }
    }

    public abstract class GenericObjectInput : GenericBucketInput
    {
        public string Key { get; set; }

        internal override HttpRequest Trans()
        {
            HttpRequest request = base.Trans();
            request.Key = Key;
            return request;
        }
    }
}