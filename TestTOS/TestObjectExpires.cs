using System;
using System.Collections.Generic;
using System.IO;
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
    public class TestObjectExpires
    {
        [TestCase(0L)]
        [TestCase(1L)]
        [TestCase(long.MaxValue)]
        public void TestSetRequest(long days)
        {
            object request = LifecycleTestSupport.Trans(new SetObjectExpiresInput
            {
                Bucket = "example-bucket", Key = "test/中文", VersionID = "version/1+", ObjectExpires = days
            });
            Assert.That(LifecycleTestSupport.Get(request, "Operation"), Is.EqualTo("SetObjectExpires"));
            Assert.That(LifecycleTestSupport.Get(request, "Method"), Is.EqualTo(HttpMethodType.HttpMethodPost));
            Assert.That(LifecycleTestSupport.Get(request, "Bucket"), Is.EqualTo("example-bucket"));
            Assert.That(LifecycleTestSupport.Get(request, "Key"), Is.EqualTo("test/中文"));
            IDictionary<string, string> query = LifecycleTestSupport.Dictionary(request, "Query");
            Assert.That(query.Count, Is.EqualTo(2));
            Assert.That(query["objectExpires"], Is.Empty);
            Assert.That(query["versionId"], Is.EqualTo("version/1+"));
            string body = LifecycleTestSupport.Body(request);
            StringAssert.Contains("\"ObjectExpires\": " + days, body);
            StringAssert.DoesNotContain("Bucket", body);
            StringAssert.DoesNotContain("VersionID", body);
            IDictionary<string, string> header = LifecycleTestSupport.Dictionary(request, "Header");
            byte[] data = Encoding.UTF8.GetBytes(body);
            Assert.That(header["Content-Length"], Is.EqualTo(data.Length.ToString()));
            using (MD5 md5 = MD5.Create())
            {
                Assert.That(header["Content-MD5"], Is.EqualTo(Convert.ToBase64String(md5.ComputeHash(data))));
            }
        }

        [Test]
        public void TestVersionIDIsOptional()
        {
            object request = LifecycleTestSupport.Trans(new SetObjectExpiresInput
            {
                Bucket = "example-bucket", Key = "key", ObjectExpires = 7
            });
            Assert.That(LifecycleTestSupport.Dictionary(request, "Query").ContainsKey("versionId"), Is.False);
        }

        [Test]
        public void TestUnsetOrNegativeExpiresAreRejected()
        {
            foreach (long? days in new long?[] { null, -1, long.MinValue })
            {
                Assert.Throws<TosClientException>(() => LifecycleTestSupport.Trans(new SetObjectExpiresInput
                {
                    Bucket = "example-bucket", Key = "key", ObjectExpires = days
                }));
            }
        }

        [TestCase(null)]
        [TestCase(0L)]
        [TestCase(7L)]
        [TestCase(long.MaxValue)]
        public void TestWriteRequestsOnlySendExplicitExpires(long? days)
        {
            GenericInput[] inputs = WriteInputs(days);
            foreach (GenericInput input in inputs)
            {
                object request = LifecycleTestSupport.Trans(input);
                IDictionary<string, string> header = LifecycleTestSupport.Dictionary(request, "Header");
                Assert.That(header.ContainsKey("x-tos-object-expires"), Is.EqualTo(days.HasValue));
                if (days.HasValue)
                {
                    Assert.That(header["x-tos-object-expires"], Is.EqualTo(days.Value.ToString()));
                }

                Assert.That(header.ContainsKey("Expires"), Is.True);
            }
        }

        [Test]
        public void TestNegativeWriteExpiresAreRejected()
        {
            foreach (GenericInput input in WriteInputs(-1))
            {
                Assert.Throws<TosClientException>(() => LifecycleTestSupport.Trans(input));
            }
        }

        [Test]
        public void TestPutObjectFromFileInheritsExpires()
        {
            string path = Path.GetTempFileName();
            try
            {
                object request = LifecycleTestSupport.Trans(new PutObjectFromFileInput
                {
                    Bucket = "example-bucket", Key = "key", FilePath = path, ObjectExpires = 7
                });
                using ((IDisposable)request)
                {
                    Assert.That(LifecycleTestSupport.Dictionary(request, "Header")["x-tos-object-expires"], Is.EqualTo("7"));
                }
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Test]
        public void TestIndependentClientAndValidation()
        {
            Assert.That(typeof(ITosClient).GetMethod("SetObjectExpires"), Is.Null);
            Assert.That(typeof(ITosObjectExpirationClient).IsAssignableFrom(typeof(ITosClientV2)), Is.False);
            using (ITosObjectExpirationClient client = TosClientBuilder.Builder()
                .SetAk("test-ak").SetSk("test-sk").SetRegion("cn-beijing")
                .SetEndpoint("http://tos.example.invalid").BuildObjectExpiration())
            {
                Assert.Throws<TosClientException>(() => client.SetObjectExpires(null));
                Assert.Throws<TosClientException>(() => client.SetObjectExpires(
                    new SetObjectExpiresInput { Key = "key", ObjectExpires = 1 }));
                Assert.Throws<TosClientException>(() => client.SetObjectExpires(
                    new SetObjectExpiresInput { Bucket = "example-bucket", ObjectExpires = 1 }));
            }
        }

        private static GenericInput[] WriteInputs(long? days)
        {
            DateTime expires = new DateTime(2035, 12, 21, 0, 0, 0, DateTimeKind.Utc);
            return new GenericInput[]
            {
                new PutObjectInput { Bucket = "example-bucket", Key = "key", ObjectExpires = days, Expires = expires },
                new CopyObjectInput
                {
                    Bucket = "example-bucket", Key = "key", SrcBucket = "source-bucket", SrcKey = "source",
                    ObjectExpires = days, Expires = expires
                },
                new AppendObjectInput
                {
                    Bucket = "example-bucket", Key = "key", Content = new MemoryStream(), ObjectExpires = days, Expires = expires
                },
                new CreateMultipartUploadInput { Bucket = "example-bucket", Key = "key", ObjectExpires = days, Expires = expires }
            };
        }
    }
}
