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
using System.IO;
using TOS.Common;

namespace TOS.Model
{
    public class UploadPartBasicInput : GenericObjectInput, ISseHeader, IMultipartUploadQuery
    {
        private int _partNumber = 1;

        internal sealed override string GetOperation()
        {
            return "UploadPart";
        }

        public string UploadID { set; get; }

        public int PartNumber
        {
            set { this._partNumber = value; }
            get { return _partNumber; }
        }

        public string ContentMD5 { set; get; }

        public string SSECAlgorithm { set; get; }

        public string SSECKey { set; get; }

        public string SSECKeyMD5 { set; get; }

        string ISseHeader.ServerSideEncryption { get; }

        internal override HttpRequest Trans()
        {
            HttpRequest request = base.Trans();
            request.Method = HttpMethodType.HttpMethodPut;

            if (!string.IsNullOrEmpty(ContentMD5))
            {
                request.Header[Constants.HeaderContentMD5] = ContentMD5;
            }

            Utils.SetSseHeader(request.Header, this);
            Utils.SetMultipartUploadQuery(request.Query, this);

            return request;
        }
    }

    public class UploadPartInput : UploadPartBasicInput
    {
        private long _contentLength = -1;

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

        public Stream Content { set; get; }

        internal sealed override HttpRequest Trans()
        {
            HttpRequest request = base.Trans();
            request.Body = Content;
            if (ContentLength >= 0)
            {
                request.Header[Constants.HeaderContentLength] = Convert.ToString(ContentLength);
            }
            else
            {
                Utils.TrySetContentLength(request.Header, request.Body);
            }

            return request;
        }
    }

    public class UploadPartOutput : GenericOutput
    {
        public int PartNumber { internal set; get; }

        public string ETag { internal set; get; }

        public string SSECAlgorithm { internal set; get; }

        public string SSECKeyMD5 { internal set; get; }

        public ulong HashCrc64ecma { internal set; get; }

        internal override void Parse(HttpRequest request, HttpResponse response)
        {
            PartNumber = Convert.ToInt32(request.Query[Constants.QueryPartNumber]);
            string temp;
            response.Header.TryGetValue(Constants.HeaderETag, out temp);
            ETag = temp;

            response.Header.TryGetValue(Constants.HeaderSSECAlgorithm, out temp);
            SSECAlgorithm = temp;

            response.Header.TryGetValue(Constants.HeaderSSECKeyMD5, out temp);
            SSECKeyMD5 = temp;

            response.Header.TryGetValue(Constants.HeaderHashCrc64ecma, out temp);
            if (!string.IsNullOrEmpty(temp))
            {
                HashCrc64ecma = Convert.ToUInt64(temp);
            }
        }
    }
}