using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using TOS;
using TOS.Common;
using TOS.Error;
using TOS.Model;

namespace TestTOS
{
    [TestFixture]
    public class TestObjectSetManagement
    {
        [TestCase(true)]
        [TestCase(false)]
        public void TestConfigurationRequestAndResponse(bool enableDefault)
        {
            object request = LifecycleTestSupport.Trans(new PutBucketObjectSetConfigurationInput
            {
                Bucket = "example-bucket", PathLevel = 3, CustomDelimiter = "/",
                EnableDefaultObjectSet = enableDefault, StorageQuota = "1099511627776",
                Qos = new QosConfig { ReadsQps = 0, WritesQps = 2000, ListQps = 3000, ReadsRate = 4000, WritesRate = 5000 }
            });
            AssertRequest(request, "PutBucketObjectSetConfiguration", HttpMethodType.HttpMethodPut, "objectsetconfiguration");
            string body = AssertBody(request);
            StringAssert.DoesNotContain("Bucket", body);
            StringAssert.Contains("\"EnableDefaultObjectSet\": " + (enableDefault ? "true" : "false"), body);
            GetBucketObjectSetConfigurationOutput output = Parse<GetBucketObjectSetConfigurationOutput>(body);
            Assert.That(output.PathLevel, Is.EqualTo(3));
            Assert.That(output.CustomDelimiter, Is.EqualTo("/"));
            Assert.That(output.EnableDefaultObjectSet, Is.EqualTo(enableDefault));
            Assert.That(output.StorageQuota, Is.EqualTo("1099511627776"));
            Assert.That(output.Qos.ReadsQps, Is.EqualTo(0));
            Assert.That(output.Qos.WritesQps, Is.EqualTo(2000));
            Assert.That(output.Qos.ListQps, Is.EqualTo(3000));
            Assert.That(output.Qos.ReadsRate, Is.EqualTo(4000));
            Assert.That(output.Qos.WritesRate, Is.EqualTo(5000));
        }

        [Test]
        public void TestMinimalConfigurationOmitsOptionalFields()
        {
            string body = LifecycleTestSupport.Body(LifecycleTestSupport.Trans(
                new PutBucketObjectSetConfigurationInput { Bucket = "example-bucket", PathLevel = 3 }));
            StringAssert.DoesNotContain("Qos", body);
            StringAssert.DoesNotContain("CustomDelimiter", body);
            StringAssert.DoesNotContain("StorageQuota", body);
            StringAssert.Contains("\"EnableDefaultObjectSet\": false", body);
            GetBucketObjectSetConfigurationOutput output = Parse<GetBucketObjectSetConfigurationOutput>(
                "{\"PathLevel\":3,\"Qos\":{\"ReadsQps\":7,\"Future\":true}}");
            Assert.That(output.Qos.ReadsQps, Is.EqualTo(7));
            Assert.That(output.Qos.ListQps, Is.Null);
            Assert.That(Parse<GetBucketObjectSetConfigurationOutput>("{}").Qos, Is.Null);
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void TestInvalidPathLevel(int level)
        {
            Assert.Throws<TosClientException>(() => LifecycleTestSupport.Trans(
                new PutBucketObjectSetConfigurationInput { Bucket = "example-bucket", PathLevel = level }));
        }

        [Test]
        public void TestReadAndDeleteRequests()
        {
            const string name = "tenant/中文/a+b%/";
            GenericInput[] inputs =
            {
                new GetBucketObjectSetConfigurationInput { Bucket = "example-bucket" },
                new GetObjectSetInput { Bucket = "example-bucket", ObjectSetName = name },
                new GetObjectSetTaggingInput { Bucket = "example-bucket", ObjectSetName = name },
                new DeleteObjectSetInput { Bucket = "example-bucket", ObjectSetName = name }
            };
            string[] operations = { "GetBucketObjectSetConfiguration", "GetObjectSet", "GetObjectSetTagging", "DeleteObjectSet" };
            string[] queries = { "objectsetconfiguration", "objectset", "objectsettagging", "objectset" };
            for (int i = 0; i < inputs.Length; i++)
            {
                object request = LifecycleTestSupport.Trans(inputs[i]);
                AssertRequest(request, operations[i], i == 3 ? HttpMethodType.HttpMethodDelete : HttpMethodType.HttpMethodGet, queries[i]);
                Assert.That(LifecycleTestSupport.Get(request, "Body"), Is.Null);
                IDictionary<string, string> query = LifecycleTestSupport.Dictionary(request, "Query");
                Assert.That(query.Count, Is.EqualTo(i == 0 ? 1 : 2));
                if (i > 0)
                {
                    Assert.That(query["ObjectSetName"], Is.EqualTo(name));
                }
            }
        }

        [TestCase(true)]
        [TestCase(false)]
        public void TestPutObjectSetAndTaggingUseJsonBody(bool tagging)
        {
            const string name = "tenant/中文/space +";
            TagSet tags = new TagSet { Tags = new[] { new Tag { Key = "env", Value = "生产" }, new Tag { Key = "empty", Value = "" } } };
            GenericInput input = tagging
                ? (GenericInput)new PutObjectSetTaggingInput { Bucket = "example-bucket", ObjectSetName = name, TagSet = tags }
                : new PutObjectSetInput { Bucket = "example-bucket", ObjectSetName = name, TagSet = tags };
            object request = LifecycleTestSupport.Trans(input);
            AssertRequest(request, tagging ? "PutObjectSetTagging" : "PutObjectSet", HttpMethodType.HttpMethodPut,
                tagging ? "objectsettagging" : "objectset");
            Assert.That(LifecycleTestSupport.Dictionary(request, "Query").Count, Is.EqualTo(1));
            string body = AssertBody(request);
            GetObjectSetOutput output = Parse<GetObjectSetOutput>(body);
            Assert.That(output.ObjectSetName, Is.EqualTo(name));
            Assert.That(output.TagSet.Tags.Length, Is.EqualTo(2));
            Assert.That(output.TagSet.Tags[0].Value, Is.EqualTo("生产"));
            Assert.That(output.TagSet.Tags[1].Value, Is.Empty);
            Assert.That(Parse<GetObjectSetTaggingOutput>(body).TagSet.Tags[0].Key, Is.EqualTo("env"));
        }

        [Test]
        public void TestCreateWithoutTagsAndClearTags()
        {
            string create = LifecycleTestSupport.Body(LifecycleTestSupport.Trans(new PutObjectSetInput
            {
                Bucket = "example-bucket", ObjectSetName = "a/b/c"
            }));
            StringAssert.DoesNotContain("TagSet", create);
            foreach (Tag[] tags in new[] { null, new Tag[0] })
            {
                string body = LifecycleTestSupport.Body(LifecycleTestSupport.Trans(new PutObjectSetTaggingInput
                {
                    Bucket = "example-bucket", ObjectSetName = "a/b/c", TagSet = new TagSet { Tags = tags }
                }));
                StringAssert.Contains("\"Tags\": []", body);
            }
        }

        [TestCase(null)]
        [TestCase("")]
        public void TestMissingNamesFailLocally(string name)
        {
            GenericInput[] inputs =
            {
                new PutObjectSetInput { ObjectSetName = name }, new GetObjectSetInput { ObjectSetName = name },
                new DeleteObjectSetInput { ObjectSetName = name }, new PutObjectSetTaggingInput { ObjectSetName = name },
                new GetObjectSetTaggingInput { ObjectSetName = name }
            };
            foreach (GenericInput input in inputs)
            {
                Assert.Throws<TosClientException>(() => LifecycleTestSupport.Trans(input));
            }
        }

        [Test]
        public void TestInvalidTagsFailLocally()
        {
            Assert.Throws<TosClientException>(() => LifecycleTestSupport.Trans(new PutObjectSetTaggingInput
            {
                Bucket = "example-bucket", ObjectSetName = "a/b/c"
            }));
            foreach (Tag tag in new[] { null, new Tag(), new Tag { Key = "" } })
            {
                Assert.Throws<TosClientException>(() => LifecycleTestSupport.Trans(new PutObjectSetInput
                {
                    Bucket = "example-bucket", ObjectSetName = "a/b/c", TagSet = new TagSet { Tags = new[] { tag } }
                }));
            }
        }

        [Test]
        public void TestListPaginationAndFiltersAreNotPreEncoded()
        {
            object request = LifecycleTestSupport.Trans(new ListObjectSetInput
            {
                Bucket = "example-bucket", Prefix = "中文/", Tags = "env=prod&owner=a+b", Marker = "a/%+ b/", MaxKeys = 2
            });
            AssertRequest(request, "ListObjectSet", HttpMethodType.HttpMethodGet, "objectsets");
            IDictionary<string, string> query = LifecycleTestSupport.Dictionary(request, "Query");
            Assert.That(query.Count, Is.EqualTo(5));
            Assert.That(query["prefix"], Is.EqualTo("中文/"));
            Assert.That(query["tags"], Is.EqualTo("env=prod&owner=a+b"));
            Assert.That(query["marker"], Is.EqualTo("a/%+ b/"));
            Assert.That(query["max-keys"], Is.EqualTo("2"));
            object defaults = LifecycleTestSupport.Trans(new ListObjectSetInput { Bucket = "example-bucket" });
            Assert.That(LifecycleTestSupport.Dictionary(defaults, "Query").Count, Is.EqualTo(1));
        }

        [TestCase(0)]
        [TestCase(1000)]
        public void TestListMaxKeysMatchesExistingListConventions(int maxKeys)
        {
            ListObjectSetInput input = new ListObjectSetInput { Bucket = "example-bucket", MaxKeys = maxKeys };
            input.MaxKeys = -1;
            Assert.That(LifecycleTestSupport.Dictionary(LifecycleTestSupport.Trans(input), "Query")["max-keys"],
                Is.EqualTo(maxKeys.ToString()));
        }

        [Test]
        public void TestListResponsePreservesNamesAndTags()
        {
            ListObjectSetOutput output = Parse<ListObjectSetOutput>("{\"IsTruncated\":true,\"NextMarker\":\"a/b/c/\"," +
                "\"ObjectSets\":[{\"ObjectSetName\":\"a/b/c/\",\"TagSet\":{\"Tags\":[{\"Key\":\"k\",\"Value\":\"\"}]}}," +
                "{\"ObjectSetName\":\"中文/集合/一/\",\"Future\":42}]}");
            Assert.That(output.IsTruncated, Is.True);
            Assert.That(output.NextMarker, Is.EqualTo("a/b/c/"));
            Assert.That(output.ObjectSets.Length, Is.EqualTo(2));
            Assert.That(output.ObjectSets[0].TagSet.Tags[0].Value, Is.Empty);
            Assert.That(output.ObjectSets[1].ObjectSetName, Is.EqualTo("中文/集合/一/"));
            Assert.That(output.ObjectSets[1].TagSet.Tags, Is.Empty);
            Assert.That(output.RequestID, Is.EqualTo("request-id"));
        }

        [TestCase("{}")]
        [TestCase("{\"ObjectSets\":null,\"TagSet\":null}")]
        [TestCase("{\"ObjectSets\":[],\"TagSet\":{\"Tags\":null}}")]
        public void TestMissingCollectionsAreEmpty(string body)
        {
            Assert.That(Parse<ListObjectSetOutput>(body).ObjectSets, Is.Empty);
            Assert.That(Parse<GetObjectSetOutput>(body).TagSet.Tags, Is.Empty);
        }

        [Test]
        public void TestTagResponseMayOmitEmptyValue()
        {
            GetObjectSetTaggingOutput output = Parse<GetObjectSetTaggingOutput>(
                "{\"ObjectSetName\":\"a/b/c/\",\"TagSet\":{\"Tags\":[{\"Key\":\"empty\"}]}}");
            Assert.That(output.TagSet.Tags.Length, Is.EqualTo(1));
            Assert.That(output.TagSet.Tags[0].Key, Is.EqualTo("empty"));
            Assert.That(output.TagSet.Tags[0].Value, Is.Null);
        }

        [Test]
        public void TestNewClientDoesNotExpandExistingInterfaces()
        {
            Assert.That(typeof(ITosObjectSetClient).IsAssignableFrom(typeof(ITosClientV2)), Is.False);
            Assert.That(typeof(ITosClient).GetMethod("PutObjectSet"), Is.Null);
            using (ITosObjectSetClient client = TosClientBuilder.Builder().SetAk("test-ak").SetSk("test-sk")
                .SetRegion("cn-beijing").SetEndpoint("http://tos.example.invalid").BuildObjectSet())
            {
                Assert.Throws<TosClientException>(() => client.PutBucketObjectSetConfiguration(null));
                Assert.Throws<TosClientException>(() => client.GetBucketObjectSetConfiguration(null));
                Assert.Throws<TosClientException>(() => client.PutObjectSet(null));
                Assert.Throws<TosClientException>(() => client.GetObjectSet(null));
                Assert.Throws<TosClientException>(() => client.ListObjectSet(null));
                Assert.Throws<TosClientException>(() => client.DeleteObjectSet(null));
                Assert.Throws<TosClientException>(() => client.PutObjectSetTagging(null));
                Assert.Throws<TosClientException>(() => client.GetObjectSetTagging(null));
                Assert.Throws<TosClientException>(() => client.ListObjectSet(new ListObjectSetInput { Bucket = "Invalid_Bucket" }));
            }
        }

        private static void AssertRequest(object request, string operation, HttpMethodType method, string query)
        {
            Assert.That(LifecycleTestSupport.Get(request, "Operation"), Is.EqualTo(operation));
            Assert.That(LifecycleTestSupport.Get(request, "Method"), Is.EqualTo(method));
            Assert.That(LifecycleTestSupport.Get(request, "Bucket"), Is.EqualTo("example-bucket"));
            Assert.That(LifecycleTestSupport.Dictionary(request, "Query")[query], Is.Empty);
        }

        private static string AssertBody(object request)
        {
            string body = LifecycleTestSupport.Body(request);
            Assert.That(LifecycleTestSupport.Dictionary(request, "Header")["Content-Length"],
                Is.EqualTo(Encoding.UTF8.GetByteCount(body).ToString()));
            return body;
        }

        private static T Parse<T>(string body) where T : GenericOutput, new()
        {
            return LifecycleTestSupport.Parse<T>(new Dictionary<string, string>(), body, null);
        }
    }
}
