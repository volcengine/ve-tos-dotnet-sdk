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
using Newtonsoft.Json.Linq;
using TOS.Common;

namespace TOS.Model
{
    public class CreateMultipartUploadInput : GenericObjectInput, IHttpBasicHeader, IAclHeader, ISseHeader, IMiscHeader
    {
        internal sealed override string GetOperation()
        {
            return "CreateMultipartUpload";
        }

        public string EncodingType { set; get; }

        long IHttpBasicHeader.ContentLength { get; }

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

        internal sealed override HttpRequest Trans()
        {
            HttpRequest request = base.Trans();
            request.Method = HttpMethodType.HttpMethodPost;

            Utils.SetHttpBasicHeader(request.Header, this);
            Utils.SetAclHeader(request.Header, this);
            Utils.SetSseHeader(request.Header, this);
            Utils.SetMetaHeader(request.Header, Meta);
            Utils.SetMiscHeader(request.Header, this);

            if (!string.IsNullOrEmpty(EncodingType))
            {
                request.Query[Constants.QueryEncodingType] = EncodingType;
            }

            request.Query["uploads"] = string.Empty;

            return request;
        }
    }

    public class CreateMultipartUploadOutput : GenericOutput
    {
        public string Bucket { internal set; get; }

        public string Key { internal set; get; }

        public string UploadID { internal set; get; }

        public string SSECAlgorithm { internal set; get; }

        public string SSECKeyMD5 { internal set; get; }

        public string EncodingType { internal set; get; }

        internal sealed override void Parse(HttpRequest request, HttpResponse response)
        {
            JObject json = Utils.ParseJson(response.Body);
            Bucket = json["Bucket"]?.Value<string>();
            Key = json["Key"]?.Value<string>();
            UploadID = json["UploadId"]?.Value<string>();
            EncodingType = json["EncodingType"]?.Value<string>();

            string temp;
            response.Header.TryGetValue(Constants.HeaderSSECAlgorithm, out temp);
            SSECAlgorithm = temp;

            response.Header.TryGetValue(Constants.HeaderSSECKeyMD5, out temp);
            SSECKeyMD5 = temp;
        }
    }
}