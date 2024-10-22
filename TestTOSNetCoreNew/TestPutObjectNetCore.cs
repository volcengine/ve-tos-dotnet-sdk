using System;
using System.Collections.Generic;
using NUnit.Framework;
using TestTOS;
using TOS.Error;
using TOS.Model;

namespace TestTOSNetCore5
{
    [TestFixture]
    public class TestPutObjectNetCore
    {
        [Test]
        public void TestNormal()
        {
            var env = new TestEnv();
            var client = env.PrepareClientBuilder().SetSocketTimeout(360 * 1000).SetConnectionTimeout(360 * 1000)
                .Build();
            var bucket = Util.GenerateBucketName("put-basic");
            var key1 = Util.GenerateObjectName("normal-1");
            var key2 = Util.GenerateObjectName("normal-2");
            var data = "hello world";
            var md5 = Util.CalculateMd5(data.Substring(0, 5));
            var keys = new List<string>()
            {
                "    ",
                "a",
                "仅包含中文",
                "にほんご",
                "Ελληνικά",
                "（!-_.*()/&$@=;:+ ,?{^}%`]>[~<#|'\"）"
            };

            var createBucketInput = new CreateBucketInput
            {
                Bucket = bucket
            };
            Assert.DoesNotThrow(() => client.CreateBucket(createBucketInput));

            // 上传各种有效字符的对象
            foreach (var k in keys)
            {
                Util.PutObjectFromStringAndCheckResponse(client, bucket, k, data);
                Util.GetObjectAndCheckResponse(client, bucket, k, data);
                Util.DeleteObjectAndCheckResponse(client, bucket, k);
            }

            // 必选参数上传对象
            Util.PutObjectFromStringAndCheckResponse(client, bucket, key1, data);
            Util.GetObjectAndCheckResponse(client, bucket, key1, data);

            // 测试流式上传
            var getObjectInput = new GetObjectInput()
            {
                Bucket = bucket,
                Key = key1
            };
            var getObjectOutput = client.GetObject(getObjectInput);
            Assert.AreEqual(0, getObjectOutput.Meta.Count);
            Util.PutObjectFromStreamAndCheckResponse(client, bucket, key2, getObjectOutput.Content);
            getObjectOutput.Content.Close();
            Util.DeleteObjectAndCheckResponse(client, bucket, key2);

            // 所有参数上传对象
            var taggingStr = "k1_K123%20%3A%2B-%3D._%2F=v1_K123%20%3A%2B-%3D._%2F&k0_K123%20%3A%2B-%3D._%2F=v0_K123%20%3A%2B-%3D._%2F";
            var putObjectInput = new PutObjectInput()
            {
                Bucket = bucket,
                Key = key1,
                Content = Util.ConvertStringToStream(data),
                ContentLength = 5,
                StorageClass = StorageClassType.StorageClassIa,
                ACL = ACLType.ACLPublicRead,
                ContentDisposition = "attachment; filename=\"中文%&!@#$%^&*()202411.2029.txt\"",
                Expires = DateTime.Now.AddHours(1),
                Meta = new Dictionary<string, string>() { { "aaa", "bbb" }, { "中文键", "中文值" } },
                ContentEncoding = "test-encoding",
                ContentLanguage = "test-language",
                ContentType = "text/plain",
                WebsiteRedirectLocation = "http://test-website-redirection-location",
                ContentMD5 = md5,
                Tagging = taggingStr,
            };
            var putObjectOutput = client.PutObject(putObjectInput);
            Assert.Greater(putObjectOutput.RequestID.Length, 0);
            Assert.Greater(putObjectOutput.ETag.Length, 0);
            Assert.AreEqual(200, putObjectOutput.StatusCode);

            getObjectOutput = Util.GetObjectAndCheckResponse(client, bucket, key1, data.Substring(0,5));
            Assert.AreEqual(data.Substring(0, 5).Length, getObjectOutput.ContentLength);
            Assert.AreEqual(putObjectInput.StorageClass, getObjectOutput.StorageClass);
            Assert.AreEqual(putObjectInput.ContentDisposition, getObjectOutput.ContentDisposition);
            Assert.AreEqual(putObjectInput.Expires.ToString(), getObjectOutput.Expires.ToString());
            Assert.AreEqual(putObjectInput.ContentEncoding, getObjectOutput.ContentEncoding);
            Assert.AreEqual(putObjectInput.ContentLanguage, getObjectOutput.ContentLanguage);
            Assert.AreEqual(putObjectInput.ContentType, getObjectOutput.ContentType);
            Assert.AreEqual(putObjectInput.WebsiteRedirectLocation, getObjectOutput.WebsiteRedirectLocation);
            Assert.AreEqual(putObjectInput.Meta.Count, getObjectOutput.Meta.Count);
            Assert.AreEqual("bbb", getObjectOutput.Meta["x-tos-meta-aaa"]);
            Assert.AreEqual("中文值", getObjectOutput.Meta["x-tos-meta-中文键"]);
            Assert.AreEqual(2, getObjectOutput.TaggingCount);
            
            var getObjectTaggingInput = new GetObjectTaggingInput()
            {
                Bucket = bucket,
                Key = key1,
            };
            var getObjectTaggingOutput = client.GetObjectTagging(getObjectTaggingInput);
            Assert.AreEqual(200, getObjectTaggingOutput.StatusCode);
            Assert.AreEqual(2, getObjectTaggingOutput.TagSet.Tags.Length);
            Assert.AreEqual("k1_K123 :+-=._/", getObjectTaggingOutput.TagSet.Tags[0].Key);
            Assert.AreEqual("v1_K123 :+-=._/", getObjectTaggingOutput.TagSet.Tags[0].Value);
            Assert.AreEqual("k0_K123 :+-=._/", getObjectTaggingOutput.TagSet.Tags[1].Key);
            Assert.AreEqual("v0_K123 :+-=._/", getObjectTaggingOutput.TagSet.Tags[1].Value);
            
            var headObjectInput = new HeadObjectInput()
            {
                Bucket = bucket,
                Key = key1,
            };
            var headObjectOutput = client.HeadObject(headObjectInput);
            Assert.AreEqual(200, headObjectOutput.StatusCode);
            Assert.AreEqual(2, headObjectOutput.TaggingCount);

            // 上传大小为 0 的对象
            Util.PutObjectFromStreamAndCheckResponse(client, bucket, key1, null);
            Util.GetObjectAndCheckResponse(client, bucket, key1, "");
            Util.DeleteObjectAndCheckResponse(client, bucket, key1);

            var deleteBucketInput = new DeleteBucketInput
            {
                Bucket = bucket
            };
            Assert.DoesNotThrow(() => client.DeleteBucket(deleteBucketInput));
        }
    }
}