using System.Collections.Generic;
using TOS.Model;
using NUnit.Framework;
using TOS.Error;

namespace TestTOS
{
    [TestFixture]
    public class TestBucket
    {
        [Test]
        public void TestHeadNonExistentBucket()
        {
            var client = new TestEnv().PrepareClient();
            var headBucketInput = new HeadBucketInput
            {
                Bucket = "non-existent-bucket"
            };
            
            var ex = Assert.Throws<TosServerException>(() => client.HeadBucket(headBucketInput));
            Assert.AreEqual(404, ex.StatusCode);
        }
        
        [Test]
        public void TestOnlyBucketNameV2()
        {
            var env = new TestEnv();
            var client = env.PrepareClient();
            var bucket = Util.GenerateBucketName("only-bucket-name");

            try
            {
                var createBucketInput = new CreateBucketInput
                {
                    Bucket = bucket
                };
                Assert.DoesNotThrow(() => client.CreateBucket(createBucketInput));
            
                Util.CheckBucketMeta(client, bucket, env.Region, StorageClassType.StorageClassStandard);
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
        public void TestAllParamsV2()
        {
            var env = new TestEnv();
            var client = env.PrepareClient();
            var bucket = Util.GenerateBucketName("create-bucket-all-params");

            try
            {
                var createBucketInput = new CreateBucketInput
                {
                    Bucket = bucket,
                    ACL = ACLType.ACLPrivate,
                    StorageClass = StorageClassType.StorageClassStandard,
                    AzRedundancy = AzRedundancyType.AzRedundancySingleAz
                };
                Assert.DoesNotThrow(() => client.CreateBucket(createBucketInput));
            
                Util.CheckBucketMeta(client, bucket, env.Region, StorageClassType.StorageClassStandard);
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
        public void TestAllParams3Az()
        {
            var env = new TestEnv();
            var client = env.PrepareClient();
            var bucket = Util.GenerateBucketName("create-bucket-all-params-3az");

            try
            {
                var createBucketInput = new CreateBucketInput
                {
                    Bucket = bucket,
                    ACL = ACLType.ACLPrivate,
                    StorageClass = StorageClassType.StorageClassStandard,
                    AzRedundancy = AzRedundancyType.AzRedundancyMultiAz
                };
                Assert.DoesNotThrow(() => client.CreateBucket(createBucketInput));
            
                Util.CheckBucketMeta(client, bucket, env.Region, 
                    StorageClassType.StorageClassStandard, AzRedundancyType.AzRedundancyMultiAz);
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
        public void TestDeleteNoneExistBucketV2()
        {
            var env = new TestEnv();
            var bucket = "this-is-a-none-exist-bucket";
            var client = env.PrepareClient();
            
            var deleteBucketInput = new DeleteBucketInput
            {
                Bucket = bucket
            };
            
            var ex = Assert.Throws<TosServerException>(() => client.DeleteBucket(deleteBucketInput));
            Assert.AreEqual(404, ex.StatusCode);
        }
        
        [Test]
        public void TestDeleteBucketV2()
        {
            var env = new TestEnv();
            var client = env.PrepareClient();
            var bucket = Util.GenerateBucketName("delete-bucket");

            var createBucketInput = new CreateBucketInput
            {
                Bucket = bucket
            };
            Assert.DoesNotThrow(() => client.CreateBucket(createBucketInput));
                
            var deleteBucketInput = new DeleteBucketInput
            {
                Bucket = bucket
            };
            Assert.DoesNotThrow(() => client.DeleteBucket(deleteBucketInput));
        }
        
        [Test]
        public void TestListBucketV2()
        {
            var env = new TestEnv();
            var client = env.PrepareClientBuilder().SetSocketTimeout(360*1000).SetConnectionTimeout(360*1000).Build();
            var bucket = Util.GenerateBucketName("delete-bucket");
            
            try
            {
                var createBucketInput = new CreateBucketInput
                {
                    Bucket = bucket,
                    ACL = ACLType.ACLPrivate,
                    StorageClass = StorageClassType.StorageClassStandard,
                    AzRedundancy = AzRedundancyType.AzRedundancySingleAz
                };
                Assert.DoesNotThrow(() => client.CreateBucket(createBucketInput));
                
                var listOutput = client.ListBuckets();
                Assert.AreEqual(200,listOutput.StatusCode);
                var testBucket = new List<string>() { };
                foreach (var b in listOutput.Buckets)
                {
                    if (b.Name != bucket)
                    {
                        continue;
                    }
                    testBucket.Add(b.Name);
                }
                Assert.AreEqual(1, testBucket.Count);
                Assert.AreEqual(bucket, testBucket[0]);
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
        
    }
}