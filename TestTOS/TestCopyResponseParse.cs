using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using NUnit.Framework;
using TOS.Common;
using TOS.Error;
using TOS.Model;

namespace TestTOS
{
    [TestFixture]
    public class TestCopyResponseParse
    {
        [Test]
        public void TestCopyObjectRejectsMissingOrEmptyETag()
        {
            var ex = Assert.Throws<TosServerException>(() => ParseCopyObject(
                "{\"Code\":\"InternalError\",\"Message\":\"copy failed\",\"HostId\":\"host\",\"Resource\":\"res\"}"));
            Assert.That(ex.StatusCode, Is.EqualTo(200));
            Assert.That(ex.Code, Is.EqualTo("InternalError"));
            Assert.That(ex.Message, Is.EqualTo("copy failed"));

            ex = Assert.Throws<TosServerException>(() => ParseCopyObject(
                "{\"ETag\":null,\"LastModified\":\"2023-01-01T00:00:00Z\"}"));
            Assert.That(ex.Message, Is.EqualTo("missing ETag in copy object response"));

            ex = Assert.Throws<TosServerException>(() => ParseCopyObject(
                "{\"ETag\":\"\",\"LastModified\":\"2023-01-01T00:00:00Z\"}"));
            Assert.That(ex.Message, Is.EqualTo("missing ETag in copy object response"));
        }

        [Test]
        public void TestUploadPartCopyRejectsMissingOrEmptyETag()
        {
            var ex = Assert.Throws<TosServerException>(() => ParseUploadPartCopy(
                "{\"Code\":\"InternalError\",\"Message\":\"copy part failed\",\"HostId\":\"host\",\"Resource\":\"res\"}"));
            Assert.That(ex.StatusCode, Is.EqualTo(200));
            Assert.That(ex.Code, Is.EqualTo("InternalError"));
            Assert.That(ex.Message, Is.EqualTo("copy part failed"));

            ex = Assert.Throws<TosServerException>(() => ParseUploadPartCopy(
                "{\"ETag\":null,\"LastModified\":\"2023-01-01T00:00:00Z\"}"));
            Assert.That(ex.Message, Is.EqualTo("missing ETag in upload part copy response"));

            ex = Assert.Throws<TosServerException>(() => ParseUploadPartCopy(
                "{\"ETag\":\"\",\"LastModified\":\"2023-01-01T00:00:00Z\"}"));
            Assert.That(ex.Message, Is.EqualTo("missing ETag in upload part copy response"));
        }

        [Test]
        public void TestCopyResponsesAcceptValidETag()
        {
            var copyObjectOutput = ParseCopyObject(
                "{\"ETag\":\"etag-copy\",\"LastModified\":\"2023-01-01T00:00:00Z\"}");
            Assert.That(copyObjectOutput.ETag, Is.EqualTo("etag-copy"));

            var uploadPartCopyOutput = ParseUploadPartCopy(
                "{\"ETag\":\"etag-part\",\"LastModified\":\"2023-01-01T00:00:00Z\"}");
            Assert.That(uploadPartCopyOutput.ETag, Is.EqualTo("etag-part"));
            Assert.That(uploadPartCopyOutput.PartNumber, Is.EqualTo(1));
        }

        private static CopyObjectOutput ParseCopyObject(string responseBody)
        {
            return ParseOutput<CopyObjectOutput>(responseBody, null);
        }

        private static UploadPartCopyOutput ParseUploadPartCopy(string responseBody)
        {
            return ParseOutput<UploadPartCopyOutput>(responseBody,
                new Dictionary<string, string> { { "partNumber", "1" } });
        }

        private static T ParseOutput<T>(string responseBody, IDictionary<string, string> query)
            where T : GenericOutput, new()
        {
            Type requestType = typeof(GenericOutput).Assembly.GetType("TOS.Common.HttpRequest");
            Type responseType = typeof(GenericOutput).Assembly.GetType("TOS.Common.HttpResponse");
            object request = CreateInstance(requestType, Type.EmptyTypes, new object[0]);
            object response = CreateInstance(responseType, new[] { typeof(int) }, new object[] { 200 });

            if (query != null)
            {
                IDictionary<string, string> requestQuery =
                    (IDictionary<string, string>)GetPropertyValue(request, "Query");
                foreach (KeyValuePair<string, string> entry in query)
                {
                    requestQuery[entry.Key] = entry.Value;
                }
            }

            SetPropertyValue(response, "Body", new MemoryStream(Encoding.UTF8.GetBytes(responseBody)));

            RequestInfo requestInfo = new RequestInfo();
            SetPropertyValue(requestInfo, "StatusCode", 200);
            SetPropertyValue(requestInfo, "RequestID", "request-id");
            SetPropertyValue(requestInfo, "ID2", "id-2");
            SetPropertyValue(requestInfo, "Header", new Dictionary<string, string>());

            T output = new T();
            MethodInfo parseMethod = typeof(GenericOutput).GetMethod("Parse",
                BindingFlags.Instance | BindingFlags.NonPublic,
                null,
                new[] { requestType, responseType, typeof(RequestInfo) },
                null);
            Assert.NotNull(parseMethod);

            try
            {
                parseMethod.Invoke(output, new[] { request, response, requestInfo });
            }
            catch (TargetInvocationException ex)
            {
                if (ex.InnerException != null)
                {
                    throw ex.InnerException;
                }

                throw;
            }

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
    }
}
