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
        public CopyObjectOutput CopyObject(CopyObjectInput input)
        {
            return this.DoRequest<CopyObjectInput, CopyObjectOutput>(input);
        }

        public DeleteObjectOutput DeleteObject(DeleteObjectInput input)
        {
            return this.DoRequest<DeleteObjectInput, DeleteObjectOutput>(input);
        }

        public DeleteMultiObjectsOutput DeleteMultiObjects(DeleteMultiObjectsInput input)
        {
            return this.DoRequest<DeleteMultiObjectsInput, DeleteMultiObjectsOutput>(input);
        }

        public GetObjectOutput GetObject(GetObjectInput input)
        {
            return this.DoRequest<GetObjectInput, GetObjectOutput>(input);
        }

        public GetObjectToFileOutput GetObjectToFile(GetObjectToFileInput input)
        {
            return this.DoRequest<GetObjectToFileInput, GetObjectToFileOutput>(input);
        }

        public GetObjectACLOutput GetObjectACL(GetObjectACLInput input)
        {
            return this.DoRequest<GetObjectACLInput, GetObjectACLOutput>(input);
        }

        public HeadObjectOutput HeadObject(HeadObjectInput input)
        {
            return this.DoRequest<HeadObjectInput, HeadObjectOutput>(input);
        }

        public AppendObjectOutput AppendObject(AppendObjectInput input)
        {
            return this.DoRequest<AppendObjectInput, AppendObjectOutput>(input);
        }

        public ListObjectsOutput ListObjects(ListObjectsInput input)
        {
            return this.DoRequest<ListObjectsInput, ListObjectsOutput>(input);
        }

        public ListObjectVersionsOutput ListObjectVersions(ListObjectVersionsInput input)
        {
            return this.DoRequest<ListObjectVersionsInput, ListObjectVersionsOutput>(input);
        }

        public PutObjectOutput PutObject(PutObjectInput input)
        {
            return this.DoRequest<PutObjectInput, PutObjectOutput>(input);
        }
        
        public RestoreObjectOutput RestoreObject(RestoreObjectInput input)
        {
            return this.DoRequest<RestoreObjectInput, RestoreObjectOutput>(input);
        }

        public PutObjectFromFileOutput PutObjectFromFile(PutObjectFromFileInput input)
        {
            return this.DoRequest<PutObjectFromFileInput, PutObjectFromFileOutput>(input);
        }

        public PutObjectACLOutput PutObjectACL(PutObjectACLInput input)
        {
            return this.DoRequest<PutObjectACLInput, PutObjectACLOutput>(input);
        }

        public SetObjectMetaOutput SetObjectMeta(SetObjectMetaInput input)
        {
            return this.DoRequest<SetObjectMetaInput, SetObjectMetaOutput>(input);
        }
    }
}