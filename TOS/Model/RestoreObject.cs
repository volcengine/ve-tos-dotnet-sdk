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
using TOS.Common;
using Newtonsoft.Json.Linq;

namespace TOS.Model
{
    public class RestoreObjectInput : GenericObjectInput
    {
        public string VersionID { get; set; }
        public int Days { get; set; }
        public RestoreJobParameters RestoreJobParameters { get; set; }
        
        internal sealed override HttpRequest Trans()
        {
            HttpRequest request = base.Trans();
            request.Method = HttpMethodType.HttpMethodPost;

            if (!string.IsNullOrEmpty(VersionID))
            {
                request.Query[Constants.QueryVersionID] = VersionID;
            }
            request.Query["restore"] = string.Empty;

            JObject json = new JObject();
            json["Days"] = Days;
            
            if (RestoreJobParameters != null)
            {
                JObject restoreJobParameters = new JObject();
                restoreJobParameters["Tier"] = RestoreJobParameters.Tier.ToString();
                json["RestoreJobParameters"] = restoreJobParameters;
            }
            
            byte[] data = Encoding.UTF8.GetBytes(json.ToString());
            request.Body = Utils.PrepareStream(request.Header, data);
            
            return request;
        }
    }


    
    public class RestoreObjectOutput : GenericOutput
    {
     
    }
}