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
    public class DeleteMultiObjectsInput : GenericBucketInput
    {
        internal sealed override string GetOperation()
        {
            return "DeleteMultiObjects";
        }

        public ObjectTobeDeleted[] Objects { set; get; }

        public bool Quiet { set; get; }

        internal sealed override HttpRequest Trans()
        {
            if (Objects == null || Objects.Length == 0)
            {
                throw new TosClientException("empty objects for delete");
            }

            JArray objects = new JArray();
            JObject temp;
            foreach (ObjectTobeDeleted obj in Objects)
            {
                if (obj == null)
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(obj.Key))
                {
                    temp = new JObject();
                    temp["Key"] = obj.Key;
                    if (!string.IsNullOrEmpty(obj.VersionID))
                    {
                        temp["VersionId"] = obj.VersionID;
                    }

                    objects.Add(temp);
                }
            }

            if (objects.Count == 0)
            {
                throw new TosClientException("empty objects for delete");
            }

            HttpRequest request = base.Trans();
            request.Method = HttpMethodType.HttpMethodPost;
            request.Query["delete"] = string.Empty;

            JObject json = new JObject();
            json["Quiet"] = Quiet;
            json["Objects"] = objects;
            byte[] data = Encoding.UTF8.GetBytes(json.ToString());
            request.Header[Constants.HeaderContentMD5] = Utils.Base64Md5(data);
            request.Body = Utils.PrepareStream(request.Header, data);

            return request;
        }
    }


    public class DeleteMultiObjectsOutput : GenericOutput
    {
        public Deleted[] Deleted { internal set; get; }
        public DeleteError[] Error { internal set; get; }

        internal sealed override void Parse(HttpRequest request, HttpResponse response)
        {
            JObject json = Utils.ParseJson(response.Body);
            JArray temp = json["Deleted"] as JArray;
            if (temp != null)
            {
                Deleted = new Deleted[temp.Count];
                for (int i = 0; i < temp.Count; i++)
                {
                    Deleted[i] = new Deleted
                    {
                        Key = temp[i]["Key"]?.Value<string>(),
                        VersionID = temp[i]["VersionId"]?.Value<string>(),
                        DeleteMarker = temp[i]["DeleteMarker"]?.Value<bool>() ?? false,
                        DeleteMarkerVersionID = temp[i]["DeleteMarkerVersionId"]?.Value<string>()
                    };
                }
            }
            else
            {
                Deleted = new Deleted[0];
            }

            temp = json["Error"] as JArray;
            if (temp != null)
            {
                Error = new DeleteError[temp.Count];
                for (int i = 0; i < temp.Count; i++)
                {
                    Error[i] = new DeleteError
                    {
                        Key = temp[i]["Key"]?.Value<string>(),
                        VersionID = temp[i]["VersionId"]?.Value<string>(),
                        Code = temp[i]["Code"]?.Value<string>(),
                        Message = temp[i]["Message"]?.Value<string>()
                    };
                }
            }
            else
            {
                Error = new DeleteError[0];
            }
        }
    }

    public class ObjectTobeDeleted
    {
        public string Key { set; get; }
        public string VersionID { set; get; }
    }

    public class Deleted
    {
        public string Key { internal set; get; }
        public string VersionID { internal set; get; }
        public bool DeleteMarker { internal set; get; }
        public string DeleteMarkerVersionID { internal set; get; }
    }

    public class DeleteError
    {
        public string Key { internal set; get; }
        public string VersionID { internal set; get; }
        public string Code { internal set; get; }
        public string Message { internal set; get; }
    }
}