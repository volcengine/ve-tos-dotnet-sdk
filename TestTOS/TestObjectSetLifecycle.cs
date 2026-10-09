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
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using NUnit.Framework;
using TOS;
using TOS.Common;
using TOS.Error;
using TOS.Model;

namespace TestTOS
{
    [TestFixture]
    public class TestObjectSetLifecycle
    {
        [Test]
        public void TestPutObjectSetLifecycleRequest()
        {
            PutObjectSetLifecycleInput input = new PutObjectSetLifecycleInput
            {
                Bucket = "example-bucket",
                ObjectSetName = "life/cycle/set",
                Rules = new[]
                {
                    new LifecycleRule
                    {
                        ID = "rule-1",
                        Prefix = "life/cycle/set/",
                        Status = StatusType.StatusEnabled,
                        Transitions = new[]
                        {
                            new Transition
                            {
                                Days = 30,
                                StorageClass = StorageClassType.StorageClassIa
                            }
                        },
                        Expiration = new Expiration { Days = 365 },
                        Tags = new[] { new Tag { Key = "env", Value = "prod" } }
                    }
                }
            };

            object request = Trans(input);
            Assert.That(GetPropertyValue(request, "Operation"), Is.EqualTo("PutObjectSetLifecycle"));
            Assert.That(GetPropertyValue(request, "Method"), Is.EqualTo(HttpMethodType.HttpMethodPut));
            Assert.That(GetPropertyValue(request, "Bucket"), Is.EqualTo("example-bucket"));

            IDictionary<string, string> query = GetStringDictionary(request, "Query");
            Assert.That(query.Count, Is.EqualTo(2));
            Assert.That(query.ContainsKey("objectset-lifecycle"), Is.True);
            Assert.That(query["objectset-lifecycle"], Is.EqualTo(string.Empty));
            Assert.That(query["ObjectSetName"], Is.EqualTo("life/cycle/set"));

            IDictionary<string, string> header = GetStringDictionary(request, "Header");
            string body = ReadBody(request);
            Assert.That(header["Content-Length"], Is.EqualTo(Encoding.UTF8.GetByteCount(body).ToString()));
            Assert.That(header["Content-MD5"], Is.EqualTo(Base64Md5(Encoding.UTF8.GetBytes(body))));
            StringAssert.Contains("\"Rules\"", body);
            StringAssert.Contains("\"ID\": \"rule-1\"", body);
            StringAssert.Contains("\"Expiration\"", body);
        }

        [Test]
        public void TestObjectSetLifecyclePreservesRawObjectSetName()
        {
            const string objectSetName = "tenant/a b+c%25/中文";
            object request = Trans(new GetObjectSetLifecycleInput
            {
                Bucket = "example-bucket",
                ObjectSetName = objectSetName
            });

            IDictionary<string, string> query = GetStringDictionary(request, "Query");
            Assert.That(query["ObjectSetName"], Is.EqualTo(objectSetName));
        }

        [Test]
        public void TestGetAndDeleteObjectSetLifecycleRequests()
        {
            object getRequest = Trans(new GetObjectSetLifecycleInput
            {
                Bucket = "example-bucket",
                ObjectSetName = "life/cycle/set"
            });
            AssertRequest(getRequest, "GetObjectSetLifecycle", HttpMethodType.HttpMethodGet);

            object deleteRequest = Trans(new DeleteObjectSetLifecycleInput
            {
                Bucket = "example-bucket",
                ObjectSetName = "life/cycle/set"
            });
            AssertRequest(deleteRequest, "DeleteObjectSetLifecycle", HttpMethodType.HttpMethodDelete);
        }

        [Test]
        public void TestGetObjectSetLifecycleResponse()
        {
            const string body = "{\"Rules\":[{" +
                                "\"ID\":\"rule-1\",\"Prefix\":\"life/cycle/set/\"," +
                                "\"Status\":\"Enabled\"," +
                                "\"Transitions\":[{\"Days\":30,\"StorageClass\":\"IA\"}]," +
                                "\"Expiration\":{\"Days\":365}," +
                                "\"Tags\":[{\"Key\":\"env\",\"Value\":\"prod\"}]}]}";

            GetObjectSetLifecycleOutput output = ParseGetOutput(body);
            Assert.That(output.StatusCode, Is.EqualTo(200));
            Assert.That(output.RequestID, Is.EqualTo("request-id"));
            Assert.That(output.Rules.Length, Is.EqualTo(1));

            LifecycleRule rule = output.Rules[0];
            Assert.That(rule.ID, Is.EqualTo("rule-1"));
            Assert.That(rule.Prefix, Is.EqualTo("life/cycle/set/"));
            Assert.That(rule.Status, Is.EqualTo(StatusType.StatusEnabled));
            Assert.That(rule.Transitions[0].Days, Is.EqualTo(30));
            Assert.That(rule.Transitions[0].StorageClass, Is.EqualTo(StorageClassType.StorageClassIa));
            Assert.That(rule.Expiration.Days, Is.EqualTo(365));
            Assert.That(rule.Tags[0].Value, Is.EqualTo("prod"));
        }

        [Test]
        public void TestGetObjectSetLifecycleResponseDefaults()
        {
            GetObjectSetLifecycleOutput output = ParseGetOutput("{}");
            Assert.That(output.Rules, Is.Empty);

            output = ParseGetOutput("{\"Rules\":[null]}");
            Assert.That(output.Rules.Length, Is.EqualTo(1));
            Assert.That(output.Rules[0].Transitions, Is.Empty);
            Assert.That(output.Rules[0].NoncurrentVersionTransitions, Is.Empty);
            Assert.That(output.Rules[0].Tags, Is.Empty);
        }

        [Test]
        public void TestObjectSetLifecycleRejectsMissingObjectSetName()
        {
            AssertMissingObjectSetName(new PutObjectSetLifecycleInput
            {
                Bucket = "example-bucket",
                Rules = new[]
                {
                    new LifecycleRule
                    {
                        Status = StatusType.StatusEnabled,
                        Expiration = new Expiration { Days = 7 }
                    }
                }
            });
            AssertMissingObjectSetName(new GetObjectSetLifecycleInput { Bucket = "example-bucket" });
            AssertMissingObjectSetName(new DeleteObjectSetLifecycleInput { Bucket = "example-bucket" });
        }

        [Test]
        public void TestPutObjectSetLifecycleReusesLifecycleValidation()
        {
            PutObjectSetLifecycleInput input = new PutObjectSetLifecycleInput
            {
                Bucket = "example-bucket",
                ObjectSetName = "life/cycle/set",
                Rules = new LifecycleRule[0]
            };

            TosClientException exception = Assert.Throws<TosClientException>(() => Trans(input));
            StringAssert.Contains("empty rules", exception.Message);

            input.Rules = new[]
            {
                new LifecycleRule
                {
                    Status = StatusType.StatusEnabled,
                    Expiration = new Expiration { Days = 0 }
                }
            };
            exception = Assert.Throws<TosClientException>(() => Trans(input));
            StringAssert.Contains("invalid days", exception.Message);
        }

        [Test]
        public void TestObjectSetLifecycleInterfaceIsAdditive()
        {
            Assert.That(typeof(ITosClient).GetMethod("PutObjectSetLifecycle"), Is.Null);
            Assert.That(typeof(ITosLifecycleClient).GetMethod("PutObjectSetLifecycle"), Is.Null);
            Assert.That(typeof(ITosClient).IsAssignableFrom(typeof(ITosClientV2)), Is.True);
            Assert.That(typeof(ITosLifecycleClient).IsAssignableFrom(typeof(ITosClientV2)), Is.True);
            Assert.That(typeof(ITosObjectSetLifecycleClient).IsAssignableFrom(typeof(ITosClientV2)), Is.True);
            Assert.That(typeof(IDisposable).IsAssignableFrom(typeof(ITosObjectSetLifecycleClient)), Is.True);
            Assert.That(typeof(ITosObjectSetLifecycleClient).GetMethod("PutObjectSetLifecycle"), Is.Not.Null);
            Assert.That(typeof(ITosObjectSetLifecycleClient).GetMethod("GetObjectSetLifecycle"), Is.Not.Null);
            Assert.That(typeof(ITosObjectSetLifecycleClient).GetMethod("DeleteObjectSetLifecycle"), Is.Not.Null);
        }

        private static void AssertRequest(object request, string operation, HttpMethodType method)
        {
            Assert.That(GetPropertyValue(request, "Operation"), Is.EqualTo(operation));
            Assert.That(GetPropertyValue(request, "Method"), Is.EqualTo(method));
            Assert.That(GetPropertyValue(request, "Bucket"), Is.EqualTo("example-bucket"));
            Assert.That(GetPropertyValue(request, "Body"), Is.Null);

            IDictionary<string, string> query = GetStringDictionary(request, "Query");
            Assert.That(query.Count, Is.EqualTo(2));
            Assert.That(query.ContainsKey("objectset-lifecycle"), Is.True);
            Assert.That(query["ObjectSetName"], Is.EqualTo("life/cycle/set"));
        }

        private static void AssertMissingObjectSetName(GenericInput input)
        {
            TosClientException exception = Assert.Throws<TosClientException>(() => Trans(input));
            StringAssert.Contains("empty object set name", exception.Message);
        }

        private static object Trans(GenericInput input)
        {
            MethodInfo transMethod = input.GetType().GetMethod("Trans",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(transMethod);
            try
            {
                return transMethod.Invoke(input, null);
            }
            catch (TargetInvocationException ex)
            {
                if (ex.InnerException != null)
                {
                    throw ex.InnerException;
                }

                throw;
            }
        }

        private static GetObjectSetLifecycleOutput ParseGetOutput(string responseBody)
        {
            Type requestType = typeof(GenericOutput).Assembly.GetType("TOS.Common.HttpRequest");
            Type responseType = typeof(GenericOutput).Assembly.GetType("TOS.Common.HttpResponse");
            object request = CreateInstance(requestType, Type.EmptyTypes, new object[0]);
            object response = CreateInstance(responseType, new[] { typeof(int) }, new object[] { 200 });
            SetPropertyValue(response, "Body", new MemoryStream(Encoding.UTF8.GetBytes(responseBody)));
            SetPropertyValue(response, "Header",
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));

            RequestInfo requestInfo = new RequestInfo();
            SetPropertyValue(requestInfo, "StatusCode", 200);
            SetPropertyValue(requestInfo, "RequestID", "request-id");
            SetPropertyValue(requestInfo, "ID2", "id-2");
            SetPropertyValue(requestInfo, "Header", new Dictionary<string, string>());

            GetObjectSetLifecycleOutput output = new GetObjectSetLifecycleOutput();
            MethodInfo parseMethod = typeof(GenericOutput).GetMethod("Parse",
                BindingFlags.Instance | BindingFlags.NonPublic,
                null,
                new[] { requestType, responseType, typeof(RequestInfo) },
                null);
            Assert.NotNull(parseMethod);
            parseMethod.Invoke(output, new[] { request, response, requestInfo });
            return output;
        }

        private static object CreateInstance(Type type, Type[] parameterTypes, object[] args)
        {
            Assert.NotNull(type);
            ConstructorInfo constructor = type.GetConstructor(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                parameterTypes,
                null);
            Assert.NotNull(constructor);
            return constructor.Invoke(args);
        }

        private static IDictionary<string, string> GetStringDictionary(object instance, string name)
        {
            return (IDictionary<string, string>)GetPropertyValue(instance, name);
        }

        private static string ReadBody(object request)
        {
            Stream body = (Stream)GetPropertyValue(request, "Body");
            body.Position = 0;
            return new StreamReader(body, Encoding.UTF8).ReadToEnd();
        }

        private static object GetPropertyValue(object instance, string name)
        {
            PropertyInfo property = GetProperty(instance, name);
            MethodInfo getter = property.GetGetMethod(true);
            Assert.NotNull(getter);
            return getter.Invoke(instance, null);
        }

        private static void SetPropertyValue(object instance, string name, object value)
        {
            PropertyInfo property = GetProperty(instance, name);
            MethodInfo setter = property.GetSetMethod(true);
            Assert.NotNull(setter);
            setter.Invoke(instance, new[] { value });
        }

        private static PropertyInfo GetProperty(object instance, string name)
        {
            PropertyInfo property = instance.GetType().GetProperty(name,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.NotNull(property);
            return property;
        }

        private static string Base64Md5(byte[] input)
        {
            return Convert.ToBase64String(MD5.Create().ComputeHash(input));
        }
    }
}
