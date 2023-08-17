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
    public class ListObjectVersionsInput : GenericBucketInput, IListCommonQuery
    {
        private int _maxKeys = -1;

        internal sealed override string GetOperation()
        {
            return "ListObjectVersions";
        }

        public string Prefix { get; set; }

        public string Delimiter { get; set; }

        public string KeyMarker { get; set; }

        public string VersionIDMarker { get; set; }

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
            if (!string.IsNullOrEmpty(KeyMarker))
            {
                request.Query[Constants.QueryKeyMarker] = KeyMarker;
            }

            if (!string.IsNullOrEmpty(VersionIDMarker))
            {
                request.Query[Constants.QueryVersionIDMarker] = VersionIDMarker;
            }

            if (MaxKeys >= 0)
            {
                request.Query[Constants.QueryMaxKeys] = Convert.ToString(MaxKeys);
            }

            request.Query["versions"] = string.Empty;

            return request;
        }
    }

    public class ListObjectVersionsOutput : GenericOutput
    {
        public string Name { internal set; get; }

        public string Prefix { internal set; get; }


        public string KeyMarker { internal set; get; }

        public string VersionIDMarker { internal set; get; }


        public int MaxKeys { internal set; get; }


        public string Delimiter { internal set; get; }


        public bool IsTruncated { internal set; get; }


        public string EncodingType { internal set; get; }


        public string NextKeyMarker { internal set; get; }

        public string NextVersionIDMarker { internal set; get; }

        public ListedCommonPrefix[] CommonPrefixes { internal set; get; }

        public ListedObjectVersion[] Versions { internal set; get; }

        public ListedDeleteMarker[] DeleteMarkers { internal set; get; }

        internal override void Parse(HttpRequest request, HttpResponse response)
        {
            JObject json = Utils.ParseJson(response.Body);
            CommonPrefixes = Utils.ParseCommonPrefixes(json);

            JArray temp = json["Versions"] as JArray;
            if (temp != null)
            {
                Versions = new ListedObjectVersion[temp.Count];
                ListedObjectVersion version;
                for (int i = 0; i < temp.Count; i++)
                {
                    Versions[i] = new ListedObjectVersion().Parse(temp[i]);
                }
            }
            else
            {
                Versions = new ListedObjectVersion[0];
            }

            temp = json["DeleteMarkers"] as JArray;
            if (temp != null)
            {
                DeleteMarkers = new ListedDeleteMarker[temp.Count];
                ListedDeleteMarker deleteMarker;
                for (int i = 0; i < temp.Count; i++)
                {
                    DeleteMarkers[i] = new ListedDeleteMarker().Parse(temp[i]);
                }
            }
            else
            {
                DeleteMarkers = new ListedDeleteMarker[0];
            }

            Name = json["Name"]?.Value<string>();
            Prefix = json["Prefix"]?.Value<string>();
            Delimiter = json["Delimiter"]?.Value<string>();
            KeyMarker = json["KeyMarker"]?.Value<string>();
            VersionIDMarker = json["VersionIdMarker"]?.Value<string>();
            MaxKeys = json["MaxKeys"]?.Value<int>() ?? 0;
            EncodingType = json["EncodingType"]?.Value<string>();
            IsTruncated = json["IsTruncated"]?.Value<bool>() ?? false;
            NextKeyMarker = json["NextKeyMarker"]?.Value<string>();
            NextVersionIDMarker = json["NextVersionIdMarker"]?.Value<string>();
        }
    }

    public class ListedObjectVersion : ListedObject
    {
        public bool IsLatest { internal set; get; }

        public string VersionID { internal set; get; }

        internal ListedObjectVersion Parse(JToken token)
        {
            base.Parse(token);
            IsLatest = token["IsLatest"]?.Value<bool>() ?? false;
            VersionID = token["VersionId"]?.Value<string>();
            return this;
        }
    }

    public class ListedDeleteMarker
    {
        public string Key { internal set; get; }

        public DateTime? LastModified { internal set; get; }

        public bool IsLatest { internal set; get; }

        public Owner Owner { internal set; get; }

        public string VersionID { internal set; get; }

        internal ListedDeleteMarker Parse(JToken token)
        {
            Key = token["Key"]?.Value<string>();
            LastModified =
                Utils.ParseJTokenDate(token["LastModified"], Constants.Iso8601DateFormat);
            IsLatest = token["IsLatest"]?.Value<bool>() ?? false;
            Owner = new Owner()
            {
                ID = token["Owner"]?["ID"]?.Value<string>()
            };
            VersionID = token["VersionId"]?.Value<string>();
            return this;
        }
    }
}