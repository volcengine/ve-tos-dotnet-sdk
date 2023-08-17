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
    public class PutObjectACLInput : GenericObjectInput, IAclHeader
    {
        internal sealed override string GetOperation()
        {
            return "PutObjectACL";
        }

        public string VersionID { set; get; }

        public ACLType? ACL { set; get; }

        public string GrantFullControl { set; get; }

        public string GrantRead { set; get; }

        public string GrantReadAcp { set; get; }

        string IAclHeader.GrantWrite { get; }

        public string GrantWriteAcp { set; get; }

        public Owner Owner { set; get; }
        public Grant[] Grants { set; get; }

        internal sealed override HttpRequest Trans()
        {
            HttpRequest request = base.Trans();
            request.Method = HttpMethodType.HttpMethodPut;

            if (ACL.HasValue && Grants != null && Grants.Length > 0)
            {
                throw new TosClientException("both acl and grants are set for set object acl");
            }

            if (ACL.HasValue)
            {
                Utils.SetAclHeader(request.Header, this);
            }
            else if (Grants == null || Grants.Length == 0)
            {
                throw new TosClientException("neither acl nor grants is set for set object acl");
            }
            else if (Owner == null || string.IsNullOrEmpty(Owner.ID))
            {
                throw new TosClientException("empty owner id for set object acl");
            }
            else
            {
                JArray grants = new JArray();
                JObject temp;
                foreach (Grant grant in Grants)
                {
                    if (grant == null || grant.Grantee == null)
                    {
                        continue;
                    }

                    if (!grant.Permission.HasValue)
                    {
                        throw new TosClientException("invalid permission type for set object acl");
                    }

                    temp = new JObject();
                    temp["Permission"] = Enums.TransEnum(grant.Permission);
                    temp["Grantee"] = this.TransGrantee(grant);
                    grants.Add(temp);
                }

                if (grants.Count == 0)
                {
                    throw new TosClientException("empty grants for set object acl");
                }

                JObject json = new JObject();
                JObject owner = new JObject();
                owner["ID"] = Owner.ID;
                json["Owner"] = owner;
                json["Grants"] = grants;
                byte[] data = Encoding.UTF8.GetBytes(json.ToString());
                request.Body = Utils.PrepareStream(request.Header, data);
            }

            if (!string.IsNullOrEmpty(VersionID))
            {
                request.Query[Constants.QueryVersionID] = VersionID;
            }

            request.Query["acl"] = string.Empty;
            return request;
        }

        private JObject TransGrantee(Grant grant)
        {
            if (!grant.Grantee.Type.HasValue)
            {
                throw new TosClientException("invalid grantee type for set object acl");
            }

            JObject grantee = new JObject();
            if (grant.Grantee.Type == GranteeType.GranteeGroup)
            {
                if (!grant.Grantee.Canned.HasValue)
                {
                    throw new TosClientException("invalid canned type for set object acl");
                }

                grantee["Canned"] = Enums.TransEnum(grant.Grantee.Canned);
            }
            else if (string.IsNullOrEmpty(grant.Grantee.ID))
            {
                throw new TosClientException("empty grantee id for set object acl");
            }
            else
            {
                grantee["ID"] = grant.Grantee.ID;
            }

            grantee["Type"] = Enums.TransEnum(grant.Grantee.Type);

            return grantee;
        }
    }

    public class PutObjectACLOutput : GenericOutput
    {
    }
}