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
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TOS.Error;
using TOS.Model;

namespace TOS.Common
{
    internal static class Utils
    {
        private static readonly Regex _ipAddressPattern =
            new Regex(@"^((2[0-4]\d|25[0-5]|[01]?\d\d?)\.){3}(2[0-4]\d|25[0-5]|[01]?\d\d?)$");

        private static readonly Regex _bucketPattern = new Regex(@"^[a-z0-9-]+$");

        internal static bool IsIpAddress(string domain)
        {
            if (string.IsNullOrEmpty(domain)) throw new TosClientException("domain is null");

            return _ipAddressPattern.IsMatch(domain);
        }

        internal static string UrlEncode(string uriToEncode)
        {
            return UrlEncode(uriToEncode, string.Empty);
        }
        
        internal static string UrlEncodeChinese(string uriToEncode)
        {
            Regex regex = new Regex(@"[\u4e00-\u9fa5]");
            return regex.Replace(uriToEncode, m => UrlEncode(m.Value, string.Empty));
        }

        internal static string UrlEncode(string uriToEncode, string safe)
        {
            if (string.IsNullOrEmpty(uriToEncode)) return string.Empty;

            StringBuilder encodedUri = new StringBuilder(uriToEncode.Length * 2);
            byte[] bytes = Encoding.UTF8.GetBytes(uriToEncode);
            foreach (byte b in bytes)
            {
                char ch = (char)b;
                if (Constants.AllowedInUrl.IndexOf(ch) != -1)
                    encodedUri.Append(ch);
                else if (!string.IsNullOrEmpty(safe) && safe.IndexOf(ch) != -1)
                    encodedUri.Append(ch);
                else
                    encodedUri.Append(Constants.EscapeSep).Append(
                        string.Format(CultureInfo.InvariantCulture, "{0:X2}", (int)b));
            }

            return encodedUri.ToString();
        }

        internal static string UrlDecode(string uriToDecode)
        {
            if (string.IsNullOrEmpty(uriToDecode)) return string.Empty;
            return Uri.UnescapeDataString(uriToDecode.Replace("+", " "));
        }

        internal static string HexSha256(string input)
        {
            return Hex(Sha256(input));
        }

        internal static byte[] Sha256(string input)
        {
            byte[] hash;
            using (SHA256 s256 = new SHA256Managed())
            {
                hash = s256.ComputeHash(Encoding.UTF8.GetBytes(input));
                s256.Clear();
            }

            return hash;
        }

        internal static string Base64Md5(string input)
        {
            return Base64Md5(Encoding.UTF8.GetBytes(input));
        }

        internal static string Base64Md5(byte[] input)
        {
            return Convert.ToBase64String(Md5(input));
        }

        internal static byte[] Md5(byte[] input)
        {
            return new MD5CryptoServiceProvider().ComputeHash(input);
        }

        internal static string Hex(byte[] input)
        {
            StringBuilder sb = new StringBuilder(input.Length * 2);
            foreach (byte b in input)
            {
                sb.AppendFormat("{0:x2}", b);
            }

            return sb.ToString();
        }

        internal static byte[] HmacSha256(byte[] signKey, string input)
        {
            byte[] byteToSign = Encoding.UTF8.GetBytes(input);
            HMACSHA256 hmac = new HMACSHA256(signKey);
            return hmac.ComputeHash(byteToSign);
        }

        internal static byte[] HmacSha256(string signKey, string input)
        {
            return HmacSha256(Encoding.UTF8.GetBytes(signKey), input);
        }

        internal static long WriteTo(Stream src, Stream dst, int bufferSize)
        {
            byte[] buffer = new byte[bufferSize];
            long written = 0;
            int readOnce;
            while ((readOnce = src.Read(buffer, 0, buffer.Length)) > 0)
            {
                written += readOnce;
                dst.Write(buffer, 0, readOnce);
            }

            dst.Flush();
            return written;
        }

        internal static long WriteTo(Stream src, Stream dst, long totalSize, int bufferSize)
        {
            byte[] buffer = new byte[bufferSize];

            long written = 0;
            int readOnce;
            while (written < totalSize && (readOnce = src.Read(buffer, 0, buffer.Length)) > 0)
            {
                if (written + readOnce > totalSize) readOnce = (int)(totalSize - written);

                written += readOnce;
                dst.Write(buffer, 0, readOnce);
            }

            dst.Flush();
            return written;
        }

        internal static void CheckBucket(string bucket)
        {
            if (bucket == null || bucket.Length < 3 || bucket.Length > 63)
                throw new TosClientException("invalid bucket name, the length must be [3, 63]");

            if (bucket[0] == '-' || bucket[bucket.Length - 1] == '-')
                throw new TosClientException(
                    "invalid bucket name, the bucket name can be neither starting with '-' nor ending with '-'");

            if (!_bucketPattern.IsMatch(bucket))
                throw new TosClientException("invalid bucket name, the character set is illegal");
        }

        internal static void CheckKey(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new TosClientException("invalid object name");
            }
        }

        internal static void CloseIDisposable(IDisposable disposable)
        {
            if (disposable != null)
                try
                {
                    disposable.Dispose();
                }
                catch (Exception ex)
                {
                    //ignore 
                }
        }

        internal static byte[] ReadAll(Stream src)
        {
            byte[] buffer = new byte[Constants.DefaultBufferSize];
            int readOnce;
            using (MemoryStream dst = new MemoryStream())
            {
                while ((readOnce = src.Read(buffer, 0, buffer.Length)) > 0) dst.Write(buffer, 0, readOnce);

                return dst.GetBuffer();
            }
        }

        internal static string GetStreamString(Stream src)
        {
            using (StreamReader reader = new StreamReader(src, Encoding.UTF8))
            {
                string result = reader.ReadToEnd();

                return result;
            } 
        }
        
        internal static JObject TryParseJson(Stream src)
        {
            byte[] b = ReadAll(src);
            if (b == null || b.Length == 0) return null;

            return JObject.Parse(Encoding.UTF8.GetString(b));
        }

        internal static JObject ParseJson(Stream src)
        {
            using (JsonReader reader = new JsonTextReader(new StreamReader(src, Encoding.UTF8)))
            {
                JObject o = JObject.Load(reader);
                while (reader.Read())
                {
                    // Any content encountered here other than a comment will throw in the reader.
                }

                return o;
            }
        }

        internal static void SetHttpBasicHeader(IDictionary<string, string> header, IHttpBasicHeader input)
        {
            if (input.ContentLength >= 0) header[Constants.HeaderContentLength] = Convert.ToString(input.ContentLength);

            if (!string.IsNullOrEmpty(input.CacheControl)) header[Constants.HeaderCacheControl] = input.CacheControl;

            if (!string.IsNullOrEmpty(input.ContentType)) header[Constants.HeaderContentType] = input.ContentType;

            if (!string.IsNullOrEmpty(input.ContentLanguage))
                header[Constants.HeaderContentLanguage] = input.ContentLanguage;

            if (!string.IsNullOrEmpty(input.ContentEncoding))
                header[Constants.HeaderContentEncoding] = input.ContentEncoding;

            if (!string.IsNullOrEmpty(input.ContentDisposition))
                header[Constants.HeaderContentDisposition] = UrlEncodeChinese(input.ContentDisposition);

            if (input.Expires.HasValue)
                header[Constants.HeaderExpires] =
                    input.Expires?.ToUniversalTime()
                        .ToString(Constants.Rfc1123DateFormat, Constants.DefaultCultureInfo);
        }

        internal static void SetSseHeader(IDictionary<string, string> header, ISseHeader input)
        {
            if (!string.IsNullOrEmpty(input.SSECAlgorithm))
            {
                if (input.SSECAlgorithm != Constants.AlgorithmAes256)
                {
                    throw new TosClientException("invalid encryption-decryption algorithm");
                }

                if (!string.IsNullOrEmpty(input.ServerSideEncryption))
                {
                    throw new TosClientException("both ssec and server side encryption are set");
                }

                if (string.IsNullOrEmpty(input.SSECKey) || string.IsNullOrEmpty(input.SSECKeyMD5))
                {
                    throw new TosClientException("empty ssec key or ssec key md5");
                }

                header[Constants.HeaderSSECAlgorithm] = input.SSECAlgorithm;
                header[Constants.HeaderSSECKey] = input.SSECKey;
                header[Constants.HeaderSSECKeyMD5] = input.SSECKeyMD5;
            }
            else if (!string.IsNullOrEmpty(input.ServerSideEncryption))
            {
                if (input.ServerSideEncryption != Constants.AlgorithmAes256)
                {
                    throw new TosClientException("invalid encryption-decryption algorithm");
                }

                header[Constants.HeaderServerSideEncryption] = input.ServerSideEncryption;
            }
        }

        internal static void SetCopySourceHeader(IDictionary<string, string> header, ICopySourceHeader input)
        {
            Utils.CheckBucket(input.SrcBucket);
            Utils.CheckKey(input.SrcKey);
            string copySource = "/" + input.SrcBucket + "/" + UrlEncode(input.SrcKey, "/");
            if (!string.IsNullOrEmpty(input.SrcVersionID))
            {
                copySource += "?versionId=" + input.SrcVersionID;
            }

            header[Constants.HeaderCopySource] = copySource;
        }

        internal static void SetCopySourceSseHeader(IDictionary<string, string> header, ICopySourceSSeHeader input)
        {
            if (!string.IsNullOrEmpty(input.CopySourceSSECAlgorithm))
            {
                if (input.CopySourceSSECAlgorithm != Constants.AlgorithmAes256)
                {
                    throw new TosClientException("invalid copy source encryption-decryption algorithm");
                }

                if (string.IsNullOrEmpty(input.CopySourceSSECKey) || string.IsNullOrEmpty(input.CopySourceSSECKeyMD5))
                {
                    throw new TosClientException("empty copy source ssec key or ssec key md5");
                }

                header[Constants.HeaderCopySourceSSECAlgorithm] = input.CopySourceSSECAlgorithm;
                header[Constants.HeaderCopySourceSSECKey] = input.CopySourceSSECKey;
                header[Constants.HeaderCopySourceSSECKeyMD5] = input.CopySourceSSECKeyMD5;
            }
        }

        internal static void SetAclHeader(IDictionary<string, string> header, IAclHeader input)
        {
            if (input.ACL.HasValue)
            {
                header[Constants.HeaderAcl] = Enums.TransEnum(input.ACL.Value);
            }

            if (!string.IsNullOrEmpty(input.GrantFullControl))
            {
                header[Constants.HeaderGrantFullControl] = input.GrantFullControl;
            }

            if (!string.IsNullOrEmpty(input.GrantRead))
            {
                header[Constants.HeaderGrantRead] = input.GrantRead;
            }

            if (!string.IsNullOrEmpty(input.GrantReadAcp))
            {
                header[Constants.HeaderGrantReadAcp] = input.GrantReadAcp;
            }

            if (!string.IsNullOrEmpty(input.GrantWrite))
            {
                header[Constants.HeaderGrantWrite] = input.GrantWrite;
            }

            if (!string.IsNullOrEmpty(input.GrantWriteAcp))
            {
                header[Constants.HeaderGrantWriteAcp] = input.GrantWriteAcp;
            }
        }

        internal static void SetMetaHeader(IDictionary<string, string> header, IDictionary<string, string> meta)
        {
            if (meta?.Count > 0)
            {
                string key;
                foreach (KeyValuePair<string, string> entry in meta)
                {
                    if (string.IsNullOrEmpty(entry.Key))
                    {
                        continue;
                    }

                    key = UrlEncode(entry.Key);
                    if (!key.StartsWith(Constants.HeaderPrefixMeta))
                    {
                        key = Constants.HeaderPrefixMeta + key;
                    }

                    header[key] = UrlEncode(entry.Value);
                }
            }
        }

        internal static void SetMiscHeader(IDictionary<string, string> header, IMiscHeader input)
        {
            if (input.StorageClass.HasValue)
            {
                header[Constants.HeaderStorageClass] = Enums.TransEnum(input.StorageClass.Value);
            }

            if (!string.IsNullOrEmpty(input.WebsiteRedirectLocation))
            {
                header[Constants.HeaderWebsiteRedirectLocation] = input.WebsiteRedirectLocation;
            }
        }

        internal static void SetIfConditionHeader(IDictionary<string, string> header, IIfConditionHeader input)
        {
            if (!string.IsNullOrEmpty(input.IfMatch))
            {
                header[Constants.HeaderIfMatch] = input.IfMatch;
            }

            if (input.IfModifiedSince.HasValue)
            {
                header[Constants.HeaderIfModifiedSince] = input.IfModifiedSince?.ToUniversalTime()
                    .ToString(Constants.Rfc1123DateFormat, Constants.DefaultCultureInfo);
            }

            if (!string.IsNullOrEmpty(input.IfNoneMatch))
            {
                header[Constants.HeaderIfNoneMatch] = input.IfNoneMatch;
            }

            if (input.IfUnmodifiedSince.HasValue)
            {
                header[Constants.HeaderIfUnmodifiedSince] = input.IfUnmodifiedSince?.ToUniversalTime()
                    .ToString(Constants.Rfc1123DateFormat, Constants.DefaultCultureInfo);
            }
        }

        internal static void SetCopySourceIfConditionHeader(IDictionary<string, string> header,
            ICopySourceIfConditionHeader input)
        {
            if (!string.IsNullOrEmpty(input.CopySourceIfMatch))
            {
                header[Constants.HeaderCopySourceIfMatch] = input.CopySourceIfMatch;
            }

            if (input.CopySourceIfModifiedSince.HasValue)
            {
                header[Constants.HeaderCopySourceIfModifiedSince] = input.CopySourceIfModifiedSince?.ToUniversalTime()
                    .ToString(Constants.Rfc1123DateFormat, Constants.DefaultCultureInfo);
            }

            if (!string.IsNullOrEmpty(input.CopySourceIfNoneMatch))
            {
                header[Constants.HeaderCopySourceIfNoneMatch] = input.CopySourceIfNoneMatch;
            }

            if (input.CopySourceIfUnmodifiedSince.HasValue)
            {
                header[Constants.HeaderCopySourceIfUnmodifiedSince] = input.CopySourceIfUnmodifiedSince
                    ?.ToUniversalTime()
                    .ToString(Constants.Rfc1123DateFormat, Constants.DefaultCultureInfo);
            }
        }

        internal static IDictionary<string, string> ParseMeta(IDictionary<string, string> header)
        {
            IDictionary<string, string> meta = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (KeyValuePair<string, string> entry in header)
            {
                if (string.IsNullOrEmpty(entry.Key))
                {
                    continue;
                }

                if (entry.Key.StartsWith(Constants.HeaderPrefixMeta, StringComparison.OrdinalIgnoreCase))
                {
                    meta[UrlDecode(entry.Key)] = UrlDecode(entry.Value);
                }
            }

            return meta;
        }

        internal static void SetListCommonQuery(IDictionary<string, string> query, IListCommonQuery commonQuery)
        {
            if (!string.IsNullOrEmpty(commonQuery.Prefix))
            {
                query[Constants.QueryPrefix] = commonQuery.Prefix;
            }

            if (!string.IsNullOrEmpty(commonQuery.Delimiter))
            {
                query[Constants.QueryDelimiter] = commonQuery.Delimiter;
            }

            if (!string.IsNullOrEmpty(commonQuery.EncodingType))
            {
                query[Constants.QueryEncodingType] = commonQuery.EncodingType;
            }
        }

        internal static void SetUploadId(IDictionary<string, string> query, string uploadId)
        {
            if (string.IsNullOrEmpty(uploadId))
            {
                throw new TosClientException("empty upload id");
            }

            query[Constants.QueryUploadID] = uploadId;
        }

        internal static void SetMultipartUploadQuery(IDictionary<string, string> query,
            IMultipartUploadQuery multipartUploadQuery)
        {
            SetUploadId(query, multipartUploadQuery.UploadID);

            if (multipartUploadQuery.PartNumber < 0)
            {
                throw new TosClientException("invalid part number");
            }

            query[Constants.QueryPartNumber] = Convert.ToString(multipartUploadQuery.PartNumber);
        }


        internal static DateTime? ParseDateTime(string input, string format)
        {
            if (string.IsNullOrEmpty(input))
            {
                return null;
            }

            return DateTime.ParseExact(input, format, Constants.DefaultCultureInfo,
                DateTimeStyles.AssumeUniversal);
        }

        internal static Stream PrepareStream(IDictionary<string, string> header, string data)
        {
            if (data == null)
            {
                data = string.Empty;
            }

            return PrepareStream(header, Encoding.UTF8.GetBytes(data));
        }

        internal static Stream PrepareStream(IDictionary<string, string> header, byte[] data)
        {
            if (data == null)
            {
                data = new byte[0];
            }

            header[Constants.HeaderContentLength] = Convert.ToString(data.Length);
            return new MemoryStream(data);
        }

        internal static DateTime? ParseJTokenDate(JToken token, string format)
        {
            if (token == null)
            {
                return null;
            }

            if (token.Type == JTokenType.Date)
            {
                return token.Value<DateTime>();
            }

            return ParseDateTime(token.Value<string>(), format);
        }

        internal static string TransJTokenDate(JToken token, string format)
        {
            if (token == null)
            {
                return string.Empty;
            }

            if (token.Type == JTokenType.Date)
            {
                return token.Value<DateTime>().ToUniversalTime()
                    .ToString(format, Constants.DefaultCultureInfo);
            }

            return token.Value<string>();
        }

        internal static ListedCommonPrefix[] ParseCommonPrefixes(JObject json)
        {
            JArray temp = json["CommonPrefixes"] as JArray;
            if (temp == null)
            {
                return new ListedCommonPrefix[0];
            }

            ListedCommonPrefix[] result = new ListedCommonPrefix[temp.Count];
            for (int i = 0; i < temp.Count; i++)
            {
                result[i] = new ListedCommonPrefix()
                {
                    Prefix = temp[i]["Prefix"]?.Value<string>()
                };
            }

            return result;
        }

        internal static void CheckFilePath(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                throw new TosClientException("empty file path");
            }

            if (Directory.Exists(filePath))
            {
                throw new TosClientException("the specified file path is a dir");
            }

            if (!File.Exists(filePath))
            {
                throw new TosClientException("the specified file path does not exist");
            }
        }

        internal static void CreateDirectoryRecursively(string directoryPath)
        {
            string parent = Path.GetDirectoryName(directoryPath);
            if (!Directory.Exists(parent))
            {
                CreateDirectoryRecursively(parent);
            }

            Directory.CreateDirectory(directoryPath);
        }

        internal static void TrySetContentLength(IDictionary<string, string> header, Stream body)
        {
            if (body == null || header.ContainsKey(Constants.HeaderContentLength))
            {
                return;
            }

            if (body is MemoryStream || body is FileStream)
            {
                header[Constants.HeaderContentLength] = Convert.ToString(body.Length);
            }
        }
    }
}