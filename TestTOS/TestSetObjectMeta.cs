using System;
using System.Collections.Generic;
using NUnit.Framework;
using TOS.Error;
using TOS.Model;

namespace TestTOS
{
    [TestFixture]
    public class TestSetObjectMeta
    {
        [Test]
        public void TestNormal()
        {
            var env = new TestEnv();
            var client = env.PrepareClient();
            var bucket = Util.GenerateBucketName("set-obj-meta-basic");
            var key = Util.GenerateObjectName("normal");
            var data = "hello world";
            var md5 = Util.CalculateMd5(data);
            
            var createBucketInput = new CreateBucketInput
            {
                Bucket = bucket
            };
            Assert.DoesNotThrow(() => client.CreateBucket(createBucketInput));
            
            // 所有参数上传对象
            var putObjectInput = new PutObjectInput()
            {
                Bucket = bucket,
                Key = key,
                Content = Util.ConvertStringToStream(data),
                StorageClass = StorageClassType.StorageClassIa,
                ACL = ACLType.ACLPublicRead,
                ContentDisposition = "test-disposition",
                Expires = DateTime.Now.AddHours(1),
                Meta = new Dictionary<string, string>(){{"aaa","bbb"}, {"中文键","中文值"}},
                ContentEncoding = "test-encoding",
                ContentLanguage = "test-language",
                ContentType = "text/plain",
                WebsiteRedirectLocation = "http://test-website-redirection-location",
                ContentMD5 = md5
            };
            var putObjectOutput = client.PutObject(putObjectInput);
            Assert.Greater(putObjectOutput.RequestID.Length, 0);                
            Assert.Greater(putObjectOutput.ETag.Length, 0);                
            Assert.AreEqual(200, putObjectOutput.StatusCode);
            
            var getObjectOutput = Util.GetObjectAndCheckResponse(client, bucket, key, data);
            Assert.AreEqual(data.Length, getObjectOutput.ContentLength);
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
            
            var getObjectInput = new GetObjectInput()
            {
                Bucket = bucket,
                Key = key,
                ResponseContentDisposition = "attachment; filename=\"中文.txt\""
            };
            getObjectOutput = client.GetObject(getObjectInput);
            Assert.Greater(getObjectOutput.RequestID.Length, 0);
            Assert.AreEqual(data,Util.ReadStreamAsString(getObjectOutput.Content));
            getObjectOutput.Content.Close();
            Assert.AreEqual(getObjectInput.ResponseContentDisposition, getObjectOutput.ContentDisposition);

            var setObjectMetaInput = new SetObjectMetaInput()
            {
                Bucket = bucket,
                Key = key,
                ContentDisposition = "test-disposition-new",
                Expires = DateTime.Now.AddHours(2),
                Meta = new Dictionary<string, string>(){{"ccc","ddd"}, {"中文键-new","中文值-new"}},
                ContentEncoding = "test-encoding-new",
                ContentLanguage = "test-language-new",
                ContentType = "text/plain-new",
            };

            var setObjectMetaOutput = client.SetObjectMeta(setObjectMetaInput);
            Assert.Greater(setObjectMetaOutput.RequestID.Length, 0);

            var headObjectInput = new HeadObjectInput()
            {
                Bucket = bucket,
                Key = key
            };
            var headObjectOutput = client.HeadObject(headObjectInput);
            Assert.AreEqual(setObjectMetaInput.ContentDisposition, headObjectOutput.ContentDisposition);
            Assert.AreEqual(setObjectMetaInput.Expires.ToString(), headObjectOutput.Expires.ToString());
            Assert.AreEqual(setObjectMetaInput.ContentEncoding, headObjectOutput.ContentEncoding);
            Assert.AreEqual(setObjectMetaInput.ContentLanguage, headObjectOutput.ContentLanguage);
            Assert.AreEqual(setObjectMetaInput.ContentType, headObjectOutput.ContentType);
            Assert.AreEqual(setObjectMetaInput.Meta.Count, headObjectOutput.Meta.Count);
            Assert.AreEqual("ddd", headObjectOutput.Meta["x-tos-meta-ccc"]);
            Assert.AreEqual("中文值-new", headObjectOutput.Meta["x-tos-meta-中文键-new"]);
        }

        [Test]
        public void TestAbnormal()
        {
            var env = new TestEnv();
            var client = env.PrepareClient();
            var bucket = Util.GenerateBucketName("set-obj-meta-basic");
            var key = Util.GenerateObjectName("abnormal");
            var data = "hello world";

            var createBucketInput = new CreateBucketInput
            {
                Bucket = bucket
            };
            Assert.DoesNotThrow(() => client.CreateBucket(createBucketInput));

            var headObjectInput = new HeadObjectInput()
            {
                Bucket = "no-exist-bucket",
                Key = key
            };
            var ex = Assert.Throws<TosServerException>(() => client.HeadObject(headObjectInput));
            Assert.AreEqual(404, ex.StatusCode);
            
            var setObjectMetaInput = new SetObjectMetaInput()
            {
                Bucket = "no-exist-bucket",
                Key = key,
                Meta = new Dictionary<string, string>(){{"ccc","ddd"}, {"中文键-new","中文值-new"}},
            };
            ex = Assert.Throws<TosServerException>(() => client.SetObjectMeta(setObjectMetaInput));
            Assert.AreEqual(404, ex.StatusCode);
            Assert.AreEqual("NoSuchBucket", ex.Code);
            
            setObjectMetaInput = new SetObjectMetaInput()
            {
                Bucket = bucket,
                Key = "no-exist-key",
                Meta = new Dictionary<string, string>(){{"ccc","ddd"}, {"中文键-new","中文值-new"}},
            };
            ex = Assert.Throws<TosServerException>(() => client.SetObjectMeta(setObjectMetaInput));
            Assert.AreEqual(404, ex.StatusCode);

            var deleteObjectInput = new DeleteObjectInput()
            {
                Bucket = bucket,
                Key = Util.GenerateRandomStr(400)
            };
            var deleteObjectOutput = client.DeleteObject(deleteObjectInput);
            Assert.Greater(deleteObjectOutput.RequestID.Length, 0);

            deleteObjectInput = new DeleteObjectInput()
            {
                Bucket = "no-exist-bucket",
                Key = Util.GenerateRandomStr(400)
            };
            ex = Assert.Throws<TosServerException>(() => client.DeleteObject(deleteObjectInput));
            Assert.AreEqual(404, ex.StatusCode);
            Assert.AreEqual("NoSuchBucket", ex.Code);

            Util.PutObjectFromStringAndCheckResponse(client, bucket, key, data);
            setObjectMetaInput = new SetObjectMetaInput()
            {
                Bucket = bucket,
                Key = key,
                Meta = new Dictionary<string, string>(){},
                VersionID = "123"
            };
            ex = Assert.Throws<TosServerException>(() => client.SetObjectMeta(setObjectMetaInput));
            Assert.AreEqual(400, ex.StatusCode);
            Assert.AreEqual("InvalidArgument", ex.Code);

            headObjectInput = new HeadObjectInput()
            {
                Bucket = bucket,
                Key = key,
                VersionID = "123"
            };
            ex = Assert.Throws<TosServerException>(() => client.HeadObject(headObjectInput));
            Assert.AreEqual(400, ex.StatusCode);
            
            deleteObjectInput = new DeleteObjectInput()
            {
                Bucket = bucket,
                Key = key,
                VersionID = "123"
            };
            ex = Assert.Throws<TosServerException>(() => client.DeleteObject(deleteObjectInput));
            Assert.AreEqual(400, ex.StatusCode);
            Assert.AreEqual("InvalidArgument", ex.Code);
        }
    }
}