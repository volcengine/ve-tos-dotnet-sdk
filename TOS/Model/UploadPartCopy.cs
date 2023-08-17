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
using Newtonsoft.Json.Linq;
using TOS.Common;
using TOS.Error;

namespace TOS.Model
{
    public class UploadPartCopyInput : GenericObjectInput, ISseHeader, ICopySourceHeader, ICopySourceSSeHeader,
        ICopySourceIfConditionHeader, IMultipartUploadQuery
    {
        private long _copySourceRangeStart = -1;
        private long _copySourceRangeEnd = -1;

        internal sealed override string GetOperation()
        {
            return "UploadPartCopy";
        }

        public string UploadID { set; get; }

        public int PartNumber { set; get; }

        public string SrcBucket { set; get; }

        public string SrcKey { set; get; }

        public string SrcVersionID { set; get; }

        public long CopySourceRangeStart
        {
            set
            {
                if (value >= 0)
                {
                    this._copySourceRangeStart = value;
                }
            }

            get { return this._copySourceRangeStart; }
        }

        public long CopySourceRangeEnd
        {
            set
            {
                if (value >= 0)
                {
                    this._copySourceRangeEnd = value;
                }
            }

            get { return this._copySourceRangeEnd; }
        }

        public string CopySourceRange { set; get; }

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
        string ISseHeader.ServerSideEncryption { get; }


        internal sealed override HttpRequest Trans()
        {
            HttpRequest request = base.Trans();
            request.Method = HttpMethodType.HttpMethodPut;
            if (!string.IsNullOrEmpty(CopySourceRange))
            {
                if (!CopySourceRange.StartsWith("bytes="))
                {
                    throw new TosClientException("invalid copy source range format");
                }

                request.Header[Constants.HeaderCopySourceRange] = CopySourceRange;
            }
            else if (CopySourceRangeStart >= 0 && CopySourceRangeEnd >= 0 && CopySourceRangeStart <= CopySourceRangeEnd)
            {
                request.Header[Constants.HeaderCopySourceRange] =
                    "bytes=" + CopySourceRangeStart + "-" + CopySourceRangeEnd;
            }

            Utils.SetCopySourceHeader(request.Header, this);
            Utils.SetCopySourceIfConditionHeader(request.Header, this);
            Utils.SetSseHeader(request.Header, this);
            Utils.SetCopySourceSseHeader(request.Header, this);
            Utils.SetMultipartUploadQuery(request.Query, this);

            return request;
        }
    }

    public class UploadPartCopyOutput : GenericOutput
    {
        public int PartNumber { internal set; get; }

        public string ETag { internal set; get; }

        public DateTime? LastModified { internal set; get; }

        public string SSECAlgorithm { internal set; get; }

        public string SSECKeyMD5 { internal set; get; }

        internal sealed override void Parse(HttpRequest request, HttpResponse response)
        {
            JObject json = Utils.ParseJson(response.Body);
            if (json.ContainsKey("ETag"))
            {
                PartNumber = Convert.ToInt32(request.Query[Constants.QueryPartNumber]);
                ETag = json["ETag"]?.Value<string>();
                LastModified = Utils.ParseJTokenDate(json["LastModified"], Constants.Iso8601DateFormat);
                string temp;
                response.Header.TryGetValue(Constants.HeaderSSECAlgorithm, out temp);
                SSECAlgorithm = temp;

                response.Header.TryGetValue(Constants.HeaderSSECKeyMD5, out temp);
                SSECKeyMD5 = temp;
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