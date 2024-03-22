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
using System.Reflection;
using Newtonsoft.Json.Linq;
using TOS.Auth;
using TOS.Common;
using TOS.Config;
using TOS.Error;
using TOS.Model;

namespace TOS
{
    internal partial class TosClient : ITosClient
    {
        public const string Version = Constants.Version;

        private HttpClient _httpClient;

        internal TosClient(ConfigHolder configHolder)
        {
            _httpClient = new HttpClient(new Signer(), configHolder);
        }

        private K DoRequest<T, K>(T input)
            where T : GenericInput
            where K : GenericOutput
        {
            HttpRequest request = null;
            HttpResponse response = null;
            try
            {
                this.CheckBucketAndKey(input);
                request = input.Trans();
                ConstructorInfo constructor = typeof(K).GetConstructor(new Type[] { });
                K output = constructor?.Invoke(null) as K;
                if (output == null)
                {
                    throw new TosClientException("cannot create output");
                }

                response = this._httpClient.DoRequest(request);
                RequestInfo requestInfo = this.CheckResponse(response, output.GetType());
                output.Parse(request, response, requestInfo);
                return output;
            }
            catch (TosException ex)
            {
                throw ex;
            }
            catch (Exception ex)
            {
                throw new TosClientException(ex.Message, ex);
            }
            finally
            {
                Utils.CloseIDisposable(request);
                Utils.CloseIDisposable(response);
            }
        }

        private RequestInfo CheckResponse(HttpResponse response, Type outputType)
        {
            RequestInfo requestInfo = new RequestInfo
            {
                StatusCode = response.StatusCode,
                Header = response.Header
            };

            string temp;
            response.Header.TryGetValue(Constants.HeaderRequestID, out temp);
            requestInfo.RequestID = temp;

            response.Header.TryGetValue(Constants.HeaderID2, out temp);
            requestInfo.ID2 = temp;
            if (response.StatusCode >= 300 || (response.StatusCode == 203 && (outputType == typeof(PutObjectOutput) || outputType == typeof(CompleteMultipartUploadOutput))))
            {
                JObject json = Utils.TryParseJson(response.Body);
                if (json != null)
                {
                    throw new TosServerException(json["Message"]?.Value<string>(), requestInfo)
                    {
                        Code = json["Code"]?.Value<string>(),
                        HostID = json["HostId"]?.Value<string>(),
                        Resource = json["Resource"]?.Value<string>()
                    };
                }

                throw new TosServerException("unexpected status code: " + response.StatusCode, requestInfo);
            }

            return requestInfo;
        }


        private void CheckBucketAndKey<T>(T input)
        {
            if (input == null)
            {
                throw new TosClientException("invalid input");
            }

            if (input is GenericObjectInput)
            {
                GenericObjectInput objectInput = input as GenericObjectInput;
                Utils.CheckBucket(objectInput.Bucket);
                Utils.CheckKey(objectInput.Key);
            }
            else if (input is GenericBucketInput)
            {
                Utils.CheckBucket((input as GenericBucketInput).Bucket);
            }
        }
    }
}