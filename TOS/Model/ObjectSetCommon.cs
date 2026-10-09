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

using Newtonsoft.Json.Linq;
using TOS.Error;

namespace TOS.Model
{
    public class ObjectSet
    {
        public string ObjectSetName { internal set; get; }
        public TagSet TagSet { internal set; get; }
    }

    internal static class ObjectSetJsonCodec
    {
        internal static void ValidateName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new TosClientException("empty object set name");
            }
        }

        internal static JObject ToJson(TagSet tagSet)
        {
            JArray tags = new JArray();
            if (tagSet.Tags != null)
            {
                foreach (Tag tag in tagSet.Tags)
                {
                    if (tag == null || string.IsNullOrEmpty(tag.Key))
                    {
                        throw new TosClientException("invalid object set tag");
                    }

                    JObject jsonTag = new JObject();
                    jsonTag["Key"] = tag.Key;
                    jsonTag["Value"] = tag.Value ?? string.Empty;
                    tags.Add(jsonTag);
                }
            }

            JObject json = new JObject();
            json["Tags"] = tags;
            return json;
        }

        internal static ObjectSet Parse(JObject json)
        {
            JObject tagSetJson = json?["TagSet"] as JObject;
            JArray tags = tagSetJson?["Tags"] as JArray;
            TagSet tagSet = new TagSet { Tags = new Tag[tags == null ? 0 : tags.Count] };
            for (int i = 0; i < tagSet.Tags.Length; i++)
            {
                JObject tag = tags[i] as JObject;
                tagSet.Tags[i] = new Tag
                {
                    Key = tag?["Key"]?.Value<string>(),
                    Value = tag?["Value"]?.Value<string>()
                };
            }

            return new ObjectSet { ObjectSetName = json?["ObjectSetName"]?.Value<string>(), TagSet = tagSet };
        }
    }
}
