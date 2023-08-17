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
    public class GetObjectACLInput : GenericObjectInput
    {
        internal sealed override string GetOperation()
        {
            return "GetObjectACL";
        }

        public string VersionID { set; get; }

        internal sealed override HttpRequest Trans()
        {
            HttpRequest request = base.Trans();
            request.Method = HttpMethodType.HttpMethodGet;
            if (!string.IsNullOrEmpty(VersionID))
            {
                request.Query[Constants.QueryVersionID] = VersionID;
            }

            request.Query["acl"] = string.Empty;

            return request;
        }
    }

    public class GetObjectACLOutput : GenericOutput
    {
        public string VersionID { internal set; get; }

        public Owner Owner { internal set; get; }

        public Grant[] Grants { internal set; get; }

        internal sealed override void Parse(HttpRequest request, HttpResponse response)
        {
            string temp;
            response.Header.TryGetValue(Constants.HeaderVersionID, out temp);
            VersionID = temp;

            JObject json = Utils.ParseJson(response.Body);
            JArray grants = json["Grants"] as JArray;
            if (grants != null)
            {
                Grants = new Grant[grants.Count];
                for (int i = 0; i < grants.Count; i++)
                {
                    Grants[i] = new Grant
                    {
                        Grantee = new Grantee
                        {
                            ID = grants[i]["Grantee"]?["ID"]?.Value<string>(),
                            Type =
                                Enums.ParseEnum<GranteeType>(grants[i]["Grantee"]?["Type"]
                                    ?.Value<string>()) as GranteeType?,
                            Canned =
                                Enums.ParseEnum<CannedType>(grants[i]["Grantee"]?["Canned"]?.Value<string>()) as
                                    CannedType?
                        },
                        Permission =
                            Enums.ParseEnum<PermissionType>(grants[i]["Permission"]
                                ?.Value<string>()) as PermissionType?
                    };
                }
            }
            else
            {
                Grants = new Grant[0];
            }

            Owner = new Owner
            {
                ID = json["Owner"]?["ID"]?.Value<string>()
            };
        }
    }
}