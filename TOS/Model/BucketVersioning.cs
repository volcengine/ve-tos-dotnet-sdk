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

using System.Text;
using Newtonsoft.Json.Linq;
using TOS.Common;
using TOS.Error;

namespace TOS.Model
{
    public class PutBucketVersioningInput : GenericBucketInput
    {
        public VersioningStatusType? Status { set; get; }

        internal sealed override string GetOperation()
        {
            return "PutBucketVersioning";
        }

        internal sealed override HttpRequest Trans()
        {
            if (Status != VersioningStatusType.VersioningStatusEnabled &&
                Status != VersioningStatusType.VersioningStatusSuspended)
            {
                throw new TosClientException("invalid status is set for put bucket versioning");
            }

            JObject json = new JObject();
            json["Status"] = Enums.TransEnum(Status);
            byte[] data = Encoding.UTF8.GetBytes(json.ToString());
            HttpRequest request = base.Trans();
            request.Method = HttpMethodType.HttpMethodPut;
            request.Query[Constants.QueryVersioning] = string.Empty;
            request.Header[Constants.HeaderContentMD5] = Utils.Base64Md5(data);
            request.Body = Utils.PrepareStream(request.Header, data);
            return request;
        }
    }

    public class PutBucketVersioningOutput : GenericOutput
    {
    }

    public class GetBucketVersioningInput : GenericBucketInput
    {
        internal sealed override string GetOperation()
        {
            return "GetBucketVersioning";
        }

        internal sealed override HttpRequest Trans()
        {
            HttpRequest request = base.Trans();
            request.Method = HttpMethodType.HttpMethodGet;
            request.Query[Constants.QueryVersioning] = string.Empty;
            return request;
        }
    }

    public class GetBucketVersioningOutput : GenericOutput
    {
        public VersioningStatusType? Status { internal set; get; }

        internal sealed override void Parse(HttpRequest request, HttpResponse response)
        {
            JObject json = Utils.ParseJson(response.Body);
            Status = Enums.ParseEnum<VersioningStatusType>(json["Status"]?.Value<string>()) as VersioningStatusType?;
        }
    }
}
