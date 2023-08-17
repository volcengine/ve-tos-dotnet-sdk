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

using TOS.Model;

namespace TOS
{
    internal partial class TosClient
    {
        public CreateMultipartUploadOutput CreateMultipartUpload(CreateMultipartUploadInput input)
        {
            return this.DoRequest<CreateMultipartUploadInput, CreateMultipartUploadOutput>(input);
        }

        public UploadPartOutput UploadPart(UploadPartInput input)
        {
            return this.DoRequest<UploadPartInput, UploadPartOutput>(input);
        }

        public UploadPartFromFileOutput UploadPartFromFile(UploadPartFromFileInput input)
        {
            return this.DoRequest<UploadPartFromFileInput, UploadPartFromFileOutput>(input);
        }

        public CompleteMultipartUploadOutput CompleteMultipartUpload(CompleteMultipartUploadInput input)
        {
            return this.DoRequest<CompleteMultipartUploadInput, CompleteMultipartUploadOutput>(input);
        }

        public AbortMultipartUploadOutput AbortMultipartUpload(AbortMultipartUploadInput input)
        {
            return this.DoRequest<AbortMultipartUploadInput, AbortMultipartUploadOutput>(input);
        }

        public UploadPartCopyOutput UploadPartCopy(UploadPartCopyInput input)
        {
            return this.DoRequest<UploadPartCopyInput, UploadPartCopyOutput>(input);
        }

        public ListMultipartUploadsOutput ListMultipartUploads(ListMultipartUploadsInput input)
        {
            return this.DoRequest<ListMultipartUploadsInput, ListMultipartUploadsOutput>(input);
        }

        public ListPartsOutput ListParts(ListPartsInput input)
        {
            return this.DoRequest<ListPartsInput, ListPartsOutput>(input);
        }
    }
}