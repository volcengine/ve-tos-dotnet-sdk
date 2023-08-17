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
    public class SetObjectMetaInput : GenericObjectInput, IHttpBasicHeader
    {
        internal sealed override string GetOperation()
        {
            return "SetObjectMeta";
        }

        public string VersionID { set; get; }

        long IHttpBasicHeader.ContentLength { get; }

        public string CacheControl { get; set; }
        public string ContentDisposition { get; set; }
        public string ContentEncoding { get; set; }
        public string ContentLanguage { get; set; }
        public string ContentType { get; set; }
        public DateTime? Expires { get; set; }

        public IDictionary<string, string> Meta { set; get; }

        internal sealed override HttpRequest Trans()
        {
            HttpRequest request = base.Trans();
            request.Method = HttpMethodType.HttpMethodPost;

            Utils.SetHttpBasicHeader(request.Header, this);
            Utils.SetMetaHeader(request.Header, Meta);

            if (!string.IsNullOrEmpty(VersionID))
            {
                request.Query[Constants.QueryVersionID] = VersionID;
            }

            request.Query["metadata"] = string.Empty;

            return request;
        }
    }

    public class SetObjectMetaOutput : GenericOutput
    {
    }
}