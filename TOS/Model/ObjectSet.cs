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
    public class PutObjectSetInput : GenericBucketInput
    {
        public string ObjectSetName { set; get; }
        public TagSet TagSet { set; get; }

        internal sealed override string GetOperation()
        {
            return "PutObjectSet";
        }

        internal sealed override HttpRequest Trans()
        {
            ObjectSetJsonCodec.ValidateName(ObjectSetName);

            JObject json = new JObject();
            json["ObjectSetName"] = ObjectSetName;
            if (TagSet != null)
            {
                json["TagSet"] = ObjectSetJsonCodec.ToJson(TagSet);
            }

            HttpRequest request = base.Trans();
            request.Method = HttpMethodType.HttpMethodPut;
            request.Query[Constants.QueryObjectSet] = string.Empty;
            request.Body = Utils.PrepareStream(request.Header, Encoding.UTF8.GetBytes(json.ToString()));
            return request;
        }
    }

    public class PutObjectSetOutput : GenericOutput
    {
    }

    public class PutObjectSetTaggingInput : GenericBucketInput
    {
        public string ObjectSetName { set; get; }
        public TagSet TagSet { set; get; }

        internal sealed override string GetOperation()
        {
            return "PutObjectSetTagging";
        }

        internal sealed override HttpRequest Trans()
        {
            ObjectSetJsonCodec.ValidateName(ObjectSetName);
            if (TagSet == null)
            {
                throw new TosClientException("tag set is required for put object set tagging");
            }

            JObject json = new JObject();
            json["ObjectSetName"] = ObjectSetName;
            json["TagSet"] = ObjectSetJsonCodec.ToJson(TagSet);

            HttpRequest request = base.Trans();
            request.Method = HttpMethodType.HttpMethodPut;
            request.Query[Constants.QueryObjectSetTagging] = string.Empty;
            request.Body = Utils.PrepareStream(request.Header, Encoding.UTF8.GetBytes(json.ToString()));
            return request;
        }
    }

    public class PutObjectSetTaggingOutput : GenericOutput
    {
    }

    public class GetObjectSetInput : GenericBucketInput
    {
        public string ObjectSetName { set; get; }

        internal sealed override string GetOperation()
        {
            return "GetObjectSet";
        }

        internal sealed override HttpRequest Trans()
        {
            ObjectSetJsonCodec.ValidateName(ObjectSetName);
            HttpRequest request = base.Trans();
            request.Method = HttpMethodType.HttpMethodGet;
            request.Query[Constants.QueryObjectSet] = string.Empty;
            request.Query[Constants.QueryObjectSetName] = ObjectSetName;
            return request;
        }
    }

    public class DeleteObjectSetInput : GenericBucketInput
    {
        public string ObjectSetName { set; get; }

        internal sealed override string GetOperation()
        {
            return "DeleteObjectSet";
        }

        internal sealed override HttpRequest Trans()
        {
            ObjectSetJsonCodec.ValidateName(ObjectSetName);
            HttpRequest request = base.Trans();
            request.Method = HttpMethodType.HttpMethodDelete;
            request.Query[Constants.QueryObjectSet] = string.Empty;
            request.Query[Constants.QueryObjectSetName] = ObjectSetName;
            return request;
        }
    }

    public class GetObjectSetTaggingInput : GenericBucketInput
    {
        public string ObjectSetName { set; get; }

        internal sealed override string GetOperation()
        {
            return "GetObjectSetTagging";
        }

        internal sealed override HttpRequest Trans()
        {
            ObjectSetJsonCodec.ValidateName(ObjectSetName);
            HttpRequest request = base.Trans();
            request.Method = HttpMethodType.HttpMethodGet;
            request.Query[Constants.QueryObjectSetTagging] = string.Empty;
            request.Query[Constants.QueryObjectSetName] = ObjectSetName;
            return request;
        }
    }

    public class GetObjectSetOutput : GenericOutput
    {
        public string ObjectSetName { internal set; get; }
        public TagSet TagSet { internal set; get; }

        internal sealed override void Parse(HttpRequest request, HttpResponse response)
        {
            ObjectSet objectSet = ObjectSetJsonCodec.Parse(Utils.ParseJson(response.Body));
            ObjectSetName = objectSet.ObjectSetName;
            TagSet = objectSet.TagSet;
        }
    }

    public class GetObjectSetTaggingOutput : GetObjectSetOutput
    {
    }

    public class DeleteObjectSetOutput : GenericOutput
    {
    }

    public class ListObjectSetInput : GenericBucketInput
    {
        private int _maxKeys = -1;

        public string Prefix { set; get; }
        public string Tags { set; get; }
        public string Marker { set; get; }

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

        internal sealed override string GetOperation()
        {
            return "ListObjectSet";
        }

        internal sealed override HttpRequest Trans()
        {
            HttpRequest request = base.Trans();
            request.Method = HttpMethodType.HttpMethodGet;
            request.Query[Constants.QueryObjectSets] = string.Empty;
            if (!string.IsNullOrEmpty(Prefix))
            {
                request.Query[Constants.QueryPrefix] = Prefix;
            }

            if (!string.IsNullOrEmpty(Tags))
            {
                request.Query[Constants.QueryTags] = Tags;
            }

            if (!string.IsNullOrEmpty(Marker))
            {
                request.Query[Constants.QueryMarker] = Marker;
            }

            if (MaxKeys >= 0)
            {
                request.Query[Constants.QueryMaxKeys] = MaxKeys.ToString(Constants.DefaultCultureInfo);
            }

            return request;
        }
    }

    public class ListObjectSetOutput : GenericOutput
    {
        public bool IsTruncated { internal set; get; }
        public string NextMarker { internal set; get; }
        public ObjectSet[] ObjectSets { internal set; get; }

        internal sealed override void Parse(HttpRequest request, HttpResponse response)
        {
            JObject json = Utils.ParseJson(response.Body);
            IsTruncated = json["IsTruncated"]?.Value<bool>() ?? false;
            NextMarker = json["NextMarker"]?.Value<string>();
            JArray sets = json["ObjectSets"] as JArray;
            ObjectSets = new ObjectSet[sets == null ? 0 : sets.Count];
            for (int i = 0; i < ObjectSets.Length; i++)
            {
                ObjectSets[i] = ObjectSetJsonCodec.Parse(sets[i] as JObject);
            }
        }
    }
}
