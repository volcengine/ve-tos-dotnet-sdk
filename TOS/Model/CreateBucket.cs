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

using TOS.Common;

namespace TOS.Model
{
    public class CreateBucketInput : GenericBucketInput, IAclHeader
    {
        internal sealed override string GetOperation()
        {
            return "CreateBucket";
        }

        public ACLType? ACL { set; get; }

        public string GrantFullControl { set; get; }

        public string GrantRead { set; get; }

        public string GrantReadAcp { set; get; }

        public string GrantWrite { set; get; }

        public string GrantWriteAcp { set; get; }

        public StorageClassType? StorageClass { set; get; }

        public AzRedundancyType? AzRedundancy { set; get; }

        internal sealed override HttpRequest Trans()
        {
            HttpRequest request = base.Trans();
            request.Method = HttpMethodType.HttpMethodPut;
            Utils.SetAclHeader(request.Header, this);

            if (StorageClass.HasValue)
            {
                request.Header[Constants.HeaderStorageClass] = Enums.TransEnum(StorageClass.Value);
            }

            if (AzRedundancy.HasValue)
            {
                request.Header[Constants.HeaderAzRedundancy] = Enums.TransEnum(AzRedundancy.Value);
            }

            return request;
        }
    }

    public class CreateBucketOutput : GenericOutput
    {
        public string Location { internal set; get; }

        internal sealed override void Parse(HttpRequest request, HttpResponse response)
        {
            string temp;
            response.Header.TryGetValue(Constants.HeaderLocation, out temp);
            Location = temp;
        }
    }
}