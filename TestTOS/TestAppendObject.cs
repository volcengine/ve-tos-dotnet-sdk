using System;
using System.Collections.Generic;
using NUnit.Framework;
using TOS.Error;
using TOS.Model;

namespace TestTOS
{
    [TestFixture]
    public class TestAppendObject
    {
        [Test]
        public void TestNormal()
        {
            var env = new TestEnv();
            var client = env.PrepareClientBuilder().SetSocketTimeout(360*1000).SetConnectionTimeout(360*1000).Build();
            var bucket = Util.GenerateBucketName("append-obj");
            var key = Util.GenerateObjectName("normal");
            var data = Util.GenerateRandomStr(7*20000);

            var createBucketInput = new CreateBucketInput
            {
                Bucket = bucket
            };
            Assert.DoesNotThrow(() => client.CreateBucket(createBucketInput));
            
            var appendObjectInput = new AppendObjectInput()
            {
                Bucket = bucket,
                Key = key,
                Content = Util.ConvertStringToStream(data)
            };
            var appendObjectOutput = client.AppendObject(appendObjectInput);
            Assert.Greater(appendObjectOutput.RequestID.Length, 0);
            Assert.Greater(appendObjectOutput.NextAppendOffset, 0);
                
            var getObjectInput = new GetObjectInput()
            {
                Bucket = bucket,
                Key = key
            };
            var getObjectOutput = client.GetObject(getObjectInput);
            Assert.Greater(getObjectOutput.RequestID.Length, 0);
            Assert.AreEqual(data, Util.ReadStreamAsString(getObjectOutput.Content));
            getObjectOutput.Content.Close();
            Assert.AreEqual(getObjectOutput.Meta.Count, 0);
            
            var deleteObjectInput = new DeleteObjectInput()
            {
                Bucket = bucket,
                Key = key,
            };
            client.DeleteObject(deleteObjectInput);
            
            // 所有参数上传对象
            appendObjectInput = new AppendObjectInput()
            {
                Bucket = bucket,
                Key = key,
                Content = Util.ConvertStringToStream(data),
                StorageClass = StorageClassType.StorageClassStandard,
                ACL = ACLType.ACLPublicRead,
                ContentDisposition = "test-disposition",
                Expires = DateTime.Now.AddHours(1),
                Meta = new Dictionary<string, string>(){{"aaa","bbb"}, {"中文键","中文值"}},
                ContentEncoding = "test-encoding",
                ContentLanguage = "test-language",
                ContentType = "text/plain",
                WebsiteRedirectLocation = "http://test-website-redirection-location",
            };
            appendObjectOutput = client.AppendObject(appendObjectInput);
            Assert.Greater(appendObjectOutput.RequestID.Length, 0);
            Assert.Greater(appendObjectOutput.NextAppendOffset, 0);
            var nextAppendOffset = appendObjectOutput.NextAppendOffset;
            
            getObjectOutput = client.GetObject(getObjectInput);
            Assert.Greater(getObjectOutput.RequestID.Length, 0);
            Assert.AreEqual(data, Util.ReadStreamAsString(getObjectOutput.Content));
            getObjectOutput.Content.Close();
            
            Assert.AreEqual(data.Length, getObjectOutput.ContentLength);
            Assert.AreEqual(appendObjectInput.StorageClass, getObjectOutput.StorageClass);
            Assert.AreEqual(appendObjectInput.ContentDisposition, getObjectOutput.ContentDisposition);
            Assert.AreEqual(appendObjectInput.Expires.ToString(), getObjectOutput.Expires.ToString());
            Assert.AreEqual(appendObjectInput.ContentEncoding, getObjectOutput.ContentEncoding);
            Assert.AreEqual(appendObjectInput.ContentLanguage, getObjectOutput.ContentLanguage);
            Assert.AreEqual(appendObjectInput.ContentType, getObjectOutput.ContentType);
            Assert.AreEqual(appendObjectInput.WebsiteRedirectLocation, getObjectOutput.WebsiteRedirectLocation);
            Assert.AreEqual(appendObjectInput.Meta.Count, getObjectOutput.Meta.Count);
            Assert.AreEqual("bbb", getObjectOutput.Meta["x-tos-meta-aaa"]);
            Assert.AreEqual("中文值", getObjectOutput.Meta["x-tos-meta-中文键"]);

            appendObjectInput.Offset = nextAppendOffset;
            appendObjectInput.Content = Util.ConvertStringToStream(data);
            appendObjectOutput = client.AppendObject(appendObjectInput);
            Assert.Greater(appendObjectOutput.RequestID.Length, 0);
            Assert.Greater(appendObjectOutput.NextAppendOffset, 0);
            
            nextAppendOffset = appendObjectOutput.NextAppendOffset;
            appendObjectInput.Offset = nextAppendOffset;
            appendObjectInput.Content = Util.ConvertStringToStream(data);
            appendObjectOutput = client.AppendObject(appendObjectInput);
            Assert.Greater(appendObjectOutput.RequestID.Length, 0);
            Assert.Greater(appendObjectOutput.NextAppendOffset, 0);
            
            getObjectOutput = client.GetObject(getObjectInput);
            Assert.Greater(getObjectOutput.RequestID.Length, 0);
            Assert.AreEqual(data+data+data, Util.ReadStreamAsString(getObjectOutput.Content));
            getObjectOutput.Content.Close();
            
            client.DeleteObject(deleteObjectInput);
            var deleteBucketInput = new DeleteBucketInput
            {
                Bucket = bucket
            };
            client.DeleteBucket(deleteBucketInput);
        }

        [Test]
        public void TestAbnormal()
        {
            var env = new TestEnv();
            var client = env.PrepareClientBuilder().SetSocketTimeout(360*1000).SetConnectionTimeout(360*1000).Build();
            var bucket = Util.GenerateBucketName("append-obj");
            var key = Util.GenerateObjectName("abnormal");
            var data = Util.GenerateRandomStr(200*1024);

            var createBucketInput = new CreateBucketInput
            {
                Bucket = bucket
            };
            Assert.DoesNotThrow(() => client.CreateBucket(createBucketInput));
            
            var appendObjectInput = new AppendObjectInput()
            {
                Bucket = "no-exist-bucket",
                Key = key,
                Content = Util.ConvertStringToStream(data)
            };
            var ex = Assert.Throws<TosServerException>(() => client.AppendObject(appendObjectInput));
            Assert.AreEqual(404, ex.StatusCode);
            Assert.AreEqual("NoSuchBucket", ex.Code);
            
            appendObjectInput = new AppendObjectInput()
            {
                Bucket = bucket,
                Key = Util.GenerateRandomStr(1025),
                Content = Util.ConvertStringToStream(data)
            };
            
            ex = Assert.Throws<TosServerException>(() => client.AppendObject(appendObjectInput));
            Assert.AreEqual(400, ex.StatusCode);
            Assert.AreEqual("KeyTooLong", ex.Code);
            
            var deleteBucketInput = new DeleteBucketInput
            {
                Bucket = bucket
            };
            client.DeleteBucket(deleteBucketInput);
        }
    }
}