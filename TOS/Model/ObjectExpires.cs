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

using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json.Linq;
using TOS.Common;
using TOS.Error;

namespace TOS.Model
{
    public class SetObjectExpiresInput : GenericObjectInput
    {
        public string VersionID { set; get; }

        public long? ObjectExpires { set; get; }

        internal sealed override string GetOperation()
        {
            return "SetObjectExpires";
        }

        internal sealed override HttpRequest Trans()
        {
            if (!ObjectExpires.HasValue)
            {
                throw new TosClientException("object expires is required for set object expires");
            }

            ObjectExpirationUtils.Validate(ObjectExpires.Value);
            JObject json = new JObject();
            json["ObjectExpires"] = ObjectExpires.Value;
            byte[] data = Encoding.UTF8.GetBytes(json.ToString());
            HttpRequest request = base.Trans();
            request.Method = HttpMethodType.HttpMethodPost;
            request.Query[Constants.QueryObjectExpires] = string.Empty;
            if (!string.IsNullOrEmpty(VersionID))
            {
                request.Query[Constants.QueryVersionID] = VersionID;
            }

            request.Header[Constants.HeaderContentMD5] = Utils.Base64Md5(data);
            request.Body = Utils.PrepareStream(request.Header, data);
            return request;
        }
    }

    public class SetObjectExpiresOutput : GenericOutput
    {
    }

    internal static class ObjectExpirationUtils
    {
        internal static void SetHeader(IDictionary<string, string> header, long? objectExpires)
        {
            if (objectExpires.HasValue)
            {
                Validate(objectExpires.Value);
                header[Constants.HeaderObjectExpires] = objectExpires.Value.ToString(Constants.DefaultCultureInfo);
            }
        }

        internal static void Validate(long objectExpires)
        {
            if (objectExpires < 0)
            {
                throw new TosClientException("object expires must not be negative");
            }
        }
    }
}
