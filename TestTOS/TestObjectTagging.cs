using NUnit.Framework;
using TOS.Error;
using TOS.Model;

namespace TestTOS
{
    [TestFixture]
    public class TestObjectTagging
    {
        [Test]
        public void TestNormal()
        {
            var env = new TestEnv();
            var client = env.PrepareClientBuilder().SetSocketTimeout(360 * 1000).SetConnectionTimeout(360 * 1000)
                .Build();
            var bucket = Util.GenerateBucketName("bkt-obj-tagging-normal");
            var key = Util.GenerateObjectName("normal");
            var data = "hello world";

            var createBucketInput = new CreateBucketInput
            {
                Bucket = bucket
            };
            Assert.DoesNotThrow(() => client.CreateBucket(createBucketInput));

            Util.PutObjectFromStringAndCheckResponse(client, bucket, key, data);
            
            var putObjectTaggingInput = new PutObjectTaggingInput()
            {
                Bucket = bucket,
                Key = key,
                TagSet = new TagSet()
                {
                    Tags = new Tag[]
                    {
                        new Tag()
                        {
                            Key = "k0",
                            Value = "v0"
                        },
                        new Tag()
                        {
                            Key = "k1_K123 :+-=._/",
                            Value = "v1_K123 :+-=._/"
                        },
                    }
                },
            };
            var putObjectTaggingOutput = client.PutObjectTagging(putObjectTaggingInput);
            Assert.AreEqual(200, putObjectTaggingOutput.StatusCode);
            Assert.Greater(putObjectTaggingOutput.RequestID.Length,0);
            Assert.IsNull(putObjectTaggingOutput.VersionID);

            var getObjectTaggingInput = new GetObjectTaggingInput()
            {
                Bucket = bucket,
                Key = key,
            };
            var getObjectTaggingOutput = client.GetObjectTagging(getObjectTaggingInput);
            Assert.AreEqual(200, getObjectTaggingOutput.StatusCode);
            Assert.AreEqual(2, getObjectTaggingOutput.TagSet.Tags.Length);
            Assert.AreEqual("k0", getObjectTaggingOutput.TagSet.Tags[0].Key);
            Assert.AreEqual("v0", getObjectTaggingOutput.TagSet.Tags[0].Value);
            Assert.AreEqual("k1_K123 :+-=._/", getObjectTaggingOutput.TagSet.Tags[1].Key);
            Assert.AreEqual("v1_K123 :+-=._/", getObjectTaggingOutput.TagSet.Tags[1].Value);
            Assert.IsNull(getObjectTaggingOutput.VersionID);
            
            var deleteObjectTaggingInput = new DeleteObjectTaggingInput()
            {
                Bucket = bucket,
                Key = key,
            };
            var deleteObjectTaggingOutput = client.DeleteObjectTagging(deleteObjectTaggingInput);
            Assert.AreEqual(204, deleteObjectTaggingOutput.StatusCode);
            Assert.IsNull(putObjectTaggingOutput.VersionID);
            
            getObjectTaggingOutput = client.GetObjectTagging(getObjectTaggingInput);
            Assert.AreEqual(200, getObjectTaggingOutput.StatusCode);
            Assert.AreEqual(0, getObjectTaggingOutput.TagSet.Tags.Length);

            ClearEnv.ClearDotNetSdkObj(client, bucket);
            Assert.DoesNotThrow(() => client.DeleteBucket(new DeleteBucketInput
            {
                Bucket = bucket
            }));
        }

        [Test]
        public void TestAbnormal()
        {
            var env = new TestEnv();
            var client = env.PrepareClientBuilder().SetSocketTimeout(360 * 1000).SetConnectionTimeout(360 * 1000)
                .Build();
            var bucket = Util.GenerateBucketName("bkt-obj-tagging-abnormal");
            var key = Util.GenerateObjectName("abnormal");
            var data = "hello world";

            var createBucketInput = new CreateBucketInput
            {
                Bucket = bucket
            };
            Assert.DoesNotThrow(() => client.CreateBucket(createBucketInput));

            Util.PutObjectFromStringAndCheckResponse(client, bucket, key, data);
            
            var putObjectTaggingInput = new PutObjectTaggingInput()
            {
                Bucket = bucket,
                Key = key,
                TagSet = new TagSet()
                {
                    Tags = new Tag[]
                    {
                        new Tag()
                        {
                            Key = "k0",
                            Value = "v0"
                        },
                        new Tag()
                        {
                            Key = "k0",
                            Value = "v0"
                        },
                    }
                },
            };
            var ex = Assert.Throws<TosServerException>(() => client.PutObjectTagging(putObjectTaggingInput));
            Assert.AreEqual(400, ex.StatusCode);
            Assert.AreEqual("InvalidArgument", ex.Code);

         
            putObjectTaggingInput = new PutObjectTaggingInput()
            {
                Bucket = "not-exist",
                Key = key,
                TagSet = new TagSet()
                {
                    Tags = new Tag[]
                    {
                        new Tag()
                        {
                            Key = "k0",
                            Value = "v0"
                        },
                    }
                },
            };
            ex = Assert.Throws<TosServerException>(() => client.PutObjectTagging(putObjectTaggingInput));
            Assert.AreEqual(404, ex.StatusCode);
            Assert.AreEqual("NoSuchBucket", ex.Code);
            

            putObjectTaggingInput = new PutObjectTaggingInput()
            {
                Bucket = bucket,
                Key = "not-exist-key",
                TagSet = new TagSet()
                {
                    Tags = new Tag[]
                    {
                        new Tag()
                        {
                            Key = "k0",
                            Value = "v0"
                        },
                    }
                },
            };
            ex = Assert.Throws<TosServerException>(() => client.PutObjectTagging(putObjectTaggingInput));
            Assert.AreEqual(404, ex.StatusCode);
            Assert.AreEqual("NoSuchKey", ex.Code);
            
            putObjectTaggingInput = new PutObjectTaggingInput()
            {
                Bucket = bucket,
                Key = key,
            };
            var cex = Assert.Throws<TosClientException>(() => client.PutObjectTagging(putObjectTaggingInput));
            Assert.AreEqual("empty tag set is set for put object tagging", cex.Message);
            
            putObjectTaggingInput = new PutObjectTaggingInput()
            {
                Bucket = bucket,
                Key = key,
                TagSet = new TagSet()
            };
            cex = Assert.Throws<TosClientException>(() => client.PutObjectTagging(putObjectTaggingInput));
            Assert.AreEqual("empty tag set is set for put object tagging", cex.Message);
            
            putObjectTaggingInput = new PutObjectTaggingInput()
            {
                Bucket = bucket,
                Key = key,
                TagSet = new TagSet()
                {
                    Tags = new Tag[]{}
                },
            };
            cex = Assert.Throws<TosClientException>(() => client.PutObjectTagging(putObjectTaggingInput));
            Assert.AreEqual("empty tag set is set for put object tagging", cex.Message);
            
            putObjectTaggingInput = new PutObjectTaggingInput()
            {
                Bucket = bucket,
                Key = key,
                TagSet = new TagSet()
                {
                    Tags = new Tag[]
                    {
                        new Tag()
                        {
                            Key = "",
                            Value = "v0"
                        },
                    }
                },
            };
            cex = Assert.Throws<TosClientException>(() => client.PutObjectTagging(putObjectTaggingInput));
            Assert.AreEqual("empty tag value or key is set for put object tagging", cex.Message);
            
            ClearEnv.ClearDotNetSdkObj(client, bucket);
            Assert.DoesNotThrow(() => client.DeleteBucket(new DeleteBucketInput
            {
                Bucket = bucket
            }));
        }
    }
}