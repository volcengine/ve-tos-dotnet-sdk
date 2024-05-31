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
using TOS.Common;

namespace TOS.Model
{
    public class GetObjectTaggingInput : GenericObjectInput
    {
        public string VersionID { set; get; }
        
        internal sealed override HttpRequest Trans()
        {
            HttpRequest request = base.Trans();
            request.Method = HttpMethodType.HttpMethodGet;

            if (!string.IsNullOrEmpty(VersionID))
            {
                request.Query[Constants.QueryVersionID] = VersionID;
            }
            
            request.Query[Constants.QueryTagging] = string.Empty;
            return request;
        }
    }
    
    public class GetObjectTaggingOutput : GenericOutput
    {
        public string VersionID { internal set; get; }
        
        public TagSet TagSet { internal set; get; }

        internal sealed override void Parse(HttpRequest request, HttpResponse response)
        {
            string tmp;
            response.Header.TryGetValue(Constants.HeaderVersionID, out tmp);
            VersionID = tmp;
            
            JObject json = Utils.ParseJson(response.Body);
            JArray tagSetArray = json["TagSet"]?["Tags"] as JArray;
            if (tagSetArray != null)
            {
                TagSet = new TagSet()
                {
                    Tags = new Tag[tagSetArray.Count],
                };
                
                for (int i = 0; i < tagSetArray.Count; i++)
                {
                    TagSet.Tags[i] = new Tag()
                    {
                        Key = tagSetArray[i]["Key"]?.Value<string>(),
                        Value = tagSetArray[i]["Value"]?.Value<string>()
                    };
                }
            }
            else
            {
                TagSet = new TagSet()
                {
                    Tags = new Tag[0],
                };
            }
        }
    }
}