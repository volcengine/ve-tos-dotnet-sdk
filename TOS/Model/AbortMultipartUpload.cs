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
    public class AbortMultipartUploadInput : GenericObjectInput
    {
        internal sealed override string GetOperation()
        {
            return "AbortMultipartUpload";
        }

        public string UploadID { set; get; }

        internal sealed override HttpRequest Trans()
        {
            HttpRequest request = base.Trans();
            request.Method = HttpMethodType.HttpMethodDelete;
            Utils.SetUploadId(request.Query, UploadID);

            return request;
        }
    }

    public class AbortMultipartUploadOutput : GenericOutput
    {
    }
}