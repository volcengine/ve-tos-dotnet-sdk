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
    public class PutObjectTaggingInput : PutObjectBasicInput
    {
        public string VersionID { set; get; }
        
        public TagSet TagSet { set; get; }
        
        internal sealed override HttpRequest Trans()
        {
            HttpRequest request = base.Trans();
            request.Method = HttpMethodType.HttpMethodPut;

            if (TagSet == null || TagSet.Tags == null || TagSet.Tags.Length == 0)
            {
                throw new TosClientException("empty tag set is set for put object tagging");
            }

            if (!string.IsNullOrEmpty(VersionID))
            {
                request.Query[Constants.QueryVersionID] = VersionID;
            }

            JArray tags = new JArray();
            foreach (Tag tag in TagSet.Tags)
            {
                if (tag == null)
                {
                    continue;
                }

                if (tag.Key.Length == 0 || tag.Value.Length == 0)
                {
                    throw new TosClientException("empty tag value or key is set for put object tagging");
                }
                
                JObject tmp = new JObject();
                tmp["Key"] = tag.Key;
                tmp["Value"] = tag.Value;
                tags.Add(tmp);
            }
            
            JObject tagSet = new JObject();
            tagSet["Tags"] = tags;
            JObject json = new JObject();
            json["TagSet"] = tagSet;
            
            byte[] data = Encoding.UTF8.GetBytes(json.ToString());
            request.Body = Utils.PrepareStream(request.Header, data);
            request.Query[Constants.QueryTagging] = string.Empty;
            return request;
        }
    }

    public class PutObjectTaggingOutput : GenericOutput
    {
        public string VersionID { internal set; get; }

        internal sealed override void Parse(HttpRequest request, HttpResponse response)
        {
            string tmp;
            response.Header.TryGetValue(Constants.HeaderVersionID, out tmp);
            VersionID = tmp;
        }
    }
}