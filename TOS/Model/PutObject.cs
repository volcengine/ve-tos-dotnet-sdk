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

namespace TOS.Model
{
    public class PutObjectBasicInput : GenericObjectInput, IHttpBasicHeader, IAclHeader, ISseHeader, IMiscHeader
    {
        private long _contentLength = -1;

        internal sealed override string GetOperation()
        {
            return "PutObject";
        }

        public string ContentMD5 { set; get; }
        public string ContentSHA256 { set; get; }

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

        public string SSECAlgorithm { set; get; }

        public string SSECKey { set; get; }

        public string SSECKeyMD5 { set; get; }

        public string ServerSideEncryption { set; get; }

        public IDictionary<string, string> Meta { set; get; }

        public string WebsiteRedirectLocation { set; get; }

        public StorageClassType? StorageClass { set; get; }
        
        public string Callback { set; get; }
        public string CallbackVar { set; get; }

        internal override HttpRequest Trans()
        {
            HttpRequest request = base.Trans();
            request.Method = HttpMethodType.HttpMethodPut;
            if (!string.IsNullOrEmpty(ContentMD5))
            {
                request.Header[Constants.HeaderContentMD5] = ContentMD5;
            }

            if (!string.IsNullOrEmpty(ContentSHA256))
            {
                request.Header[Constants.HeaderContentSHA256] = ContentSHA256;
            }

            if (!string.IsNullOrEmpty(Callback))
            {
                request.Header[Constants.HeaderCallback] = Callback;
            }
            
            if (!string.IsNullOrEmpty(CallbackVar))
            {
                request.Header[Constants.HeaderCallbackVar] = CallbackVar;
            }
            
            Utils.SetHttpBasicHeader(request.Header, this);
            Utils.SetAclHeader(request.Header, this);
            Utils.SetSseHeader(request.Header, this);
            Utils.SetMetaHeader(request.Header, Meta);
            Utils.SetMiscHeader(request.Header, this);

            return request;
        }
    }

    public class PutObjectInput : PutObjectBasicInput
    {
        public Stream Content { set; get; }

        internal sealed override HttpRequest Trans()
        {
            HttpRequest request = base.Trans();
            request.Body = Content;
            Utils.TrySetContentLength(request.Header, request.Body);
            return request;
        }
    }

    public class PutObjectOutput : GenericOutput
    {
        public string ETag { internal set; get; }

        public string SSECAlgorithm { internal set; get; }

        public string SSECKeyMD5 { internal set; get; }

        public string VersionID { internal set; get; }

        public ulong HashCrc64ecma { internal set; get; }
        
        public string CallbackResult { internal set; get; }

        internal sealed override void Parse(HttpRequest request, HttpResponse response)
        {
            string temp;
            response.Header.TryGetValue(Constants.HeaderETag, out temp);
            ETag = temp;

            response.Header.TryGetValue(Constants.HeaderSSECAlgorithm, out temp);
            SSECAlgorithm = temp;

            response.Header.TryGetValue(Constants.HeaderSSECKeyMD5, out temp);
            SSECKeyMD5 = temp;

            response.Header.TryGetValue(Constants.HeaderVersionID, out temp);
            VersionID = temp;

            response.Header.TryGetValue(Constants.HeaderHashCrc64ecma, out temp);
            if (!string.IsNullOrEmpty(temp))
            {
                HashCrc64ecma = Convert.ToUInt64(temp);
            }

            request.Header.TryGetValue(Constants.HeaderCallback, out temp);
            if (!string.IsNullOrEmpty(temp))
            {
                CallbackResult = Utils.GetStreamString(response.Body);
            }
        }
    }
}