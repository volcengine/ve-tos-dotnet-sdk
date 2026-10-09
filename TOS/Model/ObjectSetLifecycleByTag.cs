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
    public class ObjectSetTagLifecycleRule
    {
        public Tag Tag { set; get; }

        public LifecycleRule[] Rules { set; get; }
    }

    public class PutObjectSetLifecycleByTagInput : GenericBucketInput
    {
        public ObjectSetTagLifecycleRule[] ObjectSetTagRules { set; get; }

        internal sealed override string GetOperation()
        {
            return "PutObjectSetLifecycleByTag";
        }

        internal sealed override HttpRequest Trans()
        {
            byte[] data = Encoding.UTF8.GetBytes(
                ObjectSetLifecycleByTagJsonCodec.ToJson(ObjectSetTagRules).ToString());

            HttpRequest request = base.Trans();
            request.Method = HttpMethodType.HttpMethodPut;
            request.Query[Constants.QueryObjectSetLifecycleByTag] = string.Empty;
            request.Header[Constants.HeaderContentMD5] = Utils.Base64Md5(data);
            request.Body = Utils.PrepareStream(request.Header, data);
            return request;
        }
    }

    public class PutObjectSetLifecycleByTagOutput : GenericOutput
    {
    }

    public class GetObjectSetLifecycleByTagInput : GenericBucketInput
    {
        internal sealed override string GetOperation()
        {
            return "GetObjectSetLifecycleByTag";
        }

        internal sealed override HttpRequest Trans()
        {
            HttpRequest request = base.Trans();
            request.Method = HttpMethodType.HttpMethodGet;
            request.Query[Constants.QueryObjectSetLifecycleByTag] = string.Empty;
            return request;
        }
    }

    public class GetObjectSetLifecycleByTagOutput : GenericOutput
    {
        public ObjectSetTagLifecycleRule[] ObjectSetTagRules { internal set; get; }

        internal sealed override void Parse(HttpRequest request, HttpResponse response)
        {
            ObjectSetTagRules = ObjectSetLifecycleByTagJsonCodec.Parse(Utils.ParseJson(response.Body));
        }
    }

    public class DeleteObjectSetLifecycleByTagInput : GenericBucketInput
    {
        internal sealed override string GetOperation()
        {
            return "DeleteObjectSetLifecycleByTag";
        }

        internal sealed override HttpRequest Trans()
        {
            HttpRequest request = base.Trans();
            request.Method = HttpMethodType.HttpMethodDelete;
            request.Query[Constants.QueryObjectSetLifecycleByTag] = string.Empty;
            return request;
        }
    }

    public class DeleteObjectSetLifecycleByTagOutput : GenericOutput
    {
    }

    internal static class ObjectSetLifecycleByTagJsonCodec
    {
        internal static JObject ToJson(ObjectSetTagLifecycleRule[] tagRules)
        {
            Validate(tagRules);

            JArray jsonRules = new JArray();
            foreach (ObjectSetTagLifecycleRule tagRule in tagRules)
            {
                JObject jsonRule = new JObject();
                jsonRule["Tag"] = ToJson(tagRule.Tag);
                jsonRule["Rules"] = LifecycleJsonCodec.ToJson(tagRule.Rules)["Rules"];
                jsonRules.Add(jsonRule);
            }

            JObject json = new JObject();
            json["ObjectSetTagRules"] = jsonRules;
            return json;
        }

        internal static ObjectSetTagLifecycleRule[] Parse(JObject json)
        {
            JArray jsonRules = json["ObjectSetTagRules"] as JArray;
            if (jsonRules == null)
            {
                return new ObjectSetTagLifecycleRule[0];
            }

            ObjectSetTagLifecycleRule[] result = new ObjectSetTagLifecycleRule[jsonRules.Count];
            for (int i = 0; i < jsonRules.Count; i++)
            {
                JObject jsonRule = jsonRules[i] as JObject;
                if (jsonRule == null)
                {
                    result[i] = new ObjectSetTagLifecycleRule
                    {
                        Rules = new LifecycleRule[0]
                    };
                    continue;
                }

                result[i] = new ObjectSetTagLifecycleRule
                {
                    Tag = ParseTag(jsonRule["Tag"] as JObject),
                    Rules = LifecycleJsonCodec.ParseRules(jsonRule)
                };
            }

            return result;
        }

        private static JObject ToJson(Tag tag)
        {
            JObject json = new JObject();
            json["Key"] = tag.Key;
            json["Value"] = tag.Value;
            return json;
        }

        private static Tag ParseTag(JObject json)
        {
            if (json == null)
            {
                return null;
            }

            return new Tag
            {
                Key = json["Key"]?.Value<string>(),
                Value = json["Value"]?.Value<string>()
            };
        }

        private static void Validate(ObjectSetTagLifecycleRule[] tagRules)
        {
            if (tagRules == null || tagRules.Length == 0)
            {
                throw new TosClientException("empty object set tag rules are set for put object set lifecycle by tag");
            }

            foreach (ObjectSetTagLifecycleRule tagRule in tagRules)
            {
                if (tagRule == null)
                {
                    throw new TosClientException("null object set tag rule is set for put object set lifecycle by tag");
                }

                if (tagRule.Tag == null || string.IsNullOrEmpty(tagRule.Tag.Key) ||
                    string.IsNullOrEmpty(tagRule.Tag.Value))
                {
                    throw new TosClientException("invalid tag is set for put object set lifecycle by tag");
                }
            }
        }
    }
}
