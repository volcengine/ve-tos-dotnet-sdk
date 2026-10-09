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
    public class TestObjectSetLifecycleByTag
    {
        [Test]
        public void TestPutObjectSetLifecycleByTagRequest()
        {
            PutObjectSetLifecycleByTagInput input = new PutObjectSetLifecycleByTagInput
            {
                Bucket = "example-bucket",
                ObjectSetTagRules = new[]
                {
                    CreateTagRule("env", "prod", "prod-rule", "prod/", 30),
                    CreateTagRule("env", "test", "test-rule", "test/", 7)
                }
            };

            object request = Trans(input);
            Assert.That(GetPropertyValue(request, "Operation"), Is.EqualTo("PutObjectSetLifecycleByTag"));
            Assert.That(GetPropertyValue(request, "Method"), Is.EqualTo(HttpMethodType.HttpMethodPut));
            Assert.That(GetPropertyValue(request, "Bucket"), Is.EqualTo("example-bucket"));

            IDictionary<string, string> query = GetStringDictionary(request, "Query");
            Assert.That(query.Count, Is.EqualTo(1));
            Assert.That(query.ContainsKey("objectset-lifecycle-bytag"), Is.True);
            Assert.That(query["objectset-lifecycle-bytag"], Is.EqualTo(string.Empty));

            IDictionary<string, string> header = GetStringDictionary(request, "Header");
            string body = ReadBody(request);
            Assert.That(header["Content-Length"], Is.EqualTo(Encoding.UTF8.GetByteCount(body).ToString()));
            Assert.That(header["Content-MD5"], Is.EqualTo(Base64Md5(Encoding.UTF8.GetBytes(body))));
            StringAssert.Contains("\"ObjectSetTagRules\"", body);
            StringAssert.Contains("\"Key\": \"env\"", body);
            StringAssert.Contains("\"Value\": \"prod\"", body);
            StringAssert.Contains("\"ID\": \"test-rule\"", body);
        }

        [Test]
        public void TestGetAndDeleteObjectSetLifecycleByTagRequests()
        {
            object getRequest = Trans(new GetObjectSetLifecycleByTagInput { Bucket = "example-bucket" });
            AssertRequest(getRequest, "GetObjectSetLifecycleByTag", HttpMethodType.HttpMethodGet);

            object deleteRequest = Trans(new DeleteObjectSetLifecycleByTagInput { Bucket = "example-bucket" });
            AssertRequest(deleteRequest, "DeleteObjectSetLifecycleByTag", HttpMethodType.HttpMethodDelete);
        }

        [Test]
        public void TestObjectSetLifecycleByTagRoundTrip()
        {
            PutObjectSetLifecycleByTagInput input = new PutObjectSetLifecycleByTagInput
            {
                Bucket = "example-bucket",
                ObjectSetTagRules = new[]
                {
                    new ObjectSetTagLifecycleRule
                    {
                        Tag = new Tag { Key = "department", Value = "finance" },
                        Rules = new[]
                        {
                            new LifecycleRule
                            {
                                ID = "archive-rule",
                                Prefix = "finance/",
                                Status = StatusType.StatusEnabled,
                                Transitions = new[]
                                {
                                    new Transition
                                    {
                                        Days = 30,
                                        StorageClass = StorageClassType.StorageClassIa
                                    }
                                },
                                Expiration = new Expiration { Days = 365 }
                            }
                        }
                    }
                }
            };

            GetObjectSetLifecycleByTagOutput output = ParseGetOutput(ReadBody(Trans(input)));
            Assert.That(output.ObjectSetTagRules.Length, Is.EqualTo(1));
            Assert.That(output.ObjectSetTagRules[0].Tag.Key, Is.EqualTo("department"));
            Assert.That(output.ObjectSetTagRules[0].Tag.Value, Is.EqualTo("finance"));
            Assert.That(output.ObjectSetTagRules[0].Rules.Length, Is.EqualTo(1));
            Assert.That(output.ObjectSetTagRules[0].Rules[0].ID, Is.EqualTo("archive-rule"));
            Assert.That(output.ObjectSetTagRules[0].Rules[0].Transitions[0].Days, Is.EqualTo(30));
            Assert.That(output.ObjectSetTagRules[0].Rules[0].Expiration.Days, Is.EqualTo(365));
        }

        [Test]
        public void TestGetObjectSetLifecycleByTagResponse()
        {
            const string body = "{\"ObjectSetTagRules\":[{" +
                                "\"Tag\":{\"Key\":\"env\",\"Value\":\"prod\"}," +
                                "\"Rules\":[{\"ID\":\"rule-1\",\"Prefix\":\"prod/\"," +
                                "\"Status\":\"Enabled\",\"Expiration\":{\"Days\":7}}]}," +
                                "{\"Tag\":{\"Key\":\"env\",\"Value\":\"test\"}," +
                                "\"Rules\":[{\"ID\":\"rule-2\",\"Status\":\"Disabled\"}]}]}";

            GetObjectSetLifecycleByTagOutput output = ParseGetOutput(body);
            Assert.That(output.StatusCode, Is.EqualTo(200));
            Assert.That(output.RequestID, Is.EqualTo("request-id"));
            Assert.That(output.ObjectSetTagRules.Length, Is.EqualTo(2));
            Assert.That(output.ObjectSetTagRules[0].Tag.Value, Is.EqualTo("prod"));
            Assert.That(output.ObjectSetTagRules[0].Rules[0].Expiration.Days, Is.EqualTo(7));
            Assert.That(output.ObjectSetTagRules[1].Tag.Value, Is.EqualTo("test"));
            Assert.That(output.ObjectSetTagRules[1].Rules[0].Status, Is.EqualTo(StatusType.StatusDisabled));
        }

        [Test]
        public void TestGetObjectSetLifecycleByTagResponseDefaults()
        {
            GetObjectSetLifecycleByTagOutput output = ParseGetOutput("{}");
            Assert.That(output.ObjectSetTagRules, Is.Empty);

            output = ParseGetOutput("{\"ObjectSetTagRules\":[null,{\"Rules\":[]}]}");
            Assert.That(output.ObjectSetTagRules.Length, Is.EqualTo(2));
            Assert.That(output.ObjectSetTagRules[0].Tag, Is.Null);
            Assert.That(output.ObjectSetTagRules[0].Rules, Is.Empty);
            Assert.That(output.ObjectSetTagRules[1].Tag, Is.Null);
            Assert.That(output.ObjectSetTagRules[1].Rules, Is.Empty);
        }

        [Test]
        public void TestPutObjectSetLifecycleByTagRejectsEmptyGroups()
        {
            AssertValidation(null, "empty object set tag rules");
            AssertValidation(new ObjectSetTagLifecycleRule[0], "empty object set tag rules");
            AssertValidation(new ObjectSetTagLifecycleRule[] { null }, "null object set tag rule");
        }

        [Test]
        public void TestPutObjectSetLifecycleByTagRejectsInvalidTag()
        {
            AssertValidation(new[]
            {
                new ObjectSetTagLifecycleRule
                {
                    Rules = new[] { EnabledExpirationRule() }
                }
            }, "invalid tag");
            AssertValidation(new[]
            {
                new ObjectSetTagLifecycleRule
                {
                    Tag = new Tag { Key = string.Empty, Value = "prod" },
                    Rules = new[] { EnabledExpirationRule() }
                }
            }, "invalid tag");
            AssertValidation(new[]
            {
                new ObjectSetTagLifecycleRule
                {
                    Tag = new Tag { Key = "env", Value = string.Empty },
                    Rules = new[] { EnabledExpirationRule() }
                }
            }, "invalid tag");
        }

        [Test]
        public void TestPutObjectSetLifecycleByTagReusesLifecycleValidation()
        {
            AssertValidation(new[]
            {
                new ObjectSetTagLifecycleRule
                {
                    Tag = new Tag { Key = "env", Value = "prod" },
                    Rules = new LifecycleRule[0]
                }
            }, "empty rules");
            AssertValidation(new[]
            {
                new ObjectSetTagLifecycleRule
                {
                    Tag = new Tag { Key = "env", Value = "prod" },
                    Rules = new[]
                    {
                        new LifecycleRule
                        {
                            Status = StatusType.StatusEnabled,
                            Expiration = new Expiration { Days = 0 }
                        }
                    }
                }
            }, "invalid days");
        }

        [Test]
        public void TestObjectSetLifecycleByTagInterfaceIsAdditive()
        {
            Assert.That(typeof(ITosClient).GetMethod("PutObjectSetLifecycleByTag"), Is.Null);
            Assert.That(typeof(ITosLifecycleClient).GetMethod("PutObjectSetLifecycleByTag"), Is.Null);
            Assert.That(typeof(ITosObjectSetLifecycleClient).GetMethod("PutObjectSetLifecycleByTag"), Is.Null);
            Assert.That(typeof(ITosObjectSetLifecycleByTagClient).IsAssignableFrom(typeof(ITosClientV2)), Is.True);
            Assert.That(typeof(IDisposable).IsAssignableFrom(typeof(ITosObjectSetLifecycleByTagClient)), Is.True);
            Assert.That(typeof(ITosObjectSetLifecycleByTagClient).GetMethod("PutObjectSetLifecycleByTag"),
                Is.Not.Null);
            Assert.That(typeof(ITosObjectSetLifecycleByTagClient).GetMethod("GetObjectSetLifecycleByTag"),
                Is.Not.Null);
            Assert.That(typeof(ITosObjectSetLifecycleByTagClient).GetMethod("DeleteObjectSetLifecycleByTag"),
                Is.Not.Null);
        }

        private static ObjectSetTagLifecycleRule CreateTagRule(string key, string value, string id,
            string prefix, int days)
        {
            return new ObjectSetTagLifecycleRule
            {
                Tag = new Tag { Key = key, Value = value },
                Rules = new[]
                {
                    new LifecycleRule
                    {
                        ID = id,
                        Prefix = prefix,
                        Status = StatusType.StatusEnabled,
                        Expiration = new Expiration { Days = days }
                    }
                }
            };
        }

        private static LifecycleRule EnabledExpirationRule()
        {
            return new LifecycleRule
            {
                Status = StatusType.StatusEnabled,
                Expiration = new Expiration { Days = 7 }
            };
        }

        private static void AssertRequest(object request, string operation, HttpMethodType method)
        {
            Assert.That(GetPropertyValue(request, "Operation"), Is.EqualTo(operation));
            Assert.That(GetPropertyValue(request, "Method"), Is.EqualTo(method));
            Assert.That(GetPropertyValue(request, "Bucket"), Is.EqualTo("example-bucket"));
            Assert.That(GetPropertyValue(request, "Body"), Is.Null);

            IDictionary<string, string> query = GetStringDictionary(request, "Query");
            Assert.That(query.Count, Is.EqualTo(1));
            Assert.That(query.ContainsKey("objectset-lifecycle-bytag"), Is.True);
        }

        private static void AssertValidation(ObjectSetTagLifecycleRule[] tagRules, string expectedMessage)
        {
            PutObjectSetLifecycleByTagInput input = new PutObjectSetLifecycleByTagInput
            {
                Bucket = "example-bucket",
                ObjectSetTagRules = tagRules
            };
            TosClientException exception = Assert.Throws<TosClientException>(() => Trans(input));
            StringAssert.Contains(expectedMessage, exception.Message);
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

        private static GetObjectSetLifecycleByTagOutput ParseGetOutput(string responseBody)
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

            GetObjectSetLifecycleByTagOutput output = new GetObjectSetLifecycleByTagOutput();
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
