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
using TOS.Common;

namespace TOS.Model
{
    public class PutBucketLifecycleInput : GenericBucketInput
    {
        public LifecycleRule[] Rules { set; get; }

        public bool AllowSameActionOverlap { set; get; }

        internal sealed override string GetOperation()
        {
            return "PutBucketLifecycle";
        }

        internal sealed override HttpRequest Trans()
        {
            byte[] data = Encoding.UTF8.GetBytes(LifecycleJsonCodec.ToJson(Rules).ToString());

            HttpRequest request = base.Trans();
            request.Method = HttpMethodType.HttpMethodPut;
            request.Query[Constants.QueryLifecycle] = string.Empty;
            request.Header[Constants.HeaderAllowSameActionOverlap] =
                AllowSameActionOverlap ? Constants.True : Constants.False;
            request.Header[Constants.HeaderContentMD5] = Utils.Base64Md5(data);
            request.Body = Utils.PrepareStream(request.Header, data);
            return request;
        }
    }

    public class PutBucketLifecycleOutput : GenericOutput
    {
    }

    public class GetBucketLifecycleInput : GenericBucketInput
    {
        internal sealed override string GetOperation()
        {
            return "GetBucketLifecycle";
        }

        internal sealed override HttpRequest Trans()
        {
            HttpRequest request = base.Trans();
            request.Method = HttpMethodType.HttpMethodGet;
            request.Query[Constants.QueryLifecycle] = string.Empty;
            return request;
        }
    }

    public class GetBucketLifecycleOutput : GenericOutput
    {
        public LifecycleRule[] Rules { internal set; get; }

        public bool AllowSameActionOverlap { internal set; get; }

        internal sealed override void Parse(HttpRequest request, HttpResponse response)
        {
            Rules = LifecycleJsonCodec.ParseRules(Utils.ParseJson(response.Body));

            string value;
            bool allowSameActionOverlap;
            response.Header.TryGetValue(Constants.HeaderAllowSameActionOverlap, out value);
            AllowSameActionOverlap = bool.TryParse(value, out allowSameActionOverlap) && allowSameActionOverlap;
        }
    }

    public class DeleteBucketLifecycleInput : GenericBucketInput
    {
        internal sealed override string GetOperation()
        {
            return "DeleteBucketLifecycle";
        }

        internal sealed override HttpRequest Trans()
        {
            HttpRequest request = base.Trans();
            request.Method = HttpMethodType.HttpMethodDelete;
            request.Query[Constants.QueryLifecycle] = string.Empty;
            return request;
        }
    }

    public class DeleteBucketLifecycleOutput : GenericOutput
    {
    }
}
