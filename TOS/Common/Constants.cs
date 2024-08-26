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

using System.Globalization;

namespace TOS.Common
{
    internal class Constants
    {
        // http client
        internal const string Version = "v2.1.7";
        internal const int DefaultConnectionTimeout = 10000;
        internal const int DefaultSocketTimeout = 30000;
        internal const int DefaultMaxConnections = 1024;
        internal const int DefaultIdleConnectionTime = 60000;
        internal const int DefaultBufferSize = 8192;
        internal const int MaxDiscardSize = 10 * 1024 * 1024;
        internal const string DefaultRegion = RegionBeijing;
        internal const string RegionBeijing = "cn-beijing";
        internal const string RegionGuangzhou = "cn-guangzhou";
        internal const string RegionShanghai = "cn-shanghai";
        internal const int DefaultExpires = 3600;

        internal const string DefaultEncoding = "utf-8";
        internal const string Iso8859Encoding = "ISO-8859-1";

        // URI encode every byte except the unreserved characters: 'A'-'Z', 'a'-'z', '0'-'9', '-', '.', '_', and '~'.
        // The space character is a reserved character and must be encoded as "%20" (and not as "+").
        internal const string AllowedInUrl = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-._~";

        internal const string EscapeSep = "%";
        internal const string ConnectionKeepAlive = "Keep-Alive";

        internal const string LongDateFormat = "yyyyMMdd\\THHmmss\\Z";
        public const string Rfc1123DateFormat = "ddd, dd MMM yyyy HH:mm:ss \\G\\M\\T";
        public const string Iso8601DateFormat = "yyyy-MM-dd\\THH:mm:ss\\Z";

        internal const string SchemaHttp = "http://";
        internal const string SchemaHttps = "https://";
        internal const string AlgorithmAes256 = "AES256";
        internal const string True = "true";

        // header key
        internal const string HeaderPrefix = "x-tos-";
        internal const string HeaderPrefixMeta = "x-tos-meta-";
        internal const string HeaderUserAgent = "User-Agent";
        internal const string HeaderConnection = "Connection";
        internal const string HeaderContentMD5 = "Content-MD5";
        internal const string HeaderContentLength = "Content-Length";
        internal const string HeaderCacheControl = "Cache-Control";
        internal const string HeaderContentType = "Content-Type";
        internal const string HeaderContentTypeLower = "content-type";
        internal const string HeaderContentLanguage = "Content-Language";
        internal const string HeaderContentEncoding = "Content-Encoding";
        internal const string HeaderContentDisposition = "Content-Disposition";
        internal const string HeaderExpires = "Expires";
        internal const string HeaderHost = "Host";
        internal const string HeaderHostLower = "host";
        internal const string HeaderETag = "ETag";
        internal const string HeaderLastModified = "Last-Modified";
        internal const string HeaderRange = "Range";
        internal const string HeaderContentRange = "Content-Range";
        internal const string HeaderIfMatch = "If-Match";
        internal const string HeaderIfModifiedSince = "If-Modified-Since";
        internal const string HeaderIfNoneMatch = "If-None-Match";
        internal const string HeaderIfUnmodifiedSince = "If-Unmodified-Since";
        internal const string HeaderLocation = "Location";
        internal const string HeaderAuthorization = "Authorization";
        internal const string HeaderSecurityToken = HeaderPrefix + "security-token";
        internal const string HeaderRequestDate = HeaderPrefix + "date";
        internal const string HeaderRequestID = HeaderPrefix + "request-id";
        internal const string HeaderID2 = HeaderPrefix + "id-2";
        internal const string HeaderContentSHA256 = HeaderPrefix + "content-sha256";
        internal const string HeaderSSECAlgorithm = HeaderPrefix + "server-side-encryption-customer-algorithm";
        internal const string HeaderSSECKey = HeaderPrefix + "server-side-encryption-customer-key";
        internal const string HeaderSSECKeyMD5 = HeaderPrefix + "server-side-encryption-customer-key-MD5";
        internal const string HeaderServerSideEncryption = HeaderPrefix + "server-side-encryption";
        internal const string HeaderCallback = HeaderPrefix + "callback";
        internal const string HeaderCallbackVar = HeaderPrefix + "callback-var";

        internal const string HeaderCopySourceSSECAlgorithm =
            HeaderPrefix + "copy-source-server-side-encryption-customer-algorithm";

        internal const string HeaderCopySourceSSECKey =
            HeaderPrefix + "copy-source-server-side-encryption-customer-key";

        internal const string HeaderCopySourceSSECKeyMD5 =
            HeaderPrefix + "copy-source-server-side-encryption-customer-key-MD5";

        internal const string HeaderCopySourceIfMatch = HeaderPrefix + "copy-source-if-match";
        internal const string HeaderCopySourceIfModifiedSince = HeaderPrefix + "copy-source-if-modified-since";
        internal const string HeaderCopySourceIfNoneMatch = HeaderPrefix + "copy-source-if-none-match";
        internal const string HeaderCopySourceIfUnmodifiedSince = HeaderPrefix + "copy-source-if-unmodified-since";

        internal const string HeaderTagging = HeaderPrefix + "tagging";
        internal const string HeaderTaggingCount = HeaderPrefix + "tagging-count";
        internal const string HeaderTaggingDirective = HeaderPrefix + "tagging-directive";
        internal const string HeaderVersionID = HeaderPrefix + "version-id";
        internal const string HeaderCopySourceVersionID = HeaderPrefix + "copy-source-version-id";
        internal const string HeaderHashCrc64ecma = HeaderPrefix + "hash-crc64ecma";
        internal const string HeaderAcl = HeaderPrefix + "acl";
        internal const string HeaderGrantFullControl = HeaderPrefix + "grant-full-control";
        internal const string HeaderGrantRead = HeaderPrefix + "grant-read";
        internal const string HeaderGrantReadAcp = HeaderPrefix + "grant-read-acp";
        internal const string HeaderGrantWrite = HeaderPrefix + "grant-write";
        internal const string HeaderGrantWriteAcp = HeaderPrefix + "grant-write-acp";
        internal const string HeaderStorageClass = HeaderPrefix + "storage-class";
        internal const string HeaderAzRedundancy = HeaderPrefix + "az-redundancy";
        internal const string HeaderWebsiteRedirectLocation = HeaderPrefix + "website-redirect-location";
        internal const string HeaderDeleteMarker = HeaderPrefix + "delete-marker";
        internal const string HeaderObjectType = HeaderPrefix + "object-type";
        internal const string HeaderNextAppendOffset = HeaderPrefix + "next-append-offset";
        internal const string HeaderBucketRegion = HeaderPrefix + "bucket-region";
        internal const string HeaderCopySource = HeaderPrefix + "copy-source";
        internal const string HeaderCopySourceRange = HeaderPrefix + "copy-source-range";
        internal const string HeaderMetadataDirective = HeaderPrefix + "metadata-directive";

        // query key
        internal const string QueryVersionID = "versionId";
        internal const string QueryPartNumber = "partNumber";
        internal const string QueryResponseCacheControl = "response-cache-control";
        internal const string QueryResponseContentDisposition = "response-content-disposition";
        internal const string QueryResponseContentEncoding = "response-content-encoding";
        internal const string QueryResponseContentLanguage = "response-content-language";
        internal const string QueryResponseContentType = "response-content-type";
        internal const string QueryResponseExpires = "response-expires";
        internal const string QueryPrefix = "prefix";
        internal const string QueryDelimiter = "delimiter";
        internal const string QueryEncodingType = "encoding-type";
        internal const string QueryMarker = "marker";
        internal const string QueryKeyMarker = "key-marker";
        internal const string QueryVersionIDMarker = "version-id-marker";
        internal const string QueryUploadIDMarker = "upload-id-marker";
        internal const string QueryMaxKeys = "max-keys";
        internal const string QueryMaxUploads = "max-uploads";
        internal const string QueryMaxParts = "max-parts";
        internal const string QueryUploadID = "uploadId";
        internal const string QueryNextPartNumberMarker = "part-number-marker";
        internal const string QueryAlgorithm = "X-Tos-Algorithm";
        internal const string QueryCredential = "X-Tos-Credential";
        internal const string QueryDate = "X-Tos-Date";
        internal const string QueryExpires = "X-Tos-Expires";
        internal const string QuerySignedHeaders = "X-Tos-SignedHeaders";
        internal const string QuerySecurityToken = "X-Tos-Security-Token";
        internal const string QuerySignature = "X-Tos-Signature";
        internal const string QueryTagging = "tagging";

        internal static readonly CultureInfo DefaultCultureInfo = CultureInfo.GetCultureInfo("en-US");
    }
}