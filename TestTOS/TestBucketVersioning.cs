using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using NUnit.Framework;
using TOS;
using TOS.Error;
using TOS.Model;

namespace TestTOS
{
    [TestFixture]
    public class TestBucketVersioning
    {
        [TestCase(VersioningStatusType.VersioningStatusEnabled, "Enabled")]
        [TestCase(VersioningStatusType.VersioningStatusSuspended, "Suspended")]
        public void TestVersioningRequestAndResponse(VersioningStatusType status, string wireValue)
        {
            object request = LifecycleTestSupport.Trans(new PutBucketVersioningInput { Bucket = "example-bucket", Status = status });
            Assert.That(LifecycleTestSupport.Get(request, "Operation"), Is.EqualTo("PutBucketVersioning"));
            Assert.That(LifecycleTestSupport.Get(request, "Method"), Is.EqualTo(HttpMethodType.HttpMethodPut));
            Assert.That(LifecycleTestSupport.Get(request, "Bucket"), Is.EqualTo("example-bucket"));
            Assert.That(LifecycleTestSupport.Dictionary(request, "Query")["versioning"], Is.Empty);
            string body = LifecycleTestSupport.Body(request);
            StringAssert.Contains("\"Status\": \"" + wireValue + "\"", body);
            byte[] bytes = Encoding.UTF8.GetBytes(body);
            using (MD5 md5 = MD5.Create())
            {
                Assert.That(LifecycleTestSupport.Dictionary(request, "Header")["Content-MD5"],
                    Is.EqualTo(Convert.ToBase64String(md5.ComputeHash(bytes))));
            }

            Assert.That(LifecycleTestSupport.Dictionary(request, "Header")["Content-Length"], Is.EqualTo(bytes.Length.ToString()));
            GetBucketVersioningOutput output = Parse(body);
            Assert.That(output.Status, Is.EqualTo(status));
            Assert.That(output.RequestID, Is.EqualTo("request-id"));
        }

        [Test]
        public void TestGetRequest()
        {
            object request = LifecycleTestSupport.Trans(new GetBucketVersioningInput { Bucket = "example-bucket" });
            Assert.That(LifecycleTestSupport.Get(request, "Operation"), Is.EqualTo("GetBucketVersioning"));
            Assert.That(LifecycleTestSupport.Get(request, "Method"), Is.EqualTo(HttpMethodType.HttpMethodGet));
            Assert.That(LifecycleTestSupport.Dictionary(request, "Query").Count, Is.EqualTo(1));
            Assert.That(LifecycleTestSupport.Dictionary(request, "Query")["versioning"], Is.Empty);
            Assert.That(LifecycleTestSupport.Get(request, "Body"), Is.Null);
        }

        [TestCase("{}")]
        [TestCase("{\"Status\":null}")]
        [TestCase("{\"Status\":\"\"}")]
        [TestCase("{\"Status\":\"FutureStatus\",\"Future\":true}")]
        public void TestUnversionedOrUnknownStatusIsNull(string body)
        {
            Assert.That(Parse(body).Status, Is.Null);
        }

        [Test]
        public void TestStatusValidation()
        {
            foreach (VersioningStatusType? status in new VersioningStatusType?[] { null, (VersioningStatusType)(-1), (VersioningStatusType)99 })
            {
                Assert.Throws<TosClientException>(() => LifecycleTestSupport.Trans(
                    new PutBucketVersioningInput { Bucket = "example-bucket", Status = status }));
            }
        }

        [Test]
        public void TestIndependentInterfaceAndLocalValidation()
        {
            Assert.That(typeof(ITosBucketVersioningClient).IsAssignableFrom(typeof(ITosClientV2)), Is.False);
            Assert.That(typeof(ITosClient).GetMethod("PutBucketVersioning"), Is.Null);
            Assert.That(typeof(ITosClient).GetMethod("GetBucketVersioning"), Is.Null);
            Assert.That(typeof(ITosClientV2).GetMethod("PutBucketVersioning"), Is.Null);
            Assert.That(typeof(ITosClientV2).GetMethod("GetBucketVersioning"), Is.Null);
            using (ITosBucketVersioningClient client = TosClientBuilder.Builder().SetAk("test-ak").SetSk("test-sk")
                .SetRegion("cn-beijing").SetEndpoint("http://tos.example.invalid").BuildBucketVersioning())
            {
                Assert.Throws<TosClientException>(() => client.PutBucketVersioning(null));
                Assert.Throws<TosClientException>(() => client.GetBucketVersioning(null));
                Assert.Throws<TosClientException>(() => client.PutBucketVersioning(new PutBucketVersioningInput
                {
                    Bucket = "Invalid_Bucket", Status = VersioningStatusType.VersioningStatusEnabled
                }));
                Assert.Throws<TosClientException>(() => client.GetBucketVersioning(new GetBucketVersioningInput()));
            }
        }

        private static GetBucketVersioningOutput Parse(string body)
        {
            return LifecycleTestSupport.Parse<GetBucketVersioningOutput>(new Dictionary<string, string>(), body, null);
        }
    }
}
