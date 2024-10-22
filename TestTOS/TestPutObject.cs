using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using TOS.Error;
using TOS.Model;

namespace TestTOS
{
    [TestFixture]
    public class TestPutObject
    {
        [Test]
        public void TestCas()
        {
            var env = new TestEnv();
            var client = env.PrepareClientBuilder().SetSocketTimeout(360 * 1000).SetConnectionTimeout(360 * 1000)
                .Build();
            var bucket = Util.GenerateBucketName("put-basic");
            var key = Util.GenerateObjectName("cas");
            var data = "hello world";

            var createBucketInput = new CreateBucketInput
            {
                Bucket = bucket
            };
            Assert.DoesNotThrow(() => client.CreateBucket(createBucketInput));

            var putObjectOutput = Util.PutObjectFromStringAndCheckResponse(client, bucket, key, data);
            var etag = putObjectOutput.ETag;
            var noneMatchEtag = Util.CalculateMd5(putObjectOutput.ETag);
            var nowTime = DateTime.Now;

            var getObjectInput = new GetObjectInput()
            {
                Bucket = bucket,
                Key = key,
                IfMatch = etag,
                IfNoneMatch = noneMatchEtag,
                IfModifiedSince = nowTime.AddHours(-1),
                IfUnmodifiedSince = nowTime.AddHours(1),
            };

            var getObjectOutput = client.GetObject(getObjectInput);
            Assert.Greater(getObjectOutput.RequestID.Length, 0);
            Assert.AreEqual(data, Util.ReadStreamAsString(getObjectOutput.Content));
            getObjectOutput.Content.Close();
            var lastModified = getObjectOutput.LastModified;

            getObjectInput = new GetObjectInput()
            {
                Bucket = bucket,
                Key = key,
                IfMatch = noneMatchEtag
            };
            var ex = Assert.Throws<TosServerException>(() => client.GetObject(getObjectInput));
            Assert.AreEqual(412, ex.StatusCode);
            Assert.AreEqual("PreconditionFailed", ex.Code);

            getObjectInput = new GetObjectInput()
            {
                Bucket = bucket,
                Key = key,
                IfNoneMatch = etag
            };
            ex = Assert.Throws<TosServerException>(() => client.GetObject(getObjectInput));
            Assert.AreEqual(304, ex.StatusCode);

            getObjectInput = new GetObjectInput()
            {
                Bucket = bucket,
                Key = key,
                IfUnmodifiedSince = lastModified.Value.AddHours(-1)
            };
            ex = Assert.Throws<TosServerException>(() => client.GetObject(getObjectInput));
            Assert.AreEqual(412, ex.StatusCode);
            Assert.AreEqual("PreconditionFailed", ex.Code);

            getObjectInput = new GetObjectInput()
            {
                Bucket = bucket,
                Key = key,
                IfModifiedSince = lastModified.Value.AddHours(1)
            };
            ex = Assert.Throws<TosServerException>(() => client.GetObject(getObjectInput));
            Assert.AreEqual(304, ex.StatusCode);

            var deleteObjectInput = new DeleteObjectInput()
            {
                Bucket = bucket,
                Key = key,
            };
            client.DeleteObject(deleteObjectInput);

            var deleteBucketInput = new DeleteBucketInput
            {
                Bucket = bucket
            };
            client.DeleteBucket(deleteBucketInput);
        }
        
        [Test]
        public void TestHeadNoneExistentObject()
        {
            var env = new TestEnv();
            var client = env.PrepareClient();
            var bucket = Util.GenerateBucketName("put-basic");
            var key = "none-exist-key";

            try
            {
                var createBucketInput = new CreateBucketInput
                {
                    Bucket = bucket
                };
                Assert.DoesNotThrow(() => client.CreateBucket(createBucketInput));

                Util.CheckBucketMeta(client, bucket, env.Region, StorageClassType.StorageClassStandard);

                var headObjectInput = new HeadObjectInput
                {
                    Bucket = bucket,
                    Key = key
                };

                var ex = Assert.Throws<TosServerException>(() => client.HeadObject(headObjectInput));
                Assert.AreEqual(404, ex.StatusCode);
                Assert.AreEqual("unexpected status code: 404", ex.Message);
            }
            finally
            {
                var deleteBucketInput = new DeleteBucketInput
                {
                    Bucket = bucket
                };
                client.DeleteBucket(deleteBucketInput);
            }
        }

        [Test]
        public void TestGetNoneExistentObject()
        {
            var env = new TestEnv();
            var client = env.PrepareClient();
            var bucket = Util.GenerateBucketName("put-basic");
            var key = "none-exist-key";

            try
            {
                var createBucketInput = new CreateBucketInput
                {
                    Bucket = bucket
                };
                Assert.DoesNotThrow(() => client.CreateBucket(createBucketInput));

                Util.CheckBucketMeta(client, bucket, env.Region, StorageClassType.StorageClassStandard);

                var getObjectInput = new GetObjectInput
                {
                    Bucket = bucket,
                    Key = key
                };

                var ex = Assert.Throws<TosServerException>(() => client.GetObject(getObjectInput));
                Assert.AreEqual(404, ex.StatusCode);
                Assert.AreEqual("NoSuchKey", ex.Code);
            }
            finally
            {
                var deleteBucketInput = new DeleteBucketInput
                {
                    Bucket = bucket
                };
                client.DeleteBucket(deleteBucketInput);
            }
        }

        [Test]
        public void TestPutBasic()
        {
            var env = new TestEnv();
            var client = env.PrepareClient();
            var bucket = Util.GenerateBucketName("put-basic");
            var key = "key123";

            try
            {
                var createBucketInput = new CreateBucketInput
                {
                    Bucket = bucket
                };
                Assert.DoesNotThrow(() => client.CreateBucket(createBucketInput));

                Util.PutRandomObject(client, bucket, key, 4096, StorageClassType.StorageClassStandard);

                Util.WaitUntilObjectExist(client, bucket, key);

                var deleteObjectInput = new DeleteObjectInput
                {
                    Bucket = bucket,
                    Key = key
                };
                client.DeleteObject(deleteObjectInput);
            }
            finally
            {
                var deleteBucketInput = new DeleteBucketInput
                {
                    Bucket = bucket
                };
                client.DeleteBucket(deleteBucketInput);
            }
        }

        [Test]
        public void TestPutCallback()
        {
            var env = new TestEnv();
            var client = env.PrepareClient();
            var bucket = Util.GenerateBucketName("put-callback");
            var key1 = "key1231";
            var key2 = "key1232";

            try
            {
                var createBucketInput = new CreateBucketInput
                {
                    Bucket = bucket
                };
                Assert.DoesNotThrow(() => client.CreateBucket(createBucketInput));

                var invalidOriginInput =
                    $"{{\"callbackUrl\":\"http://343545.xxxx.com\",\"callbackBody\":\"{{\\\"bucket\\\": ${{bucket}}, \\\"object\\\": ${{object}}, \\\"key1\\\": ${{x:key1}}}}\",\"callbackBodyType\":\"application/json\"}}";
                var originInput =
                    $"{{\"callbackUrl\":\"{env.CallbackUrl}\",\"callbackBody\":\"{{\\\"bucket\\\": ${{bucket}}, \\\"object\\\": ${{object}}, \\\"key1\\\": ${{x:key1}}}}\",\"callbackBodyType\":\"application/json\"}}";
                var originVarInput = "{\"x:key1\" : \"ceshi\"}";

                // 回调失败
                var exServer = Assert.Throws<TosServerException>(() => Util.PutRandomObjectWithCallback(client, bucket, key1, 4096, invalidOriginInput, originVarInput));
                Assert.AreEqual(203, exServer.StatusCode);
                Assert.AreEqual("CallbackFailed", exServer.Code);
                Util.WaitUntilObjectExist(client, bucket, key1);
                
                // 回调成功
                Util.PutRandomObjectWithCallback(client, bucket, key2, 4096, originInput, originVarInput);
                Util.WaitUntilObjectExist(client, bucket, key2);

                var deleteObjectInput1 = new DeleteObjectInput
                {
                    Bucket = bucket,
                    Key = key1
                };
                client.DeleteObject(deleteObjectInput1);
                var deleteObjectInput2 = new DeleteObjectInput
                {
                    Bucket = bucket,
                    Key = key2
                };
                client.DeleteObject(deleteObjectInput2);
            }
            finally
            {
                var deleteBucketInput = new DeleteBucketInput
                {
                    Bucket = bucket
                };
                client.DeleteBucket(deleteBucketInput);
            }
        }


        public void TestPutLargeObject()
        {
            var env = new TestEnv();
            var client = env.PrepareClientBuilder().SetSocketTimeout(360 * 1000).SetConnectionTimeout(360 * 1000)
                .Build();
            var bucket = Util.GenerateBucketName("put-basic");
            var key = "key123";

            try
            {
                var createBucketInput = new CreateBucketInput
                {
                    Bucket = bucket
                };
                Assert.DoesNotThrow(() => client.CreateBucket(createBucketInput));

                Util.PutRandomObject(client, bucket, key, 100 * 4096 * 4096, StorageClassType.StorageClassStandard);
                Util.WaitUntilObjectExist(client, bucket, key);

                var deleteObjectInput = new DeleteObjectInput
                {
                    Bucket = bucket,
                    Key = key
                };
                client.DeleteObject(deleteObjectInput);
            }
            finally
            {
                var deleteBucketInput = new DeleteBucketInput
                {
                    Bucket = bucket
                };
                client.DeleteBucket(deleteBucketInput);
            }
        }

        [Test]
        public void TestAbnormal()
        {
            var env = new TestEnv();
            var client = env.PrepareClientBuilder().SetSocketTimeout(360 * 1000).SetConnectionTimeout(360 * 1000)
                .Build();
            var bucket = Util.GenerateBucketName("put-basic");
            var key = Util.GenerateObjectName("abnormal");
            var data = "hello world";

            var createBucketInput = new CreateBucketInput
            {
                Bucket = bucket
            };
            Assert.DoesNotThrow(() => client.CreateBucket(createBucketInput));

            var putObjectInput = new PutObjectInput()
            {
                Bucket = bucket,
                Key = null,
                Content = Util.ConvertStringToStream(data)
            };
            var exClient = Assert.Throws<TosClientException>(() => client.PutObject(putObjectInput));
            Assert.AreEqual("invalid object name", exClient.Message);

            putObjectInput = new PutObjectInput()
            {
                Bucket = bucket,
                Key = Util.GenerateRandomStr(1025),
                Content = Util.ConvertStringToStream(data)
            };
            var exServer = Assert.Throws<TosServerException>(() => client.PutObject(putObjectInput));
            Assert.AreEqual(400, exServer.StatusCode);
            Assert.AreEqual("KeyTooLong", exServer.Code);

            putObjectInput = new PutObjectInput()
            {
                Bucket = bucket,
                Key = key,
                Content = Util.ConvertStringToStream(data),
                SSECAlgorithm = "unknown_ssec_algorithm"
            };
            exClient = Assert.Throws<TosClientException>(() => client.PutObject(putObjectInput));
            Assert.AreEqual("invalid encryption-decryption algorithm", exClient.Message);

            putObjectInput = new PutObjectInput()
            {
                Bucket = "no-exist-bkt",
                Key = key,
                Content = Util.ConvertStringToStream(data),
            };
            exServer = Assert.Throws<TosServerException>(() => client.PutObject(putObjectInput));
            Assert.AreEqual(404, exServer.StatusCode);
            Assert.AreEqual("NoSuchBucket", exServer.Code);

            var getObjectInput = new GetObjectInput()
            {
                Bucket = bucket,
                Key = key + "-no-exist-obj",
            };
            exServer = Assert.Throws<TosServerException>(() => client.GetObject(getObjectInput));
            Assert.AreEqual(404, exServer.StatusCode);
            Assert.AreEqual("NoSuchKey", exServer.Code);

            getObjectInput = new GetObjectInput()
            {
                Bucket = bucket,
                Key = key + "-no-exist-obj",
                Range = "",
                VersionID = "123"
            };
            exServer = Assert.Throws<TosServerException>(() => client.GetObject(getObjectInput));
            Assert.AreEqual(400, exServer.StatusCode);
            Assert.AreEqual("InvalidArgument", exServer.Code);

            putObjectInput = new PutObjectInput()
            {
                Bucket = bucket,
                Key = Util.GenerateRandomStr(3),
                Content = Util.ConvertStringToStream(data),
                ContentMD5 = Convert.ToBase64String(Encoding.UTF8.GetBytes(data))
            };
            exServer = Assert.Throws<TosServerException>(() => client.PutObject(putObjectInput));
            Assert.AreEqual(400, exServer.StatusCode);
            Assert.AreEqual("InvalidDigest", exServer.Code);

            var deleteBucketInput = new DeleteBucketInput
            {
                Bucket = bucket
            };
            client.DeleteBucket(deleteBucketInput);
        }

        [Test]
        public void TestSsec()
        {
            var env = new TestEnv();
            var client = env.PrepareClientBuilder().SetSocketTimeout(360 * 1000).SetConnectionTimeout(360 * 1000)
                .Build();
            var bucket = Util.GenerateBucketName("put-basic");
            var key = Util.GenerateObjectName("ssec");
            var data = "hello world";
            var md5 = Util.CalculateMd5(data);

            var createBucketInput = new CreateBucketInput
            {
                Bucket = bucket
            };
            Assert.DoesNotThrow(() => client.CreateBucket(createBucketInput));

            var putObjectInput = new PutObjectInput()
            {
                Bucket = bucket,
                Key = key,
                Content = Util.ConvertStringToStream(data),
                ContentMD5 = md5,
                SSECAlgorithm = "AES256",
                SSECKey = Convert.ToBase64String(Encoding.UTF8.GetBytes("01234567890123456789012345678901")),
                SSECKeyMD5 = Util.CalculateMd5("01234567890123456789012345678901")
            };
            var putObjectOutput = client.PutObject(putObjectInput);
            Assert.Greater(putObjectOutput.RequestID.Length, 0);

            var getObjectInput = new GetObjectInput()
            {
                Bucket = bucket,
                Key = key,
                SSECAlgorithm = putObjectInput.SSECAlgorithm,
                SSECKey = putObjectInput.SSECKey,
                SSECKeyMD5 = putObjectInput.SSECKeyMD5
            };
            var getObjectOutput = client.GetObject(getObjectInput);
            Assert.Greater(getObjectOutput.RequestID.Length, 0);
            Assert.AreEqual(data, Util.ReadStreamAsString(getObjectOutput.Content));
            getObjectOutput.Content.Close();

            getObjectInput = new GetObjectInput()
            {
                Bucket = bucket,
                Key = key,
            };
            var ex = Assert.Throws<TosServerException>(() => client.GetObject(getObjectInput));
            Assert.AreEqual(400, ex.StatusCode);
            Assert.AreEqual("InvalidRequest", ex.Code);

            var deleteObjectInput = new DeleteObjectInput()
            {
                Bucket = bucket,
                Key = key,
            };
            client.DeleteObject(deleteObjectInput);

            var deleteBucketInput = new DeleteBucketInput
            {
                Bucket = bucket
            };
            client.DeleteBucket(deleteBucketInput);
        }

        [Test]
        public void TestRange()
        {
            var env = new TestEnv();
            var client = env.PrepareClientBuilder().SetSocketTimeout(360 * 1000).SetConnectionTimeout(360 * 1000)
                .Build();
            var bucket = Util.GenerateBucketName("put-basic");
            var key = Util.GenerateObjectName("range");
            var data = "hello world";

            var createBucketInput = new CreateBucketInput
            {
                Bucket = bucket
            };
            Assert.DoesNotThrow(() => client.CreateBucket(createBucketInput));
            Util.PutObjectFromStringAndCheckResponse(client, bucket, key, data);

            var getObjectInput = new GetObjectInput()
            {
                Bucket = bucket,
                Key = key,
                RangeStart = 0,
                RangeEnd = 0
            };
            var getObjectOutput = client.GetObject(getObjectInput);
            Assert.AreEqual(data.Substring(0, 1), Util.ReadStreamAsString(getObjectOutput.Content));
            getObjectOutput.Content.Close();

            getObjectInput = new GetObjectInput()
            {
                Bucket = bucket,
                Key = key,
                Range = "bytes=3-4"
            };
            getObjectOutput = client.GetObject(getObjectInput);
            Assert.AreEqual(data.Substring(3, 4 - 3 + 1), Util.ReadStreamAsString(getObjectOutput.Content));
            getObjectOutput.Content.Close();

            var deleteObjectInput = new DeleteObjectInput()
            {
                Bucket = bucket,
                Key = key,
            };
            client.DeleteObject(deleteObjectInput);

            var deleteBucketInput = new DeleteBucketInput
            {
                Bucket = bucket
            };
            client.DeleteBucket(deleteBucketInput);
        }

        [Test]
        public void TestResponseHttpHeader()
        {
            var env = new TestEnv();
            var client = env.PrepareClientBuilder().SetSocketTimeout(360 * 1000).SetConnectionTimeout(360 * 1000)
                .Build();
            var bucket = Util.GenerateBucketName("put-basic");
            var key = Util.GenerateObjectName("response-http-header");
            var data = "hello world";
            var nowTime = DateTime.Now;

            var createBucketInput = new CreateBucketInput
            {
                Bucket = bucket
            };
            Assert.DoesNotThrow(() => client.CreateBucket(createBucketInput));
            Util.PutObjectFromStringAndCheckResponse(client, bucket, key, data);

            var getObjectInput = new GetObjectInput()
            {
                Bucket = bucket,
                Key = key,
                ResponseContentType = "test-content-type",
                ResponseCacheControl = "test-cache-control",
                ResponseContentDisposition = "test-disposition",
                ResponseContentEncoding = "test-content-encoding",
                ResponseContentLanguage = "test-content-language",
                ResponseExpires = nowTime
            };
            var getObjectOutput = client.GetObject(getObjectInput);
            Assert.AreEqual(data, Util.ReadStreamAsString(getObjectOutput.Content));
            getObjectOutput.Content.Close();

            Assert.AreEqual(getObjectInput.ResponseContentType, getObjectOutput.ContentType);
            Assert.AreEqual(getObjectInput.ResponseCacheControl, getObjectOutput.CacheControl);
            Assert.AreEqual(getObjectInput.ResponseContentDisposition, getObjectOutput.ContentDisposition);
            Assert.AreEqual(getObjectInput.ResponseContentEncoding, getObjectOutput.ContentEncoding);
            Assert.AreEqual(getObjectInput.ResponseContentLanguage, getObjectOutput.ContentLanguage);
            Assert.AreEqual(getObjectInput.ResponseExpires.ToString(), getObjectOutput.Expires.ToString());

            var deleteObjectInput = new DeleteObjectInput()
            {
                Bucket = bucket,
                Key = key,
            };
            client.DeleteObject(deleteObjectInput);

            var deleteBucketInput = new DeleteBucketInput
            {
                Bucket = bucket
            };
            client.DeleteBucket(deleteBucketInput);
        }
    }
}