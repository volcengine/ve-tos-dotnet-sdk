using System;
using System.Collections.Generic;
using NUnit.Framework;
using TOS.Error;
using TOS.Model;

namespace TestTOS
{
    [TestFixture]
    public class TestCopyObject
    {
        [Test]
        public void TestNormal()
        {
            var env = new TestEnv();
            var client = env.PrepareClientBuilder().SetSocketTimeout(360 * 1000).SetConnectionTimeout(360 * 1000)
                .Build();
            var bucket1 = Util.GenerateBucketName("copy-obj-basic-1");
            var bucket2 = Util.GenerateBucketName("copy-obj-basic-2");
            var key1 = Util.GenerateObjectName("normal-1");
            var key2 = Util.GenerateObjectName("normal-2");
            var data = "hello world";

            var createBucketInput = new CreateBucketInput
            {
                Bucket = bucket1
            };
            Assert.DoesNotThrow(() => client.CreateBucket(createBucketInput));

            var putObjectInput = new PutObjectInput()
            {
                Bucket = bucket1,
                Key = key1,
                Content = Util.ConvertStringToStream(data),
                Meta = new Dictionary<string, string>() { { "aaa", "bbb" }, { "ccc", "ddd" } }
            };
            var putObjectOutput = client.PutObject(putObjectInput);
            Assert.Greater(putObjectOutput.RequestID.Length, 0);                
            Assert.Greater(putObjectOutput.ETag.Length, 0);                
            Assert.AreEqual(200, putObjectOutput.StatusCode);

            var dstKey1 = key1 + "-bak";
            var copyObjectInput = new CopyObjectInput()
            {
                SrcBucket = bucket1,
                SrcKey = key1,
                Bucket = bucket1,
                Key = dstKey1
            };
            var copyObjectOutput = client.CopyObject(copyObjectInput);
            Assert.Greater(copyObjectOutput.RequestID.Length, 0);                
            Assert.Greater(copyObjectOutput.ETag.Length, 0);   
            
            var getObjectInput = new GetObjectInput()
            {
                Bucket = bucket1,
                Key = dstKey1
            };
            var getObjectOutput = client.GetObject(getObjectInput);
            Assert.Greater(getObjectOutput.RequestID.Length, 0);
            Assert.AreEqual(data, Util.ReadStreamAsString(getObjectOutput.Content));
            getObjectOutput.Content.Close();
            
            createBucketInput = new CreateBucketInput
            {
                Bucket = bucket2
            };
            Assert.DoesNotThrow(() => client.CreateBucket(createBucketInput));
            
            copyObjectInput = new CopyObjectInput()
            {
                SrcBucket = bucket1,
                SrcKey = key1,
                Bucket = bucket2,
                Key = dstKey1
            };
            copyObjectOutput = client.CopyObject(copyObjectInput);
            Assert.Greater(copyObjectOutput.RequestID.Length, 0);                
            Assert.Greater(copyObjectOutput.ETag.Length, 0);  
            
            getObjectInput = new GetObjectInput()
            {
                Bucket = bucket2,
                Key = dstKey1
            };
            getObjectOutput = client.GetObject(getObjectInput);
            Assert.Greater(getObjectOutput.RequestID.Length, 0);
            Assert.AreEqual(data, Util.ReadStreamAsString(getObjectOutput.Content));
            getObjectOutput.Content.Close();
            
            // 所有参数Copy对象
            var dstKey2 = key2 + "-bak";
            copyObjectInput = new CopyObjectInput()
            {
                SrcBucket = bucket1,
                SrcKey = key1,
                Bucket = bucket2,
                Key = dstKey2,
                StorageClass = StorageClassType.StorageClassIa,
                ACL = ACLType.ACLPublicRead,
                ContentDisposition = "test-disposition",
                Expires = DateTime.Now.AddHours(1),
                Meta = new Dictionary<string, string>(){{"aaa","bbb"}, {"中文键","中文值"}},
                ContentEncoding = "test-encoding",
                ContentLanguage = "test-language",
                ContentType = "text/plain",
                WebsiteRedirectLocation = "http://test-website-redirection-location",
                MetadataDirective = MetadataDirectiveType.MetadataDirectiveCopy
            };
            copyObjectOutput = client.CopyObject(copyObjectInput);
            Assert.Greater(copyObjectOutput.RequestID.Length, 0);                
            Assert.Greater(copyObjectOutput.ETag.Length, 0);  
            
            getObjectInput = new GetObjectInput()
            {
                Bucket = bucket2,
                Key = dstKey2
            };
            getObjectOutput = client.GetObject(getObjectInput);
            Assert.Greater(getObjectOutput.RequestID.Length, 0);
            Assert.AreEqual(data, Util.ReadStreamAsString(getObjectOutput.Content));
            getObjectOutput.Content.Close();
            Assert.AreEqual(copyObjectInput.StorageClass, getObjectOutput.StorageClass);
            Assert.AreEqual(copyObjectInput.Meta.Count, getObjectOutput.Meta.Count);
            Assert.AreEqual("bbb", getObjectOutput.Meta["x-tos-meta-aaa"]);
            Assert.AreEqual("ddd", getObjectOutput.Meta["x-tos-meta-ccc"]);

            copyObjectInput.StorageClass = StorageClassType.StorageClassStandard;
            copyObjectInput.MetadataDirective = MetadataDirectiveType.MetadataDirectiveReplace;
            copyObjectOutput = client.CopyObject(copyObjectInput);
            Assert.Greater(copyObjectOutput.RequestID.Length, 0);                
            Assert.Greater(copyObjectOutput.ETag.Length, 0);  
            
            getObjectOutput = client.GetObject(getObjectInput);
            Assert.Greater(getObjectOutput.RequestID.Length, 0);
            Assert.AreEqual(data, Util.ReadStreamAsString(getObjectOutput.Content));
            getObjectOutput.Content.Close();
            
            Assert.AreEqual(data.Length, getObjectOutput.ContentLength);
            Assert.AreEqual(copyObjectInput.StorageClass, getObjectOutput.StorageClass);
            Assert.AreEqual(copyObjectInput.ContentDisposition, getObjectOutput.ContentDisposition);
            Assert.AreEqual(copyObjectInput.Expires.ToString(), getObjectOutput.Expires.ToString());
            Assert.AreEqual(copyObjectInput.ContentEncoding, getObjectOutput.ContentEncoding);
            Assert.AreEqual(copyObjectInput.ContentLanguage, getObjectOutput.ContentLanguage);
            Assert.AreEqual(copyObjectInput.ContentType, getObjectOutput.ContentType);
            Assert.AreEqual(copyObjectInput.WebsiteRedirectLocation, getObjectOutput.WebsiteRedirectLocation);
            Assert.AreEqual(copyObjectInput.Meta.Count, getObjectOutput.Meta.Count);
            Assert.AreEqual("bbb", getObjectOutput.Meta["x-tos-meta-aaa"]);
            Assert.AreEqual("中文值", getObjectOutput.Meta["x-tos-meta-中文键"]);

            copyObjectInput = new CopyObjectInput()
            {
                SrcBucket = bucket1,
                SrcKey = key1,
                Bucket = bucket2,
                Key = dstKey2,
                SrcVersionID = "123"
            };
            var ex = Assert.Throws<TosServerException>(() => client.CopyObject(copyObjectInput));
            Assert.AreEqual(400, ex.StatusCode);
            Assert.AreEqual( "InvalidArgument", ex.Code);
            
            ClearEnv.ClearDotNetSdkObj(client, bucket1);
            ClearEnv.ClearDotNetSdkObj(client, bucket2);
            Assert.DoesNotThrow(() => client.DeleteBucket(new DeleteBucketInput
            {
                Bucket = bucket1
            }));
            Assert.DoesNotThrow(() => client.DeleteBucket(new DeleteBucketInput
            {
                Bucket = bucket2
            }));
        }
    }
}