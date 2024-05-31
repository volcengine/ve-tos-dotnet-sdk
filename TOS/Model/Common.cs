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

using System;

namespace TOS.Model
{
    public class Owner
    {
        public string ID { set; get; }
        public string DisplayName { set; get; }
    }

    public class Grantee
    {
        public string ID { set; get; }
        public string DisplayName { set; get; }

        public GranteeType? Type { set; get; }

        public CannedType? Canned { set; get; }
    }

    public class Grant
    {
        public Grantee Grantee { set; get; }

        public PermissionType? Permission { set; get; }
    }

    public class RestoreJobParameters
    {
        public TierType? Tier  { set; get; }
    }

    public class TagSet
    {
        public Tag[] Tags { set; get; }
    }
    
    public class Tag
    {
        public string Key { set; get; }
        public string Value { set; get; }
    }
    
    internal interface IHttpBasicHeader
    {
        long ContentLength { get; }

        string CacheControl { get; }

        string ContentDisposition { get; }

        string ContentEncoding { get; }

        string ContentLanguage { get; }

        string ContentType { get; }

        DateTime? Expires { get; }
    }

    internal interface ISseHeader
    {
        string SSECAlgorithm { get; }

        string SSECKey { get; }

        string SSECKeyMD5 { get; }

        string ServerSideEncryption { get; }
    }

    internal interface ICopySourceSSeHeader
    {
        string CopySourceSSECAlgorithm { get; }

        string CopySourceSSECKey { get; }

        string CopySourceSSECKeyMD5 { get; }
    }

    internal interface ICopySourceHeader
    {
        string SrcBucket { get; }

        string SrcKey { get; }

        string SrcVersionID { get; }
    }

    internal interface IAclHeader
    {
        ACLType? ACL { get; }

        string GrantFullControl { get; }

        string GrantRead { get; }

        string GrantReadAcp { get; }

        string GrantWrite { get; }

        string GrantWriteAcp { get; }
    }

    internal interface IMiscHeader
    {
        string WebsiteRedirectLocation { get; }

        StorageClassType? StorageClass { get; }
    }

    internal interface IIfConditionHeader
    {
        string IfMatch { get; }

        DateTime? IfModifiedSince { get; }

        string IfNoneMatch { get; }

        DateTime? IfUnmodifiedSince { get; }
    }

    internal interface ICopySourceIfConditionHeader
    {
        string CopySourceIfMatch { get; }

        DateTime? CopySourceIfModifiedSince { get; }

        string CopySourceIfNoneMatch { get; }

        DateTime? CopySourceIfUnmodifiedSince { get; }
    }

    internal interface IListCommonQuery
    {
        string Prefix { get; set; }

        string Delimiter { get; set; }

        string EncodingType { get; set; }
    }

    internal interface IMultipartUploadQuery
    {
        string UploadID { get; }

        int PartNumber { get; }
    }
}