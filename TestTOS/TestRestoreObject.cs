using System;
using System.Collections.Generic;
using System.Threading;
using NUnit.Framework;
using TOS.Error;
using TOS.Model;

namespace TestTOS
{
    [TestFixture]
    public class TestRestoreObject
    {
        [Test]
        public void TestNormal()
        {
            var env = new TestEnv();
            var client = env.PrepareClient();
            var bucket = Util.GenerateBucketName("put-restore-object-normal");
            var keys = new List<string>() { "key123-archive" , "key123-cold-archive", "key123-deep-cold-archive"};

            try
            {
                var createBucketInput = new CreateBucketInput
                {
                    Bucket = bucket
                };
                Assert.DoesNotThrow(() => client.CreateBucket(createBucketInput));

                Util.PutRandomObject(client, bucket, keys[0], 4096, StorageClassType.StorageClassArchive);
                Util.PutRandomObject(client, bucket, keys[1], 4096, StorageClassType.StorageClassColdArchive);
                Util.PutRandomObject(client, bucket, keys[2], 4096, StorageClassType.StorageClassDeepColdArchive);
                Util.WaitUntilObjectExist(client, bucket, keys[0]);
                Util.WaitUntilObjectExist(client, bucket, keys[1]);
                Util.WaitUntilObjectExist(client, bucket, keys[2]);

                var restoreObjectInput = new RestoreObjectInput
                {
                    Bucket = bucket,
                    Key = keys[0],
                    Days = 1,
                };
                var restoreObjectOutput = client.RestoreObject(restoreObjectInput);
                Assert.AreEqual(202, restoreObjectOutput.StatusCode);
                
                Thread.Sleep(60000);
                
                restoreObjectOutput = client.RestoreObject(restoreObjectInput);
                Assert.AreEqual(200, restoreObjectOutput.StatusCode);

                restoreObjectInput.Key = keys[1];
                restoreObjectInput.RestoreJobParameters = new RestoreJobParameters()
                {
                    Tier = TierType.Expedited,
                };
                restoreObjectOutput = client.RestoreObject(restoreObjectInput);
                Assert.AreEqual(202, restoreObjectOutput.StatusCode);
                
                restoreObjectInput.Key = keys[2];
                restoreObjectInput.RestoreJobParameters = new RestoreJobParameters()
                {
                    Tier = TierType.Bulk,
                };
                restoreObjectOutput = client.RestoreObject(restoreObjectInput);
                Assert.AreEqual(202, restoreObjectOutput.StatusCode);
            }
            finally
            {
                foreach (var key in keys)
                {
                    var deleteObjectInput = new DeleteObjectInput
                    {
                        Bucket = bucket,
                        Key = key
                    };
                    client.DeleteObject(deleteObjectInput);
                }

                
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
            var client = env.PrepareClient();
            var bucket = Util.GenerateBucketName("put-restore-object-abnormal");
            var keys = new List<string>() { "key123-archive" , "key123-cold-archive", "key123-deep-cold-archive"};

            try
            {
                var createBucketInput = new CreateBucketInput
                {
                    Bucket = bucket
                };
                Assert.DoesNotThrow(() => client.CreateBucket(createBucketInput));

                Util.PutRandomObject(client, bucket, keys[0], 4096, StorageClassType.StorageClassArchive);
                Util.PutRandomObject(client, bucket, keys[1], 4096, StorageClassType.StorageClassColdArchive);
                Util.PutRandomObject(client, bucket, keys[2], 4096, StorageClassType.StorageClassDeepColdArchive);
                Util.WaitUntilObjectExist(client, bucket, keys[0]);
                Util.WaitUntilObjectExist(client, bucket, keys[1]);
                Util.WaitUntilObjectExist(client, bucket, keys[2]);

                var restoreObjectInput = new RestoreObjectInput { };
                var clientEx = Assert.Throws<TosClientException>(() => client.RestoreObject(restoreObjectInput));
                Assert.AreEqual("invalid bucket name, the length must be [3, 63]", clientEx.Message);
                
                restoreObjectInput = new RestoreObjectInput { Bucket = bucket};
                clientEx = Assert.Throws<TosClientException>(() => client.RestoreObject(restoreObjectInput));
                Assert.AreEqual("invalid object name", clientEx.Message);
                
                restoreObjectInput = new RestoreObjectInput
                {
                    Bucket = bucket,
                    Key = keys[0],
                };
                var serverEx = Assert.Throws<TosServerException>(() => client.RestoreObject(restoreObjectInput));
                Assert.AreEqual(400, serverEx.StatusCode);
                Assert.AreEqual("InvalidArgument", serverEx.Code);

                restoreObjectInput.Days = 366;
                serverEx = Assert.Throws<TosServerException>(() => client.RestoreObject(restoreObjectInput));
                Assert.AreEqual(400, serverEx.StatusCode);
                Assert.AreEqual("InvalidArgument", serverEx.Code);
                
                restoreObjectInput = new RestoreObjectInput
                {
                    Bucket = bucket,
                    Key = keys[0],
                    Days = 365,
                    RestoreJobParameters = new RestoreJobParameters()
                    {
                        Tier = TierType.Bulk
                    }
                };
                serverEx = Assert.Throws<TosServerException>(() => client.RestoreObject(restoreObjectInput));
                Assert.AreEqual(400, serverEx.StatusCode);
                Assert.AreEqual("MalformedBody", serverEx.Code);

                restoreObjectInput.RestoreJobParameters.Tier = TierType.Expedited;
                serverEx = Assert.Throws<TosServerException>(() => client.RestoreObject(restoreObjectInput));
                Assert.AreEqual(400, serverEx.StatusCode);
                Assert.AreEqual("MalformedBody", serverEx.Code);
                
                restoreObjectInput = new RestoreObjectInput
                {
                    Bucket = bucket,
                    Key = keys[1],
                    Days = 1,
                };
                var restoreObjectOutput = client.RestoreObject(restoreObjectInput);
                Assert.AreEqual(202, restoreObjectOutput.StatusCode);
                
                serverEx = Assert.Throws<TosServerException>(() => client.RestoreObject(restoreObjectInput));
                Assert.AreEqual(409, serverEx.StatusCode);
                Assert.AreEqual("RestoreAlreadyInProgress", serverEx.Code);
                
                restoreObjectInput = new RestoreObjectInput
                {
                    Bucket = bucket,
                    Key = keys[2],
                    Days = 1,
                    RestoreJobParameters = new RestoreJobParameters()
                    {
                        Tier = TierType.Expedited
                    }
                };
                serverEx = Assert.Throws<TosServerException>(() => client.RestoreObject(restoreObjectInput));
                Assert.AreEqual(400, serverEx.StatusCode);
                Assert.AreEqual("MalformedBody", serverEx.Code);
                
                
            }
            finally
            {
                foreach (var key in keys)
                {
                    var deleteObjectInput = new DeleteObjectInput
                    {
                        Bucket = bucket,
                        Key = key
                    };
                    client.DeleteObject(deleteObjectInput);
                }

                
                var deleteBucketInput = new DeleteBucketInput
                {
                    Bucket = bucket
                };
                client.DeleteBucket(deleteBucketInput);
            }
        }

    }
}