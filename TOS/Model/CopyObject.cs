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
using TOS.Error;

namespace TOS.Model
{
    public class CopyObjectInput : GenericObjectInput, IAclHeader, IHttpBasicHeader, IMiscHeader, ISseHeader,
        ICopySourceHeader, ICopySourceSSeHeader, ICopySourceIfConditionHeader
    {
        private IHttpBasicHeader _httpBasicHeaderImplementation;

        internal sealed override string GetOperation()
        {
            return "CopyObject";
        }

        public string SrcBucket { set; get; }

        public string SrcKey { set; get; }

        public string SrcVersionID { set; get; }

        long IHttpBasicHeader.ContentLength { get; }

        public string CacheControl { get; set; }

        public string ContentDisposition { get; set; }

        public string ContentEncoding { get; set; }

        public string ContentLanguage { get; set; }

        public string ContentType { get; set; }

        public DateTime? Expires { get; set; }

        public string CopySourceIfMatch { get; set; }

        public DateTime? CopySourceIfModifiedSince { get; set; }

        public string CopySourceIfNoneMatch { get; set; }

        public DateTime? CopySourceIfUnmodifiedSince { get; set; }

        public string CopySourceSSECAlgorithm { get; set; }

        public string CopySourceSSECKey { get; set; }

        public string CopySourceSSECKeyMD5 { get; set; }

        public string SSECAlgorithm { set; get; }

        public string SSECKey { set; get; }

        public string SSECKeyMD5 { set; get; }

        public string ServerSideEncryption { set; get; }

        public ACLType? ACL { set; get; }

        public string GrantFullControl { set; get; }

        public string GrantRead { set; get; }

        public string GrantReadAcp { set; get; }

        string IAclHeader.GrantWrite { get; }

        public string GrantWriteAcp { set; get; }

        public IDictionary<string, string> Meta { set; get; }

        public string WebsiteRedirectLocation { set; get; }

        public StorageClassType? StorageClass { set; get; }

        public MetadataDirectiveType? MetadataDirective { set; get; }

        internal sealed override HttpRequest Trans()
        {
            HttpRequest request = base.Trans();
            request.Method = HttpMethodType.HttpMethodPut;
            Utils.SetCopySourceHeader(request.Header, this);
            Utils.SetCopySourceIfConditionHeader(request.Header, this);
            Utils.SetHttpBasicHeader(request.Header, this);
            Utils.SetSseHeader(request.Header, this);
            Utils.SetCopySourceSseHeader(request.Header, this);
            Utils.SetAclHeader(request.Header, this);
            Utils.SetMetaHeader(request.Header, Meta);
            Utils.SetMiscHeader(request.Header, this);
            if (MetadataDirective.HasValue)
            {
                request.Header[Constants.HeaderMetadataDirective] = Enums.TransEnum(MetadataDirective.Value);
            }

            return request;
        }
    }

    public class CopyObjectOutput : GenericOutput
    {
        public string ETag { internal set; get; }

        public DateTime? LastModified { internal set; get; }

        public string SSECAlgorithm { internal set; get; }

        public string SSECKeyMD5 { internal set; get; }

        public string VersionID { internal set; get; }

        public string CopySourceVersionID { internal set; get; }

        internal sealed override void Parse(HttpRequest request, HttpResponse response)
        {
            JObject json = Utils.ParseJson(response.Body);
            if (json.ContainsKey("ETag"))
            {
                ETag = json["ETag"]?.Value<string>();
                LastModified = Utils.ParseJTokenDate(json["LastModified"], Constants.Iso8601DateFormat);

                string temp;
                response.Header.TryGetValue(Constants.HeaderSSECAlgorithm, out temp);
                SSECAlgorithm = temp;

                response.Header.TryGetValue(Constants.HeaderSSECKeyMD5, out temp);
                SSECKeyMD5 = temp;

                response.Header.TryGetValue(Constants.HeaderVersionID, out temp);
                VersionID = temp;

                response.Header.TryGetValue(Constants.HeaderCopySourceVersionID, out temp);
                CopySourceVersionID = temp;
                return;
            }

            throw new TosServerException(json["Message"]?.Value<string>(), base._requestInfo)
            {
                Code = json["Code"]?.Value<string>(),
                HostID = json["HostId"]?.Value<string>(),
                Resource = json["Resource"]?.Value<string>()
            };
        }
    }
}