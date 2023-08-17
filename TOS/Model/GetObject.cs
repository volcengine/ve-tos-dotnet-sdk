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
using TOS.Error;

namespace TOS.Model
{
    public class GetObjectInput : GenericObjectInput, IIfConditionHeader, ISseHeader
    {
        private long _rangeStart = -1;
        private long _rangeEnd = -1;

        internal sealed override string GetOperation()
        {
            return "GetObject";
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

        public string ResponseCacheControl { set; get; }

        public string ResponseContentDisposition { set; get; }

        public string ResponseContentEncoding { set; get; }

        public string ResponseContentLanguage { set; get; }

        public string ResponseContentType { set; get; }

        public DateTime? ResponseExpires { set; get; }

        public long RangeStart
        {
            set
            {
                if (value >= 0)
                {
                    this._rangeStart = value;
                }
            }

            get { return this._rangeStart; }
        }

        public long RangeEnd
        {
            set
            {
                if (value >= 0)
                {
                    this._rangeEnd = value;
                }
            }

            get { return this._rangeEnd; }
        }

        public string Range { set; get; }

        internal override HttpRequest Trans()
        {
            HttpRequest request = base.Trans();
            request.Method = HttpMethodType.HttpMethodGet;

            Utils.SetIfConditionHeader(request.Header, this);
            Utils.SetSseHeader(request.Header, this);

            if (!string.IsNullOrEmpty(Range))
            {
                if (!Range.StartsWith("bytes="))
                {
                    throw new TosClientException("invalid range format");
                }

                request.Header[Constants.HeaderRange] = Range;
            }
            else if (RangeStart >= 0 && RangeEnd >= 0 && RangeStart <= RangeEnd)
            {
                request.Header[Constants.HeaderRange] = "bytes=" + RangeStart + "-" + RangeEnd;
            }

            if (!string.IsNullOrEmpty(VersionID))
            {
                request.Query[Constants.QueryVersionID] = VersionID;
            }

            if (!string.IsNullOrEmpty(ResponseCacheControl))
            {
                request.Query[Constants.QueryResponseCacheControl] = ResponseCacheControl;
            }

            if (!string.IsNullOrEmpty(ResponseContentDisposition))
            {
                request.Query[Constants.QueryResponseContentDisposition] = ResponseContentDisposition;
            }

            if (!string.IsNullOrEmpty(ResponseContentEncoding))
            {
                request.Query[Constants.QueryResponseContentEncoding] = ResponseContentEncoding;
            }

            if (!string.IsNullOrEmpty(ResponseContentLanguage))
            {
                request.Query[Constants.QueryResponseContentLanguage] = ResponseContentLanguage;
            }

            if (!string.IsNullOrEmpty(ResponseContentType))
            {
                request.Query[Constants.QueryResponseContentType] = ResponseContentType;
            }

            if (ResponseExpires.HasValue)
            {
                request.Query[Constants.QueryResponseExpires] = ResponseExpires?.ToUniversalTime()
                    .ToString(Constants.Rfc1123DateFormat, Constants.DefaultCultureInfo);
            }

            return request;
        }
    }

    public class GetObjectBasicOutput : HeadObjectOutput, IHttpBasicHeader, IMiscHeader
    {
        public string ContentRange { internal set; get; }

        internal override void Parse(HttpRequest request, HttpResponse response)
        {
            base.Parse(request, response);
            string temp;
            response.Header.TryGetValue(Constants.HeaderContentRange, out temp);
            ContentRange = temp;
        }
    }

    public class GetObjectOutput : GetObjectBasicOutput, IDisposable
    {
        private bool _disposed;
        private Stream _content;

        public Stream Content
        {
            internal set { this._content = value; }
            get { return _content; }
        }

        public Stream Detach()
        {
            Stream temp = _content;
            _content = null;
            return temp;
        }

        public void Dispose()
        {
            try
            {
                if (_disposed)
                {
                    return;
                }

                if (this._content != null)
                {
                    this._content.Close();
                    this._content = null;
                }

                _disposed = true;
            }
            finally
            {
                GC.SuppressFinalize(this);
            }
        }

        internal sealed override void Parse(HttpRequest request, HttpResponse response)
        {
            response.AutoClose = false;
            Content = response.Body;
            base.Parse(request, response);
        }
    }
}