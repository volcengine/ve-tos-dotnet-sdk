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
using TOS.Error;

namespace TOS.Model
{
    public class PutObjectSetLifecycleInput : GenericBucketInput
    {
        public string ObjectSetName { set; get; }

        public LifecycleRule[] Rules { set; get; }

        internal sealed override string GetOperation()
        {
            return "PutObjectSetLifecycle";
        }

        internal sealed override HttpRequest Trans()
        {
            byte[] data = Encoding.UTF8.GetBytes(LifecycleJsonCodec.ToJson(Rules).ToString());

            HttpRequest request = base.Trans();
            request.Method = HttpMethodType.HttpMethodPut;
            ObjectSetLifecycleRequest.AddQuery(request, ObjectSetName);
            request.Header[Constants.HeaderContentMD5] = Utils.Base64Md5(data);
            request.Body = Utils.PrepareStream(request.Header, data);
            return request;
        }
    }

    public class PutObjectSetLifecycleOutput : GenericOutput
    {
    }

    public class GetObjectSetLifecycleInput : GenericBucketInput
    {
        public string ObjectSetName { set; get; }

        internal sealed override string GetOperation()
        {
            return "GetObjectSetLifecycle";
        }

        internal sealed override HttpRequest Trans()
        {
            HttpRequest request = base.Trans();
            request.Method = HttpMethodType.HttpMethodGet;
            ObjectSetLifecycleRequest.AddQuery(request, ObjectSetName);
            return request;
        }
    }

    public class GetObjectSetLifecycleOutput : GenericOutput
    {
        public LifecycleRule[] Rules { internal set; get; }

        internal sealed override void Parse(HttpRequest request, HttpResponse response)
        {
            Rules = LifecycleJsonCodec.ParseRules(Utils.ParseJson(response.Body));
        }
    }

    public class DeleteObjectSetLifecycleInput : GenericBucketInput
    {
        public string ObjectSetName { set; get; }

        internal sealed override string GetOperation()
        {
            return "DeleteObjectSetLifecycle";
        }

        internal sealed override HttpRequest Trans()
        {
            HttpRequest request = base.Trans();
            request.Method = HttpMethodType.HttpMethodDelete;
            ObjectSetLifecycleRequest.AddQuery(request, ObjectSetName);
            return request;
        }
    }

    public class DeleteObjectSetLifecycleOutput : GenericOutput
    {
    }

    internal static class ObjectSetLifecycleRequest
    {
        internal static void AddQuery(HttpRequest request, string objectSetName)
        {
            if (string.IsNullOrEmpty(objectSetName))
            {
                throw new TosClientException("empty object set name is set for object set lifecycle");
            }

            request.Query[Constants.QueryObjectSetLifecycle] = string.Empty;
            request.Query[Constants.QueryObjectSetName] = objectSetName;
        }
    }
}
