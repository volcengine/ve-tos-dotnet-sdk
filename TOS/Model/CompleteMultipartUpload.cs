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
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json.Linq;
using TOS.Common;
using TOS.Error;

namespace TOS.Model
{
    public class CompleteMultipartUploadInput : GenericObjectInput
    {
        internal sealed override string GetOperation()
        {
            return "CompleteMultipartUpload";
        }

        public string UploadID { set; get; }

        public UploadedPart[] Parts { set; get; }

        public string Callback  { set; get; }
        public string CallbackVar  { set; get; }

        internal sealed override HttpRequest Trans()
        {
            HttpRequest request = base.Trans();
            request.Method = HttpMethodType.HttpMethodPost;
            if (Parts == null || Parts.Length == 0)
            {
                throw new TosClientException("empty parts for complete multipart upload");
            }

            JArray parts = new JArray();
            JObject temp;
            List<UploadedPart> partList = new List<UploadedPart>(Parts);
            partList.Sort(delegate(UploadedPart x, UploadedPart y)
            {
                return x.PartNumber == y.PartNumber ? 0 : x.PartNumber > y.PartNumber ? 1 : -1;
            });

            foreach (UploadedPart part in partList)
            {
                if (part == null)
                {
                    continue;
                }

                temp = new JObject();
                temp["PartNumber"] = part.PartNumber;
                temp["ETag"] = part.ETag;
                parts.Add(temp);
            }

            if (parts.Count == 0)
            {
                throw new TosClientException("empty grants for complete multipart upload");
            }
            
            if (!string.IsNullOrEmpty(Callback))
            {
                request.Header[Constants.HeaderCallback] = Callback;
            }
            
            if (!string.IsNullOrEmpty(CallbackVar))
            {
                request.Header[Constants.HeaderCallbackVar] = CallbackVar;
            }

            JObject json = new JObject();
            json["Parts"] = parts;
            byte[] data = Encoding.UTF8.GetBytes(json.ToString());
            request.Body = Utils.PrepareStream(request.Header, data);
            Utils.SetUploadId(request.Query, UploadID);

            return request;
        }
    }

    public class CompleteMultipartUploadOutput : GenericOutput
    {
        public string Bucket { internal set; get; }

        public string Key { internal set; get; }

        public string ETag { internal set; get; }

        public string Location { internal set; get; }

        public string VersionID { internal set; get; }

        public ulong HashCrc64ecma { internal set; get; }

        public string CallbackResult { internal set; get; }
        
        internal override void Parse(HttpRequest request, HttpResponse response)
        {
            string temp;
            response.Header.TryGetValue(Constants.HeaderVersionID, out temp);
            VersionID = temp;

            response.Header.TryGetValue(Constants.HeaderHashCrc64ecma, out temp);
            if (!string.IsNullOrEmpty(temp))
            {
                HashCrc64ecma = Convert.ToUInt64(temp);
            }
            
            request.Header.TryGetValue(Constants.HeaderCallback, out temp);
            if (!string.IsNullOrEmpty(temp))
            {
                CallbackResult = Utils.GetStreamString(response.Body);
                
                string tempHeader;
                response.Header.TryGetValue(Constants.HeaderETag, out tempHeader);
                if (!string.IsNullOrEmpty(tempHeader))
                {
                    ETag = tempHeader;
                }
                
                response.Header.TryGetValue(Constants.HeaderLocation, out tempHeader);
                if (!string.IsNullOrEmpty(tempHeader))
                {
                    Location = tempHeader;
                }
            }
            else
            {
                JObject json = Utils.ParseJson(response.Body);
                Bucket = json["Bucket"]?.Value<string>();
                Key = json["Key"]?.Value<string>();
                ETag = json["ETag"]?.Value<string>();
                Location = json["Location"]?.Value<string>();  
            }
        }
    }

    public class UploadedPart
    {
        public int PartNumber { set; get; }

        public string ETag { set; get; }

        public long Size { internal set; get; }

        public DateTime? LastModified { internal set; get; }

        internal UploadedPart Parse(JToken token)
        {
            PartNumber = token["PartNumber"]?.Value<int>() ?? 0;
            ETag = token["ETag"]?.Value<string>();
            Size = token["Size"]?.Value<long>() ?? 0;
            LastModified = Utils.ParseJTokenDate(token["LastModified"], Constants.Iso8601DateFormat);
            return this;
        }
    }
}