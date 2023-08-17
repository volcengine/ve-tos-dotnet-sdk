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
    public class HeadObjectInput : GenericObjectInput, IIfConditionHeader, ISseHeader
    {
        internal sealed override string GetOperation()
        {
            return "HeadObject";
        }

        public string VersionID { set; get; }

        public string IfMatch { set; get; }

        public DateTime? IfModifiedSince { set; get; }

        public string IfNoneMatch { set; get; }

        public DateTime? IfUnmodifiedSince { set; get; }

        public string SSECAlgorithm { set; get; }

        public string SSECKey { set; get; }

        public string SSECKeyMD5 { set; get; }

        string ISseHeader.ServerSideEncryption { get; }

        internal sealed override HttpRequest Trans()
        {
            HttpRequest request = base.Trans();
            request.Method = HttpMethodType.HttpMethodHead;

            Utils.SetIfConditionHeader(request.Header, this);
            Utils.SetSseHeader(request.Header, this);
            if (!string.IsNullOrEmpty(VersionID))
            {
                request.Query[Constants.QueryVersionID] = VersionID;
            }

            return request;
        }
    }

    public class HeadObjectOutput : GenericOutput
    {
        private long _contentLength = -1;

        public string ETag { internal set; get; }

        public DateTime? LastModified { internal set; get; }

        public bool DeleteMarker { internal set; get; }

        public string SSECAlgorithm { internal set; get; }

        public string SSECKeyMD5 { internal set; get; }

        public string VersionID { internal set; get; }

        public string WebsiteRedirectLocation { internal set; get; }

        public string ObjectType { internal set; get; }

        public ulong HashCrc64ecma { internal set; get; }

        public StorageClassType? StorageClass { internal set; get; }

        public IDictionary<string, string> Meta { internal set; get; }

        public long ContentLength
        {
            internal set
            {
                if (value >= 0)
                {
                    _contentLength = value;
                }
            }
            get { return _contentLength; }
        }

        public string CacheControl { internal set; get; }

        public string ContentDisposition { internal set; get; }

        public string ContentEncoding { internal set; get; }

        public string ContentLanguage { internal set; get; }

        public string ContentType { internal set; get; }

        public DateTime? Expires { internal set; get; }

        internal override void Parse(HttpRequest request, HttpResponse response)
        {
            string temp;
            response.Header.TryGetValue(Constants.HeaderETag, out temp);
            ETag = temp;

            response.Header.TryGetValue(Constants.HeaderLastModified, out temp);
            LastModified = Utils.ParseDateTime(temp, Constants.Rfc1123DateFormat);

            response.Header.TryGetValue(Constants.HeaderDeleteMarker, out temp);
            DeleteMarker = temp == Constants.True;

            response.Header.TryGetValue(Constants.HeaderSSECAlgorithm, out temp);
            SSECAlgorithm = temp;

            response.Header.TryGetValue(Constants.HeaderSSECKeyMD5, out temp);
            SSECKeyMD5 = temp;

            response.Header.TryGetValue(Constants.HeaderVersionID, out temp);
            VersionID = temp;

            response.Header.TryGetValue(Constants.HeaderWebsiteRedirectLocation, out temp);
            WebsiteRedirectLocation = temp;

            response.Header.TryGetValue(Constants.HeaderObjectType, out temp);
            ObjectType = temp;

            response.Header.TryGetValue(Constants.HeaderHashCrc64ecma, out temp);
            if (!string.IsNullOrEmpty(temp))
            {
                HashCrc64ecma = Convert.ToUInt64(temp);
            }

            response.Header.TryGetValue(Constants.HeaderStorageClass, out temp);
            StorageClass = Enums.ParseEnum<StorageClassType>(temp) as StorageClassType?;

            Meta = Utils.ParseMeta(response.Header);

            response.Header.TryGetValue(Constants.HeaderContentLength, out temp);
            if (!string.IsNullOrEmpty(temp))
            {
                ContentLength = Convert.ToInt64(temp);
            }

            response.Header.TryGetValue(Constants.HeaderCacheControl, out temp);
            CacheControl = temp;

            response.Header.TryGetValue(Constants.HeaderContentDisposition, out temp);
            ContentDisposition = Utils.UrlDecode(temp);

            response.Header.TryGetValue(Constants.HeaderContentEncoding, out temp);
            ContentEncoding = temp;

            response.Header.TryGetValue(Constants.HeaderContentLanguage, out temp);
            ContentLanguage = temp;

            response.Header.TryGetValue(Constants.HeaderContentType, out temp);
            ContentType = temp;

            response.Header.TryGetValue(Constants.HeaderExpires, out temp);
            Expires = Utils.ParseDateTime(temp, Constants.Rfc1123DateFormat);
        }
    }
}