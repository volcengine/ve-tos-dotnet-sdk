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

using System;
using Newtonsoft.Json.Linq;
using TOS.Common;

namespace TOS.Model
{
    public class ListPartsInput : GenericObjectInput
    {
        private int _maxParts = -1;

        internal sealed override string GetOperation()
        {
            return "ListParts";
        }

        public string UploadID { set; get; }

        public int PartNumberMarker { set; get; }

        public int MaxParts
        {
            get { return _maxParts; }

            set
            {
                if (value >= 0)
                {
                    _maxParts = value;
                }
            }
        }

        internal sealed override HttpRequest Trans()
        {
            HttpRequest request = base.Trans();
            request.Method = HttpMethodType.HttpMethodGet;

            Utils.SetUploadId(request.Query, UploadID);
            if (PartNumberMarker >= 0)
            {
                request.Query[Constants.QueryNextPartNumberMarker] = Convert.ToString(PartNumberMarker);
            }

            if (MaxParts >= 0)
            {
                request.Query[Constants.QueryMaxParts] = Convert.ToString(MaxParts);
            }

            return request;
        }
    }

    public class ListPartsOutput : GenericOutput
    {
        public string Bucket { internal set; get; }

        public string Key { internal set; get; }

        public string UploadID { internal set; get; }

        public int PartNumberMarker { internal set; get; }

        public int MaxParts { internal set; get; }

        public bool IsTruncated { internal set; get; }

        public int NextPartNumberMarker { internal set; get; }

        public Owner Owner { internal set; get; }

        public StorageClassType? StorageClass { internal set; get; }

        public UploadedPart[] Parts { internal set; get; }

        internal override void Parse(HttpRequest request, HttpResponse response)
        {
            JObject json = Utils.ParseJson(response.Body);
            JArray parts = json["Parts"] as JArray;
            if (parts != null)
            {
                Parts = new UploadedPart[parts.Count];
                for (int i = 0; i < parts.Count; i++)
                {
                    Parts[i] = new UploadedPart().Parse(parts[i]);
                }
            }
            else
            {
                Parts = new UploadedPart[0];
            }

            Bucket = json["Bucket"]?.Value<string>();
            Key = json["Key"]?.Value<string>();
            UploadID = json["UploadId"]?.Value<string>();
            PartNumberMarker = json["PartNumberMarker"]?.Value<int>() ?? 0;
            MaxParts = json["MaxParts"]?.Value<int>() ?? 0;
            IsTruncated = json["IsTruncated"]?.Value<bool>() ?? false;
            NextPartNumberMarker = json["NextPartNumberMarker"]?.Value<int>() ?? 0;
            StorageClass =
                Enums.ParseEnum<StorageClassType>(json["StorageClass"]?.Value<string>()) as
                    StorageClassType?;

            Owner = new Owner()
            {
                ID = json["Owner"]?["ID"]?.Value<string>()
            };
        }
    }
}