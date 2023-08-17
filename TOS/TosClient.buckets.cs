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
        public CreateBucketOutput CreateBucket(CreateBucketInput input)
        {
            return this.DoRequest<CreateBucketInput, CreateBucketOutput>(input);
        }

        public HeadBucketOutput HeadBucket(HeadBucketInput input)
        {
            return this.DoRequest<HeadBucketInput, HeadBucketOutput>(input);
        }

        public DeleteBucketOutput DeleteBucket(DeleteBucketInput input)
        {
            return this.DoRequest<DeleteBucketInput, DeleteBucketOutput>(input);
        }

        public ListBucketsOutput ListBuckets()
        {
            return this.ListBuckets(new ListBucketsInput());
        }

        public ListBucketsOutput ListBuckets(ListBucketsInput input)
        {
            if (input == null)
            {
                input = new ListBucketsInput();
            }

            return this.DoRequest<ListBucketsInput, ListBucketsOutput>(input);
        }
    }
}