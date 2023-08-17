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
    public class ListObjectsInput : GenericBucketInput, IListCommonQuery
    {
        private int _maxKeys = -1;

        internal sealed override string GetOperation()
        {
            return "ListObjects";
        }

        public string Prefix { get; set; }

        public string Delimiter { get; set; }

        public string Marker { get; set; }

        public int MaxKeys
        {
            get { return _maxKeys; }

            set
            {
                if (value >= 0)
                {
                    _maxKeys = value;
                }
            }
        }

        public string EncodingType { get; set; }

        internal sealed override HttpRequest Trans()
        {
            HttpRequest request = base.Trans();
            request.Method = HttpMethodType.HttpMethodGet;

            Utils.SetListCommonQuery(request.Query, this);
            if (!string.IsNullOrEmpty(Marker))
            {
                request.Query[Constants.QueryMarker] = Marker;
            }

            if (MaxKeys >= 0)
            {
                request.Query[Constants.QueryMaxKeys] = Convert.ToString(MaxKeys);
            }

            return request;
        }
    }

    public class ListObjectsOutput : GenericOutput
    {
        public string Name { internal set; get; }

        public string Prefix { internal set; get; }


        public string Marker { internal set; get; }


        public int MaxKeys { internal set; get; }


        public string Delimiter { internal set; get; }


        public bool IsTruncated { internal set; get; }


        public string EncodingType { internal set; get; }


        public string NextMarker { internal set; get; }

        public ListedCommonPrefix[] CommonPrefixes { internal set; get; }

        public ListedObject[] Contents { internal set; get; }

        internal sealed override void Parse(HttpRequest request, HttpResponse response)
        {
            JObject json = Utils.ParseJson(response.Body);
            CommonPrefixes = Utils.ParseCommonPrefixes(json);

            JArray temp = json["Contents"] as JArray;
            if (temp != null)
            {
                Contents = new ListedObject[temp.Count];
                for (int i = 0; i < temp.Count; i++)
                {
                    Contents[i] = new ListedObject().Parse(temp[i]);
                }
            }
            else
            {
                Contents = new ListedObject[0];
            }

            Name = json["Name"]?.Value<string>();
            Prefix = json["Prefix"]?.Value<string>();
            Delimiter = json["Delimiter"]?.Value<string>();
            Marker = json["Marker"]?.Value<string>();
            MaxKeys = json["MaxKeys"]?.Value<int>() ?? 0;
            IsTruncated = json["IsTruncated"]?.Value<bool>() ?? false;
            EncodingType = json["EncodingType"]?.Value<string>();
            NextMarker = json["NextMarker"]?.Value<string>();
        }
    }

    public class ListedObject
    {
        public string Key { internal set; get; }

        public DateTime? LastModified { internal set; get; }

        public string ETag { internal set; get; }

        public long Size { internal set; get; }

        public Owner Owner { internal set; get; }

        public StorageClassType? StorageClass { internal set; get; }

        public ulong HashCrc64ecma { internal set; get; }

        internal ListedObject Parse(JToken token)
        {
            this.Key = token["Key"]?.Value<string>();
            this.LastModified = Utils.ParseJTokenDate(token["LastModified"], Constants.Iso8601DateFormat);
            this.ETag = token["ETag"]?.Value<string>();
            this.Size = token["Size"]?.Value<long>() ?? 0;
            this.Owner = new Owner()
            {
                ID = token["Owner"]?["ID"]?.Value<string>()
            };
            this.StorageClass =
                Enums.ParseEnum<StorageClassType>(token["StorageClass"]?.Value<string>()) as
                    StorageClassType?;
            string hashCrc64ecma = token["HashCrc64ecma"]?.Value<string>();
            this.HashCrc64ecma = string.IsNullOrEmpty(hashCrc64ecma) ? 0 : Convert.ToUInt64(hashCrc64ecma);
            return this;
        }
    }

    public class ListedCommonPrefix
    {
        public string Prefix { internal set; get; }
    }
}