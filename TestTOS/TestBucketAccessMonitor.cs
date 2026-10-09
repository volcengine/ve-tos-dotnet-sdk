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
    public class TestBucketAccessMonitor
    {
        [TestCase(StatusType.StatusEnabled, "Enabled")]
        [TestCase(StatusType.StatusDisabled, "Disabled")]
        public void TestPutRequest(StatusType status, string wireValue)
        {
            object request = LifecycleTestSupport.Trans(new PutBucketAccessMonitorInput
            {
                Bucket = "example-bucket", Status = status
            });
            Assert.That(LifecycleTestSupport.Get(request, "Operation"), Is.EqualTo("PutBucketAccessMonitor"));
            Assert.That(LifecycleTestSupport.Get(request, "Method"), Is.EqualTo(HttpMethodType.HttpMethodPut));
            Assert.That(LifecycleTestSupport.Get(request, "Bucket"), Is.EqualTo("example-bucket"));
            IDictionary<string, string> query = LifecycleTestSupport.Dictionary(request, "Query");
            Assert.That(query.Count, Is.EqualTo(1));
            Assert.That(query["accessmonitor"], Is.Empty);
            string body = LifecycleTestSupport.Body(request);
            StringAssert.Contains("\"Status\": \"" + wireValue + "\"", body);
            IDictionary<string, string> header = LifecycleTestSupport.Dictionary(request, "Header");
            byte[] data = Encoding.UTF8.GetBytes(body);
            Assert.That(header["Content-Length"], Is.EqualTo(data.Length.ToString()));
            using (MD5 md5 = MD5.Create())
            {
                Assert.That(header["Content-MD5"], Is.EqualTo(Convert.ToBase64String(md5.ComputeHash(data))));
            }
        }

        [Test]
        public void TestGetRequest()
        {
            object request = LifecycleTestSupport.Trans(new GetBucketAccessMonitorInput { Bucket = "example-bucket" });
            Assert.That(LifecycleTestSupport.Get(request, "Operation"), Is.EqualTo("GetBucketAccessMonitor"));
            Assert.That(LifecycleTestSupport.Get(request, "Method"), Is.EqualTo(HttpMethodType.HttpMethodGet));
            Assert.That(LifecycleTestSupport.Get(request, "Bucket"), Is.EqualTo("example-bucket"));
            IDictionary<string, string> query = LifecycleTestSupport.Dictionary(request, "Query");
            Assert.That(query.Count, Is.EqualTo(1));
            Assert.That(query["accessmonitor"], Is.Empty);
            Assert.That(LifecycleTestSupport.Get(request, "Body"), Is.Null);
        }

        [TestCase("{\"Status\":\"Enabled\"}", StatusType.StatusEnabled)]
        [TestCase("{\"Status\":\"Disabled\",\"FutureField\":true}", StatusType.StatusDisabled)]
        public void TestParseResponse(string body, StatusType status)
        {
            GetBucketAccessMonitorOutput output = LifecycleTestSupport.Parse<GetBucketAccessMonitorOutput>(
                new Dictionary<string, string>(), body, null);
            Assert.That(output.Status, Is.EqualTo(status));
            Assert.That(output.StatusCode, Is.EqualTo(200));
            Assert.That(output.RequestID, Is.EqualTo("request-id"));
            Assert.That(output.ID2, Is.EqualTo("id-2"));
        }

        [TestCase("{}")]
        [TestCase("{\"Status\":null}")]
        [TestCase("{\"Status\":\"\"}")]
        [TestCase("{\"Status\":\"FutureStatus\"}")]
        public void TestMissingOrUnknownStatusIsNotEnabled(string body)
        {
            Assert.That(LifecycleTestSupport.Parse<GetBucketAccessMonitorOutput>(
                new Dictionary<string, string>(), body, null).Status, Is.Null);
        }

        [Test]
        public void TestStatusIsRequiredAndValidated()
        {
            foreach (StatusType? status in new StatusType?[] { null, (StatusType)(-1), (StatusType)99 })
            {
                Assert.Throws<TosClientException>(() => LifecycleTestSupport.Trans(
                    new PutBucketAccessMonitorInput { Bucket = "example-bucket", Status = status }));
            }
        }

        [Test]
        public void TestNullInputAndInvalidBucketFailBeforeSending()
        {
            using (ITosAccessMonitorClient client = Builder().BuildAccessMonitor())
            {
                Assert.Throws<TosClientException>(() => client.PutBucketAccessMonitor(null));
                Assert.Throws<TosClientException>(() => client.GetBucketAccessMonitor(null));
                foreach (string bucket in new[] { null, "", "ab", "Invalid_Bucket", new string('a', 64) })
                {
                    Assert.Throws<TosClientException>(() => client.PutBucketAccessMonitor(
                        new PutBucketAccessMonitorInput { Bucket = bucket, Status = StatusType.StatusEnabled }));
                    Assert.Throws<TosClientException>(() => client.GetBucketAccessMonitor(
                        new GetBucketAccessMonitorInput { Bucket = bucket }));
                }
            }
        }

        [Test]
        public void TestExistingInterfaceContractsAreUnchanged()
        {
            Assert.That(typeof(ITosClient).GetMethod("PutBucketAccessMonitor"), Is.Null);
            Assert.That(typeof(ITosClientV2).GetMethod("PutBucketAccessMonitor"), Is.Null);
            Assert.That(typeof(ITosAccessMonitorClient).IsAssignableFrom(typeof(ITosClientV2)), Is.False);
            Assert.That(typeof(TosClientBuilder).GetMethod("Build").ReturnType, Is.EqualTo(typeof(ITosClient)));
            Assert.That(typeof(TosClientBuilder).GetMethod("BuildV2").ReturnType, Is.EqualTo(typeof(ITosClientV2)));
            using (ITosClient legacy = Builder().Build())
            using (ITosClientV2 v2 = Builder().BuildV2())
            {
                Assert.That(legacy, Is.InstanceOf<ITosAccessMonitorClient>());
                Assert.That(v2, Is.InstanceOf<ITosAccessMonitorClient>());
            }
        }

        private static TosClientBuilder Builder()
        {
            return TosClientBuilder.Builder().SetAk("test-ak").SetSk("test-sk")
                .SetRegion("cn-beijing").SetEndpoint("http://tos.example.invalid");
        }
    }
}
