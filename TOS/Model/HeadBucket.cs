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
    public class HeadBucketInput : GenericBucketInput
    {
        internal sealed override string GetOperation()
        {
            return "HeadBucket";
        }

        internal sealed override HttpRequest Trans()
        {
            HttpRequest request = base.Trans();
            request.Method = HttpMethodType.HttpMethodHead;
            return request;
        }
    }

    public class HeadBucketOutput : GenericOutput
    {
        public string Region { internal set; get; }

        public StorageClassType? StorageClass { internal set; get; }

        public AzRedundancyType? AzRedundancy { internal set; get; }

        internal sealed override void Parse(HttpRequest request, HttpResponse response)
        {
            string temp;
            response.Header.TryGetValue(Constants.HeaderBucketRegion, out temp);
            Region = temp;

            response.Header.TryGetValue(Constants.HeaderStorageClass, out temp);
            StorageClass = Enums.ParseEnum<StorageClassType>(temp) as StorageClassType?;

            response.Header.TryGetValue(Constants.HeaderAzRedundancy, out temp);
            AzRedundancy = Enums.ParseEnum<AzRedundancyType>(temp) as AzRedundancyType?;
        }
    }
}