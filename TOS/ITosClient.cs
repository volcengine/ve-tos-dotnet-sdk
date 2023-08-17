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
    public interface ITosClient
    {
        CreateBucketOutput CreateBucket(CreateBucketInput input);

        HeadBucketOutput HeadBucket(HeadBucketInput input);

        DeleteBucketOutput DeleteBucket(DeleteBucketInput input);

        ListBucketsOutput ListBuckets();

        ListBucketsOutput ListBuckets(ListBucketsInput input);

        CopyObjectOutput CopyObject(CopyObjectInput input);

        DeleteObjectOutput DeleteObject(DeleteObjectInput input);

        DeleteMultiObjectsOutput DeleteMultiObjects(DeleteMultiObjectsInput input);
        GetObjectOutput GetObject(GetObjectInput input);

        GetObjectToFileOutput GetObjectToFile(GetObjectToFileInput input);

        GetObjectACLOutput GetObjectACL(GetObjectACLInput input);

        HeadObjectOutput HeadObject(HeadObjectInput input);

        AppendObjectOutput AppendObject(AppendObjectInput input);

        ListObjectsOutput ListObjects(ListObjectsInput input);

        ListObjectVersionsOutput ListObjectVersions(ListObjectVersionsInput input);

        PutObjectOutput PutObject(PutObjectInput input);

        PutObjectFromFileOutput PutObjectFromFile(PutObjectFromFileInput input);

        PutObjectACLOutput PutObjectACL(PutObjectACLInput input);

        SetObjectMetaOutput SetObjectMeta(SetObjectMetaInput input);

        CreateMultipartUploadOutput CreateMultipartUpload(CreateMultipartUploadInput input);

        UploadPartOutput UploadPart(UploadPartInput input);

        UploadPartFromFileOutput UploadPartFromFile(UploadPartFromFileInput input);

        CompleteMultipartUploadOutput CompleteMultipartUpload(CompleteMultipartUploadInput input);

        AbortMultipartUploadOutput AbortMultipartUpload(AbortMultipartUploadInput input);

        UploadPartCopyOutput UploadPartCopy(UploadPartCopyInput input);

        ListMultipartUploadsOutput ListMultipartUploads(ListMultipartUploadsInput input);

        ListPartsOutput ListParts(ListPartsInput input);

        PreSignedURLOutput PreSignedURL(PreSignedURLInput input);
    }
}