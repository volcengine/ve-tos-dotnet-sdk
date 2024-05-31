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
using System.IO;
using System.Text;
using Newtonsoft.Json.Linq;
using TOS.Common;
using TOS.Error;

namespace TOS.Model
{
    public class DeleteObjectTaggingInput : GenericObjectInput
    {
        public string VersionID { set; get; }
        
        internal sealed override HttpRequest Trans()
        {
            HttpRequest request = base.Trans();
            request.Method = HttpMethodType.HttpMethodDelete;

            if (!string.IsNullOrEmpty(VersionID))
            {
                request.Query[Constants.QueryVersionID] = VersionID;
            }
            
            request.Query[Constants.QueryTagging] = string.Empty;
            return request;
        }
    }

    public class DeleteObjectTaggingOutput : GenericOutput
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