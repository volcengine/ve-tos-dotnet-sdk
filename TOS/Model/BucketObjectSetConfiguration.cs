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
    public class QosConfig
    {
        public int? ReadsQps { set; get; }
        public int? WritesQps { set; get; }
        public int? ListQps { set; get; }
        public int? ReadsRate { set; get; }
        public int? WritesRate { set; get; }
    }

    public class PutBucketObjectSetConfigurationInput : GenericBucketInput
    {
        public int PathLevel { set; get; }
        public string CustomDelimiter { set; get; }
        public bool EnableDefaultObjectSet { set; get; }
        public string StorageQuota { set; get; }
        public QosConfig Qos { set; get; }

        internal sealed override string GetOperation()
        {
            return "PutBucketObjectSetConfiguration";
        }

        internal sealed override HttpRequest Trans()
        {
            if (PathLevel <= 0)
            {
                throw new TosClientException("path level must be positive for bucket object set configuration");
            }

            JObject json = new JObject();
            json["PathLevel"] = PathLevel;
            json["EnableDefaultObjectSet"] = EnableDefaultObjectSet;
            if (!string.IsNullOrEmpty(CustomDelimiter))
            {
                json["CustomDelimiter"] = CustomDelimiter;
            }

            if (!string.IsNullOrEmpty(StorageQuota))
            {
                json["StorageQuota"] = StorageQuota;
            }

            if (Qos != null)
            {
                json["Qos"] = ObjectSetConfigurationJsonCodec.ToJson(Qos);
            }

            HttpRequest request = base.Trans();
            request.Method = HttpMethodType.HttpMethodPut;
            request.Query[Constants.QueryObjectSetConfiguration] = string.Empty;
            request.Body = Utils.PrepareStream(request.Header, Encoding.UTF8.GetBytes(json.ToString()));
            return request;
        }
    }

    public class PutBucketObjectSetConfigurationOutput : GenericOutput
    {
    }

    public class GetBucketObjectSetConfigurationInput : GenericBucketInput
    {
        internal sealed override string GetOperation()
        {
            return "GetBucketObjectSetConfiguration";
        }

        internal sealed override HttpRequest Trans()
        {
            HttpRequest request = base.Trans();
            request.Method = HttpMethodType.HttpMethodGet;
            request.Query[Constants.QueryObjectSetConfiguration] = string.Empty;
            return request;
        }
    }

    public class GetBucketObjectSetConfigurationOutput : GenericOutput
    {
        public int PathLevel { internal set; get; }
        public string CustomDelimiter { internal set; get; }
        public bool EnableDefaultObjectSet { internal set; get; }
        public string StorageQuota { internal set; get; }
        public QosConfig Qos { internal set; get; }

        internal sealed override void Parse(HttpRequest request, HttpResponse response)
        {
            JObject json = Utils.ParseJson(response.Body);
            PathLevel = json["PathLevel"]?.Value<int>() ?? 0;
            CustomDelimiter = json["CustomDelimiter"]?.Value<string>();
            EnableDefaultObjectSet = json["EnableDefaultObjectSet"]?.Value<bool>() ?? false;
            StorageQuota = json["StorageQuota"]?.Value<string>();
            Qos = ObjectSetConfigurationJsonCodec.Parse(json["Qos"] as JObject);
        }
    }

    internal static class ObjectSetConfigurationJsonCodec
    {
        internal static JObject ToJson(QosConfig qos)
        {
            JObject json = new JObject();
            Add(json, "ReadsQps", qos.ReadsQps);
            Add(json, "WritesQps", qos.WritesQps);
            Add(json, "ListQps", qos.ListQps);
            Add(json, "ReadsRate", qos.ReadsRate);
            Add(json, "WritesRate", qos.WritesRate);
            return json;
        }

        internal static QosConfig Parse(JObject json)
        {
            if (json == null)
            {
                return null;
            }

            return new QosConfig
            {
                ReadsQps = json["ReadsQps"]?.Value<int?>(),
                WritesQps = json["WritesQps"]?.Value<int?>(),
                ListQps = json["ListQps"]?.Value<int?>(),
                ReadsRate = json["ReadsRate"]?.Value<int?>(),
                WritesRate = json["WritesRate"]?.Value<int?>()
            };
        }

        private static void Add(JObject json, string key, int? value)
        {
            if (value.HasValue)
            {
                json[key] = value.Value;
            }
        }
    }
}
