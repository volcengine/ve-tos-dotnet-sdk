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
        public PutBucketObjectSetConfigurationOutput PutBucketObjectSetConfiguration(PutBucketObjectSetConfigurationInput input)
        {
            return this.DoRequest<PutBucketObjectSetConfigurationInput, PutBucketObjectSetConfigurationOutput>(input);
        }

        public GetBucketObjectSetConfigurationOutput GetBucketObjectSetConfiguration(GetBucketObjectSetConfigurationInput input)
        {
            return this.DoRequest<GetBucketObjectSetConfigurationInput, GetBucketObjectSetConfigurationOutput>(input);
        }

        public PutObjectSetOutput PutObjectSet(PutObjectSetInput input)
        {
            return this.DoRequest<PutObjectSetInput, PutObjectSetOutput>(input);
        }

        public GetObjectSetOutput GetObjectSet(GetObjectSetInput input)
        {
            return this.DoRequest<GetObjectSetInput, GetObjectSetOutput>(input);
        }

        public ListObjectSetOutput ListObjectSet(ListObjectSetInput input)
        {
            return this.DoRequest<ListObjectSetInput, ListObjectSetOutput>(input);
        }

        public DeleteObjectSetOutput DeleteObjectSet(DeleteObjectSetInput input)
        {
            return this.DoRequest<DeleteObjectSetInput, DeleteObjectSetOutput>(input);
        }

        public PutObjectSetTaggingOutput PutObjectSetTagging(PutObjectSetTaggingInput input)
        {
            return this.DoRequest<PutObjectSetTaggingInput, PutObjectSetTaggingOutput>(input);
        }

        public GetObjectSetTaggingOutput GetObjectSetTagging(GetObjectSetTaggingInput input)
        {
            return this.DoRequest<GetObjectSetTaggingInput, GetObjectSetTaggingOutput>(input);
        }

        public PutObjectSetLifecycleOutput PutObjectSetLifecycle(PutObjectSetLifecycleInput input)
        {
            return this.DoRequest<PutObjectSetLifecycleInput, PutObjectSetLifecycleOutput>(input);
        }

        public GetObjectSetLifecycleOutput GetObjectSetLifecycle(GetObjectSetLifecycleInput input)
        {
            return this.DoRequest<GetObjectSetLifecycleInput, GetObjectSetLifecycleOutput>(input);
        }

        public DeleteObjectSetLifecycleOutput DeleteObjectSetLifecycle(DeleteObjectSetLifecycleInput input)
        {
            return this.DoRequest<DeleteObjectSetLifecycleInput, DeleteObjectSetLifecycleOutput>(input);
        }

        public PutObjectSetLifecycleByTagOutput PutObjectSetLifecycleByTag(PutObjectSetLifecycleByTagInput input)
        {
            return this.DoRequest<PutObjectSetLifecycleByTagInput, PutObjectSetLifecycleByTagOutput>(input);
        }

        public GetObjectSetLifecycleByTagOutput GetObjectSetLifecycleByTag(GetObjectSetLifecycleByTagInput input)
        {
            return this.DoRequest<GetObjectSetLifecycleByTagInput, GetObjectSetLifecycleByTagOutput>(input);
        }

        public DeleteObjectSetLifecycleByTagOutput DeleteObjectSetLifecycleByTag(
            DeleteObjectSetLifecycleByTagInput input)
        {
            return this.DoRequest<DeleteObjectSetLifecycleByTagInput, DeleteObjectSetLifecycleByTagOutput>(input);
        }
    }
}
