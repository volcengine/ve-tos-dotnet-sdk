using System;
using NUnit.Framework;
using TOS;
using TOS.Error;
using TOS.Model;

namespace TestTOS
{
    [TestFixture]
    public class TestLifecycleResponseErrors
    {
        [TestCase(typeof(HeadObjectOutput))]
        [TestCase(typeof(GetObjectOutput))]
        [TestCase(typeof(CopyObjectOutput))]
        [TestCase(typeof(CompleteMultipartUploadOutput))]
        public void TestLegacyOperationsKeepErrorDetails(Type outputType)
        {
            using (ITosClientV2 client = Client())
            {
                foreach (string body in new[] { "", "<html>not found</html>", "{",
                    "{\"Code\":\"NoSuchKey\",\"Message\":\"missing\",\"HostId\":\"host\",\"Resource\":\"key\"}" })
                {
                    TosServerException error = Assert.Throws<TosServerException>(() =>
                        LifecycleTestSupport.CheckResponse(client, 404, body, outputType));
                    Assert.That(error.StatusCode, Is.EqualTo(404));
                    Assert.That(error.RequestID, Is.EqualTo("request-id"));
                    Assert.That(error.ID2, Is.EqualTo("id-2"));
                    if (body.Contains("NoSuchKey"))
                    {
                        Assert.That(error.Code, Is.EqualTo("NoSuchKey"));
                        Assert.That(error.Message, Is.EqualTo("missing"));
                        Assert.That(error.HostID, Is.EqualTo("host"));
                        Assert.That(error.Resource, Is.EqualTo("key"));
                    }
                    else
                    {
                        Assert.That(error.Code, Is.Null);
                    }
                }
            }
        }

        [TestCase("")]
        [TestCase("<html>not found</html>")]
        [TestCase("{")]
        [TestCase("{\"Code\":\"NoSuchObjectSet\"}null")]
        [TestCase("null")]
        [TestCase("[]")]
        public void TestMalformedServerErrorRetainsStatusAndRequestID(string body)
        {
            using (ITosClientV2 client = Client())
            {
                TosServerException error = Assert.Throws<TosServerException>(() =>
                    LifecycleTestSupport.CheckResponse(client, 404, body, typeof(GetObjectSetOutput)));
                Assert.That(error.StatusCode, Is.EqualTo(404));
                Assert.That(error.RequestID, Is.EqualTo("request-id"));
                Assert.That(error.ID2, Is.EqualTo("id-2"));
                Assert.That(error.Code, Is.Null);
            }
        }

        [Test]
        public void TestValidServerErrorKeepsExistingFields()
        {
            using (ITosClientV2 client = Client())
            {
                TosServerException error = Assert.Throws<TosServerException>(() =>
                    LifecycleTestSupport.CheckResponse(client, 409,
                        "{\"Code\":\"Conflict\",\"Message\":\"exists\",\"HostId\":\"host\",\"Resource\":\"resource\"}",
                        typeof(PutObjectSetOutput)));
                Assert.That(error.StatusCode, Is.EqualTo(409));
                Assert.That(error.Code, Is.EqualTo("Conflict"));
                Assert.That(error.Message, Is.EqualTo("exists"));
                Assert.That(error.HostID, Is.EqualTo("host"));
                Assert.That(error.Resource, Is.EqualTo("resource"));
            }
        }

        [TestCase(typeof(PutObjectOutput))]
        [TestCase(typeof(CompleteMultipartUploadOutput))]
        public void TestCallbackFailureStillRejects203(Type outputType)
        {
            using (ITosClientV2 client = Client())
            {
                Assert.That(Assert.Throws<TosServerException>(() =>
                    LifecycleTestSupport.CheckResponse(client, 203, "", outputType)).StatusCode, Is.EqualTo(203));
            }
        }

        [TestCase(200)]
        [TestCase(203)]
        [TestCase(204)]
        public void TestSuccessResponseCheckDoesNotParseBody(int statusCode)
        {
            using (ITosClientV2 client = Client())
            {
                Assert.DoesNotThrow(() => LifecycleTestSupport.CheckResponse(client, statusCode,
                    "object bytes, not JSON", typeof(GetObjectOutput)));
            }
        }

        private static ITosClientV2 Client()
        {
            return TosClientBuilder.Builder().SetAk("test-ak").SetSk("test-sk").SetRegion("cn-beijing")
                .SetEndpoint("http://tos.example.invalid").BuildV2();
        }
    }
}
