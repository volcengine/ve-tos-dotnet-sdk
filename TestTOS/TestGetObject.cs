using NUnit.Framework;
using TOS.Error;
using TOS.Model;

namespace TestTOS
{
    [TestFixture]
    public class TestGetObject
    {
        [Test]
        public void TestNormal()
        {
            var env = new TestEnv();
            var client = env.PrepareClientBuilder().SetSocketTimeout(360 * 1000).SetConnectionTimeout(360 * 1000)
                .Build();
            var bucket = Util.GenerateBucketName("get-obj-basic");
            var key = Util.GenerateObjectName("normal");
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
                Key = key
            };
            var getObjectOutput = client.GetObject(getObjectInput);
            Assert.Greater(getObjectOutput.RequestID.Length, 0);
            Assert.AreEqual(data, Util.ReadStreamAsString(getObjectOutput.Content));
            getObjectOutput.Content.Close();
            
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
            var bucket = Util.GenerateBucketName("get-obj-basic");
            var key = Util.GenerateObjectName("abnormal");
            var data = "hello world";

            var createBucketInput = new CreateBucketInput
            {
                Bucket = bucket
            };
            Assert.DoesNotThrow(() => client.CreateBucket(createBucketInput));

            var getObjectInput = new GetObjectInput()
            {
                Bucket = bucket,
                Key = key
            };
            var ex = Assert.Throws<TosServerException>(() => client.GetObject(getObjectInput));
            Assert.AreEqual(404, ex.StatusCode);
            Assert.AreEqual("NoSuchKey", ex.Code);
            
            Assert.DoesNotThrow(() => client.DeleteBucket(new DeleteBucketInput
            {
                Bucket = bucket
            }));
        }

    }
}