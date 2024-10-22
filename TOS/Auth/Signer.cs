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
using TOS.Common;
using TOS.Config;
using TOS.Model;

namespace TOS.Auth
{
    internal class Signer
    {
        internal const string EmptyHashPayload = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855";
        internal const string UnsignedPayload = "UNSIGNED-PAYLOAD";
        internal const string Algorithm = "TOS4-HMAC-SHA256";
        internal const string Service = "tos";
        internal const string Request = "request";

        internal void SignHeader(HttpRequest request, Credential credential, RegionEndpoint regionEndpoint)
        {
            string longDate;
            string shortDate;
            string credentialScope;
            this.PrepareDateAndCredentialScope(out longDate, out shortDate, out credentialScope, regionEndpoint.Region);

            bool anonymous = credential.IsAnonymous;
            if (!anonymous && !string.IsNullOrEmpty(credential.SecurityToken))
            {
                request.Header[Constants.HeaderSecurityToken] = credential.SecurityToken;
            }

            request.Header[Constants.HeaderHost] = regionEndpoint.GetHost(request.Bucket);
            request.Header[Constants.HeaderRequestDate] = longDate;
            string signedHeaders;
            string canonicalHeaders = this.GetCanonicalHeaders(request, out signedHeaders, out _);

            string canonicalRequest = this.GetCanonicalRequest(request, canonicalHeaders, signedHeaders, false);
            if (anonymous)
            {
                return;
            }

            string stringToSign = this.GetStringToSign(canonicalRequest, longDate, credentialScope);
            string signature = this.GetSignature(stringToSign, shortDate, regionEndpoint.Region, credential.Sk);
            string authorization = Algorithm + " Credential=" + credential.Ak + "/" +
                                   credentialScope + ", SignedHeaders=" + signedHeaders +
                                   ", Signature=" + signature;
            request.Header[Constants.HeaderAuthorization] = authorization;
        }

        internal IDictionary<string, string> SignQuery(HttpRequest request, Credential credential,
            RegionEndpoint regionEndpoint, int expires, string domain, bool isCustomDomain)
        {
            IDictionary<string, string> signedHeader;
            if (credential.IsAnonymous)
            {
                signedHeader = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }
            else
            {
                string longDate;
                string shortDate;
                string credentialScope;
                this.PrepareDateAndCredentialScope(out longDate, out shortDate, out credentialScope,
                    regionEndpoint.Region);

                var bucket = request.Bucket;
                if (isCustomDomain)
                {
                    bucket = string.Empty;
                }
                request.Header[Constants.HeaderHost] = regionEndpoint.GetHost(bucket, domain);

                string signedHeaders;
                string canonicalHeaders = this.GetCanonicalHeaders(request, out signedHeaders, out signedHeader);
                request.Query[Constants.QueryAlgorithm] = Algorithm;
                request.Query[Constants.QueryCredential] = credential.Ak + "/" + credentialScope;
                request.Query[Constants.QueryDate] = longDate;
                request.Query[Constants.QueryExpires] = Convert.ToString(expires);
                request.Query[Constants.QuerySignedHeaders] = signedHeaders;
                if (!string.IsNullOrEmpty(credential.SecurityToken))
                {
                    request.Query[Constants.QuerySecurityToken] = credential.SecurityToken;
                }

                string canonicalRequest = this.GetCanonicalRequest(request, canonicalHeaders, signedHeaders, true);
                string stringToSign = this.GetStringToSign(canonicalRequest, longDate, credentialScope);
                string signature = this.GetSignature(stringToSign, shortDate, regionEndpoint.Region, credential.Sk);
                request.Query[Constants.QuerySignature] = Utils.UrlEncode(signature);
            }

            return signedHeader;
        }

        private void PrepareDateAndCredentialScope(out string longDate, out string shortDate,
            out string credentialScope, string region)
        {
            longDate = DateTime.UtcNow.ToString(Constants.LongDateFormat, Constants.DefaultCultureInfo);
            shortDate = longDate.Substring(0, 8);
            credentialScope = shortDate + "/" + region + "/" + Service + "/" + Request;
        }

        private string GetCanonicalRequest(HttpRequest request, string canonicalHeaders, string signedHeaders,
            bool isQuery)
        {
            string canonicalRequest = Enums.TransEnum(request.Method).ToUpper() + "\n" + "/";
            if (!string.IsNullOrEmpty(request.Key))
            {
                canonicalRequest += Utils.UrlEncode(request.Key, "/");
            }

            canonicalRequest += "\n";
            if (request.Query.Count > 0)
            {
                List<KeyValuePair<string, string>> kvlist = new List<KeyValuePair<string, string>>(request.Query);
                kvlist.Sort(delegate(KeyValuePair<string, string> x, KeyValuePair<string, string> y)
                {
                    return string.Compare(x.Key, y.Key, StringComparison.Ordinal);
                });
                string key;
                string value;
                foreach (KeyValuePair<string, string> kv in kvlist)
                {
                    key = Utils.UrlEncode(kv.Key);
                    value = Utils.UrlEncode(kv.Value);
                    request.Query[kv.Key] = value;
                    canonicalRequest += key + "=" + value + "&";
                }

                canonicalRequest = canonicalRequest.Substring(0, canonicalRequest.Length - 1);
            }

            canonicalRequest += "\n";

            canonicalRequest += canonicalHeaders;
            canonicalRequest += "\n";

            canonicalRequest += signedHeaders;
            canonicalRequest += "\n";
            if (isQuery)
            {
                canonicalRequest += UnsignedPayload;
            }
            else
            {
                canonicalRequest += EmptyHashPayload;
            }

            return canonicalRequest;
        }

        private string GetCanonicalHeaders(HttpRequest request, out string signedHeaders,
            out IDictionary<string, string> signedHeader)
        {
            signedHeaders = "";
            string canonicalHeaders = "";
            signedHeader = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (request.Header.Count > 0)
            {
                List<KeyValuePair<string, string>> kvlist = new List<KeyValuePair<string, string>>(request.Header);

                kvlist.Sort(delegate(KeyValuePair<string, string> x, KeyValuePair<string, string> y)
                {
                    return string.Compare(x.Key.ToLower(), y.Key.ToLower(), StringComparison.Ordinal);
                });
                string lowerKey;
                string trimValue;
                foreach (KeyValuePair<string, string> kv in kvlist)
                {
                    lowerKey = kv.Key.ToLower();
                    if (lowerKey != Constants.HeaderHostLower && lowerKey != Constants.HeaderContentTypeLower &&
                        !lowerKey.StartsWith(Constants.HeaderPrefix))
                    {
                        continue;
                    }

                    signedHeaders += lowerKey + ";";

                    if (string.IsNullOrEmpty(kv.Value))
                    {
                        trimValue = string.Empty;
                    }
                    else
                    {
                        trimValue = kv.Value.Trim();
                    }

                    signedHeader[kv.Key] = kv.Value;
                    canonicalHeaders += lowerKey + ":" + trimValue + "\n";
                }

                signedHeaders = signedHeaders.Substring(0, signedHeaders.Length - 1);
            }

            return canonicalHeaders;
        }

        private string GetStringToSign(string canonicalRequest, string longDate, string credentialScope)
        {
            return Algorithm + "\n" + longDate + "\n" + credentialScope + "\n" + Utils.HexSha256(canonicalRequest);
        }

        private string GetSignature(string stringToSign, string shortDate, string region, string sk)
        {
            return Utils.Hex(Utils.HmacSha256(Utils.HmacSha256(
                Utils.HmacSha256(Utils.HmacSha256(Utils.HmacSha256(sk, shortDate), region), Service),
                Request), stringToSign));
        }
    }
}