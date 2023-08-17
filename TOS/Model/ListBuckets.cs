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
    public class ListBucketsInput : GenericInput
    {
        internal sealed override string GetOperation()
        {
            return "ListBuckets";
        }

        internal sealed override HttpRequest Trans()
        {
            HttpRequest request = new HttpRequest();
            request.Operation = this.GetOperation();
            request.Method = HttpMethodType.HttpMethodGet;
            return request;
        }
    }

    public class ListBucketsOutput : GenericOutput
    {
        public ListedBucket[] Buckets { internal set; get; }
        public Owner Owner { internal set; get; }

        internal sealed override void Parse(HttpRequest request, HttpResponse response)
        {
            JObject json = Utils.ParseJson(response.Body);
            JArray buckets = json["Buckets"] as JArray;
            if (buckets != null)
            {
                Buckets = new ListedBucket[buckets.Count];
                for (int i = 0; i < buckets.Count; i++)
                {
                    Buckets[i] = new ListedBucket
                    {
                        CreationDate = Utils.TransJTokenDate(buckets[i]["CreationDate"], Constants.Iso8601DateFormat),
                        Name = buckets[i]["Name"]?.Value<string>(),
                        Location = buckets[i]["Location"]?.Value<string>(),
                        ExtranetEndpoint = buckets[i]["ExtranetEndpoint"]?.Value<string>(),
                        IntranetEndpoint = buckets[i]["IntranetEndpoint"]?.Value<string>()
                    };
                }
            }
            else
            {
                Buckets = new ListedBucket[0];
            }

            Owner = new Owner
            {
                ID = json["Owner"]?["ID"]?.Value<string>()
            };
        }
    }

    public class ListedBucket
    {
        public string CreationDate { internal set; get; }

        public string Name { internal set; get; }

        public string Location { internal set; get; }

        public string ExtranetEndpoint { internal set; get; }

        public string IntranetEndpoint { internal set; get; }
    }
}