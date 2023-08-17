using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using TOS.Model;

namespace TestTOS
{
    [TestFixture]
    public class TestPutObjectFromFile
    {
        [Test]
        public void TestNormal()
        {
            var env = new TestEnv();
            var client = env.PrepareClient();
            var bucket = Util.GenerateBucketName("put-obj-from-file-basic");
            var key = Util.GenerateObjectName("normal");
            var data = "hello world";
            var sampleFilePath = "./temp/" + Util.GenerateRandomStr(10) + ".txt";

            var createBucketInput = new CreateBucketInput
            {
                Bucket = bucket
            };
            Assert.DoesNotThrow(() => client.CreateBucket(createBucketInput));

            try
            {
                Util.CreateRandomStringFile(sampleFilePath, 10000);

                var md5 = Util.CalculateMd5FromFile(sampleFilePath);
                
                var putObjectFromFileInput = new PutObjectFromFileInput()
                {
                    Bucket = bucket,
                    Key = key,
                    FilePath = sampleFilePath,
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

                var putObjectFromFileOutput = client.PutObjectFromFile(putObjectFromFileInput);
                Assert.Greater(putObjectFromFileOutput.RequestID.Length, 0);
                Assert.Greater(putObjectFromFileOutput.ETag.Length, 0);
                
                var getObjectInput = new GetObjectInput()
                {
                    Bucket = bucket,
                    Key = key
                };
                var getObjectOutput = client.GetObject(getObjectInput);
                Assert.Greater(getObjectOutput.RequestID.Length, 0);  
                Assert.AreEqual(200, getObjectOutput.StatusCode);  
                Assert.AreEqual(md5, Util.CalculateMd5(Util.ReadStreamAsString(getObjectOutput.Content)));
                getObjectOutput.Content.Close();
                
                Assert.AreEqual(putObjectFromFileInput.StorageClass, getObjectOutput.StorageClass);
                Assert.AreEqual(putObjectFromFileInput.ContentDisposition, getObjectOutput.ContentDisposition);
                Assert.AreEqual(putObjectFromFileInput.Expires.ToString(), getObjectOutput.Expires.ToString());
                Assert.AreEqual(putObjectFromFileInput.ContentEncoding, getObjectOutput.ContentEncoding);
                Assert.AreEqual(putObjectFromFileInput.ContentLanguage, getObjectOutput.ContentLanguage);
                Assert.AreEqual(putObjectFromFileInput.ContentType, getObjectOutput.ContentType);
                Assert.AreEqual(putObjectFromFileInput.WebsiteRedirectLocation, getObjectOutput.WebsiteRedirectLocation);
                Assert.AreEqual(putObjectFromFileInput.Meta.Count, getObjectOutput.Meta.Count);
                Assert.AreEqual("bbb", getObjectOutput.Meta["x-tos-meta-aaa"]);
                Assert.AreEqual("中文值", getObjectOutput.Meta["x-tos-meta-中文键"]);
            }
            finally
            {
                if (File.Exists(sampleFilePath))
                {
                    File.Delete(sampleFilePath);
                }
            }
        }
    }
}