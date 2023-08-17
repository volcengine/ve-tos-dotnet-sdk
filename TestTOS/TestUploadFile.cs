using NUnit.Framework;

namespace TestTOS
{
    [TestFixture]
    public class TestUploadFile
    {
        [Test]
        public void TestNormal()
        {
            // var env = new TestEnv();
            // var client = env.PrepareClient();
            // var bucket = Util.GenerateBucketName("upload-file-basic");
            // var key = Util.GenerateObjectName("normal");
            // var sampleFilePath = "./temp/" + Util.GenerateRandomStr(10) + ".txt";
            //
            // var createBucketInput = new CreateBucketInput
            // {
            //     Bucket = bucket
            // };
            // Assert.DoesNotThrow(() => client.CreateBucket(createBucketInput));
            //
            // try
            // {
            //     Util.CreateRandomStringFile(sampleFilePath, 10000);
            //     var md5 = Util.CalculateMd5FromFile(sampleFilePath);
            //
            // }
            // finally
            // {
            //     if (File.Exists(sampleFilePath))
            //     {
            //         File.Delete(sampleFilePath);
            //     }
            // }
        }
    }
}