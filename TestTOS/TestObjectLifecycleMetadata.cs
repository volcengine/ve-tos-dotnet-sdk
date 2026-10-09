using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using NUnit.Framework;
using TOS.Model;

namespace TestTOS
{
    [TestFixture]
    public class TestObjectLifecycleMetadata
    {
        private const string ExpiryDate = "Fri, 21 Dec 2035 00:00:00 GMT";

        [Test]
        public void TestAbsentHeadersPreserveExistingMetadata()
        {
            HeadObjectOutput output = Parse(new Dictionary<string, string>
            {
                { "ETag", "etag" }, { "Content-Length", "7" }, { "Content-Type", "text/plain" },
                { "x-tos-storage-class", "STANDARD" }, { "x-tos-meta-test", "value" },
                { "Expires", ExpiryDate }, { "Last-Modified", ExpiryDate }
            });
            Assert.That(output.Expiration, Is.Null);
            Assert.That(output.RestoreInfo, Is.Null);
            Assert.That(output.ETag, Is.EqualTo("etag"));
            Assert.That(output.ContentLength, Is.EqualTo(7));
            Assert.That(output.ContentType, Is.EqualTo("text/plain"));
            Assert.That(output.StorageClass, Is.EqualTo(StorageClassType.StorageClassStandard));
            Assert.That(output.Meta["x-tos-meta-test"], Is.EqualTo("value"));
            Assert.That(output.Expires.Value.ToUniversalTime(), Is.EqualTo(UtcExpiryDate()));
            Assert.That(output.LastModified.Value.ToUniversalTime(), Is.EqualTo(UtcExpiryDate()));
        }

        [TestCase("ongoing-request=\"true\"", true)]
        [TestCase("ongoing-request=\"false\"", false)]
        [TestCase("ongoing-request=true", true)]
        public void TestOngoingRestoreWithoutParameters(string header, bool ongoing)
        {
            RestoreInfo info = ParseRestore(header);
            Assert.That(info.RestoreStatus.OngoingRequest, Is.EqualTo(ongoing));
            Assert.That(info.RestoreStatus.ExpiryDate, Is.Null);
            Assert.That(info.RestoreParam, Is.Null);
        }

        [TestCase("ongoing-request=\"false\", expiry-date=\"Fri, 21 Dec 2035 00:00:00 GMT\"")]
        [TestCase("expiry-date=\"Fri, 21 Dec 2035 00:00:00 GMT\", ongoing-request=\"false\"")]
        [TestCase(" ongoing-request = \"false\" , future=\"a,b=c\", expiry-date = \"Fri, 21 Dec 2035 00:00:00 GMT\" ")]
        public void TestQuotedDatesAndParameterOrder(string header)
        {
            RestoreInfo info = ParseRestore(header);
            Assert.That(info.RestoreStatus.OngoingRequest, Is.False);
            Assert.That(info.RestoreStatus.ExpiryDate, Is.EqualTo(UtcExpiryDate()));
            Assert.That(info.RestoreStatus.ExpiryDate.Value.Kind, Is.EqualTo(DateTimeKind.Utc));
        }

        [TestCase("Standard", TierType.Standard)]
        [TestCase("Expedited", TierType.Expedited)]
        [TestCase("Bulk", TierType.Bulk)]
        public void TestRestoreParameters(string tier, TierType expected)
        {
            HeadObjectOutput output = Parse(new Dictionary<string, string>
            {
                { "X-Tos-Restore", "ongoing-request=\"true\"" },
                { "X-Tos-Restore-Request-Date", ExpiryDate },
                { "X-Tos-Restore-Expiry-Days", "7" }, { "X-Tos-Restore-Tier", tier }
            });
            Assert.That(output.RestoreInfo.RestoreStatus.OngoingRequest, Is.True);
            Assert.That(output.RestoreInfo.RestoreParam.RequestDate, Is.EqualTo(UtcExpiryDate()));
            Assert.That(output.RestoreInfo.RestoreParam.ExpiryDays, Is.EqualTo(7));
            Assert.That(output.RestoreInfo.RestoreParam.Tier, Is.EqualTo(expected));
        }

        [TestCase("x-tos-restore-request-date", ExpiryDate)]
        [TestCase("x-tos-restore-expiry-days", "7")]
        [TestCase("x-tos-restore-tier", "Standard")]
        public void TestPartialRestoreParameters(string key, string value)
        {
            RestoreParam param = Parse(new Dictionary<string, string>
            {
                { "x-tos-restore", "ongoing-request=\"true\"" }, { key, value }
            }).RestoreInfo.RestoreParam;
            Assert.That(param, Is.Not.Null);
            Assert.That(param.RequestDate.HasValue, Is.EqualTo(key == "x-tos-restore-request-date"));
            Assert.That(param.ExpiryDays.HasValue, Is.EqualTo(key == "x-tos-restore-expiry-days"));
            Assert.That(param.Tier.HasValue, Is.EqualTo(key == "x-tos-restore-tier"));
        }

        [TestCase("invalid")]
        [TestCase("-1")]
        [TestCase("99999999999999999999")]
        public void TestMalformedOptionalValuesDoNotBreakRead(string days)
        {
            RestoreInfo info = Parse(new Dictionary<string, string>
            {
                { "x-tos-restore", "ongoing-request=\"invalid\", expiry-date=\"not-a-date\"" },
                { "x-tos-restore-request-date", "not-a-date" },
                { "x-tos-restore-expiry-days", days }, { "x-tos-restore-tier", "FutureTier" }
            }).RestoreInfo;
            Assert.That(info.RestoreStatus.OngoingRequest, Is.False);
            Assert.That(info.RestoreStatus.ExpiryDate, Is.Null);
            Assert.That(info.RestoreParam.RequestDate, Is.Null);
            Assert.That(info.RestoreParam.ExpiryDays, Is.Null);
            Assert.That(info.RestoreParam.Tier, Is.Null);
        }

        [TestCase("")]
        [TestCase(null)]
        public void TestParametersWithoutRestoreHeaderDoNotImplyRestore(string restore)
        {
            Assert.That(Parse(new Dictionary<string, string>
            {
                { "x-tos-restore", restore }, { "x-tos-restore-expiry-days", "7" }
            }).RestoreInfo, Is.Null);
        }

        [TestCase("junk")]
        [TestCase("ongoing-request=\"true")]
        [TestCase("ongoing-request=\"true\", expiry-date=\"unterminated")]
        public void TestMalformedHeaderDoesNotThrow(string restore)
        {
            Assert.DoesNotThrow(() => ParseRestore(restore));
        }

        [Test]
        public void TestDatesAreIndependentOfCurrentCulture()
        {
            CultureInfo previous = Thread.CurrentThread.CurrentCulture;
            try
            {
                Thread.CurrentThread.CurrentCulture = new CultureInfo("fr-FR");
                Assert.That(ParseRestore("expiry-date=\"" + ExpiryDate + "\"")
                    .RestoreStatus.ExpiryDate, Is.EqualTo(UtcExpiryDate()));
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = previous;
            }
        }

        [Test]
        public void TestGetObjectPreservesStreamAndRawExpiration()
        {
            string expiration = "expiry-date=\"" + ExpiryDate + "\", rule-id=\"rule%2F1\"";
            using (GetObjectOutput output = LifecycleTestSupport.Parse<GetObjectOutput>(
                new Dictionary<string, string>
                {
                    { "x-tos-expiration", expiration }, { "x-tos-restore", "ongoing-request=\"false\"" },
                    { "Expires", ExpiryDate }, { "Content-Range", "bytes 0-6/7" }, { "Content-Length", "7" }
                }, "payload", null))
            {
                Assert.That(output.Expiration, Is.EqualTo(expiration));
                Assert.That(output.Header["x-tos-expiration"], Is.EqualTo(expiration));
                Assert.That(output.Expires.Value.ToUniversalTime(), Is.EqualTo(UtcExpiryDate()));
                Assert.That(output.RestoreInfo.RestoreStatus.OngoingRequest, Is.False);
                Assert.That(output.ContentRange, Is.EqualTo("bytes 0-6/7"));
                Assert.That(new StreamReader(output.Content).ReadToEnd(), Is.EqualTo("payload"));
                Assert.That(output.RequestID, Is.EqualTo("request-id"));
            }
        }

        [Test]
        public void TestGetObjectToFileInheritsLifecycleMetadata()
        {
            string path = Path.GetTempFileName();
            try
            {
                object request = LifecycleTestSupport.Trans(new GetObjectToFileInput
                {
                    Bucket = "example-bucket", Key = "test", FilePath = path
                });
                GetObjectToFileOutput output = LifecycleTestSupport.Parse<GetObjectToFileOutput>(
                    new Dictionary<string, string>
                    {
                        { "x-tos-expiration", "expiration" }, { "x-tos-restore", "ongoing-request=\"false\"" }
                    }, "payload", request);
                Assert.That(output.Expiration, Is.EqualTo("expiration"));
                Assert.That(output.RestoreInfo, Is.Not.Null);
                Assert.That(File.ReadAllText(path), Is.EqualTo("payload"));
            }
            finally
            {
                File.Delete(path);
            }
        }

        private static HeadObjectOutput Parse(IDictionary<string, string> header)
        {
            return LifecycleTestSupport.Parse<HeadObjectOutput>(header, "", null);
        }

        private static RestoreInfo ParseRestore(string header)
        {
            return Parse(new Dictionary<string, string> { { "x-tos-restore", header } }).RestoreInfo;
        }

        private static DateTime UtcExpiryDate()
        {
            return new DateTime(2035, 12, 21, 0, 0, 0, DateTimeKind.Utc);
        }
    }
}
