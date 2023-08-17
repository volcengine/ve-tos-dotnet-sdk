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
using TOS.Common;
using TOS.Error;

namespace TOS.Model
{
    public class AppendObjectInput : GenericObjectInput, IAclHeader, IHttpBasicHeader, IMiscHeader
    {
        private long _contentLength = -1;

        internal sealed override string GetOperation()
        {
            return "AppendObject";
        }

        public long Offset { set; get; }

        public Stream Content { set; get; }

        public long ContentLength
        {
            get { return _contentLength; }

            set
            {
                if (value >= 0)
                {
                    _contentLength = value;
                }
            }
        }

        public string CacheControl { get; set; }
        public string ContentDisposition { get; set; }
        public string ContentEncoding { get; set; }
        public string ContentLanguage { get; set; }
        public string ContentType { get; set; }
        public DateTime? Expires { get; set; }

        public ACLType? ACL { set; get; }

        public string GrantFullControl { set; get; }

        public string GrantRead { set; get; }

        public string GrantReadAcp { set; get; }

        string IAclHeader.GrantWrite { get; }

        public string GrantWriteAcp { set; get; }

        public IDictionary<string, string> Meta { set; get; }

        public string WebsiteRedirectLocation { set; get; }

        public StorageClassType? StorageClass { set; get; }

        internal sealed override HttpRequest Trans()
        {
            HttpRequest request = base.Trans();
            request.Method = HttpMethodType.HttpMethodPost;
            if (Content == null)
            {
                throw new TosClientException("empty content for append object");
            }

            if (Offset < 0)
            {
                throw new TosClientException("invalid offset for append object");
            }

            Utils.SetHttpBasicHeader(request.Header, this);
            Utils.SetAclHeader(request.Header, this);
            Utils.SetMetaHeader(request.Header, Meta);
            Utils.SetMiscHeader(request.Header, this);
            request.Body = Content;
            Utils.TrySetContentLength(request.Header, request.Body);

            request.Query["append"] = string.Empty;
            request.Query["offset"] = Convert.ToString(Offset);

            return request;
        }
    }

    public class AppendObjectOutput : GenericOutput
    {
        public long NextAppendOffset { internal set; get; }

        public ulong HashCrc64ecma { internal set; get; }

        internal override void Parse(HttpRequest request, HttpResponse response)
        {
            string temp;
            response.Header.TryGetValue(Constants.HeaderNextAppendOffset, out temp);
            if (!string.IsNullOrEmpty(temp))
            {
                NextAppendOffset = Convert.ToInt64(temp);
            }

            response.Header.TryGetValue(Constants.HeaderHashCrc64ecma, out temp);
            if (!string.IsNullOrEmpty(temp))
            {
                HashCrc64ecma = Convert.ToUInt64(temp);
            }
        }
    }
}