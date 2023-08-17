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
    public class ListMultipartUploadsInput : GenericBucketInput, IListCommonQuery
    {
        private int _maxUpload = -1;

        internal sealed override string GetOperation()
        {
            return "ListMultipartUploads";
        }

        public string Prefix { get; set; }

        public string Delimiter { get; set; }

        public string KeyMarker { get; set; }

        public string UploadIDMarker { get; set; }

        public int MaxUploads
        {
            get { return _maxUpload; }
            set
            {
                if (value >= 0)
                {
                    _maxUpload = value;
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

            if (!string.IsNullOrEmpty(UploadIDMarker))
            {
                request.Query[Constants.QueryUploadIDMarker] = UploadIDMarker;
            }

            if (MaxUploads >= 0)
            {
                request.Query[Constants.QueryMaxUploads] = Convert.ToString(MaxUploads);
            }

            request.Query["uploads"] = string.Empty;

            return request;
        }
    }

    public class ListMultipartUploadsOutput : GenericOutput
    {
        public string Bucket { internal set; get; }

        public string Prefix { internal set; get; }

        public string KeyMarker { internal set; get; }

        public string UploadIDMarker { internal set; get; }

        public int MaxUploads { internal set; get; }

        public string Delimiter { internal set; get; }

        public bool IsTruncated { internal set; get; }

        public string EncodingType { internal set; get; }

        public string NextKeyMarker { internal set; get; }

        public string NextUploadIDMarker { internal set; get; }

        public ListedCommonPrefix[] CommonPrefixes { internal set; get; }

        public ListedUpload[] Uploads { internal set; get; }

        internal sealed override void Parse(HttpRequest request, HttpResponse response)
        {
            JObject json = Utils.ParseJson(response.Body);
            CommonPrefixes = Utils.ParseCommonPrefixes(json);
            JArray temp = json["Uploads"] as JArray;
            if (temp != null)
            {
                Uploads = new ListedUpload[temp.Count];
                for (int i = 0; i < temp.Count; i++)
                {
                    Uploads[i] = new ListedUpload().Parse(temp[i]);
                }
            }
            else
            {
                Uploads = new ListedUpload[0];
            }

            Bucket = json["Bucket"]?.Value<string>();
            Prefix = json["Prefix"]?.Value<string>();
            KeyMarker = json["KeyMarker"]?.Value<string>();
            UploadIDMarker = json["UploadIdMarker"]?.Value<string>();
            MaxUploads = json["MaxUploads"]?.Value<int>() ?? 0;
            Delimiter = json["Delimiter"]?.Value<string>();
            IsTruncated = json["IsTruncated"]?.Value<bool>() ?? false;
            EncodingType = json["EncodingType"]?.Value<string>();
            NextKeyMarker = json["NextKeyMarker"]?.Value<string>();
            NextUploadIDMarker = json["NextUploadIdMarker"]?.Value<string>();
        }
    }

    public class ListedUpload
    {
        public string Key { internal set; get; }

        public string UploadID { internal set; get; }

        public Owner Owner { internal set; get; }

        public StorageClassType? StorageClass { internal set; get; }

        public DateTime? Initiated { internal set; get; }

        internal ListedUpload Parse(JToken token)
        {
            this.Key = token["Key"]?.Value<string>();
            this.UploadID = token["UploadId"]?.Value<string>();
            this.Owner = new Owner()
            {
                ID = token["Owner"]?["ID"]?.Value<string>()
            };
            this.StorageClass =
                Enums.ParseEnum<StorageClassType>(token["StorageClass"]?.Value<string>()) as
                    StorageClassType?;

            this.Initiated = Utils.ParseJTokenDate(token["Initiated"], Constants.Iso8601DateFormat);

            return this;
        }
    }
}