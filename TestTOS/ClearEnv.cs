using NUnit.Framework;
using TOS;
using TOS.Model;

namespace TestTOS
{
    public class ClearEnv
    {
        public static void ClearDotNetSdkObj(ITosClient client, string bucket)
        {
            var listObjectsInput = new ListObjectsInput()
            {
                Bucket = bucket
            };
            var listObjectsOutput = client.ListObjects(listObjectsInput);

            foreach (var l in listObjectsOutput.Contents)
            {
                var deleteObjectInput = new DeleteObjectInput()
                {
                    Bucket = bucket,
                    Key = l.Key
                };
                client.DeleteObject(deleteObjectInput);
            }
        }

        public static void ClearDotNetSdkPart(ITosClient client, string bucket)
        {
            var listMultipartUploadsInput = new ListMultipartUploadsInput()
            {
                Bucket = bucket,
                MaxUploads = 10
            };
            
            while (true)
            {
                var listMultipartUploadsOutput = client.ListMultipartUploads(listMultipartUploadsInput);
                foreach (var upload in listMultipartUploadsOutput.Uploads)
                {
                    var abortMultipartUploadInput = new AbortMultipartUploadInput()
                    {
                        Bucket = bucket,
                        Key = upload.Key,
                        UploadID = upload.UploadID
                    };

                    client.AbortMultipartUpload(abortMultipartUploadInput);
                }

                if (!listMultipartUploadsOutput.IsTruncated)
                {
                    break;
                }
                
                listMultipartUploadsInput.KeyMarker = listMultipartUploadsOutput.NextKeyMarker;
                listMultipartUploadsInput.UploadIDMarker = listMultipartUploadsOutput.UploadIDMarker;
            }
        }

        [Test]
        public void ClearDotNetSdkBuckets()
        {
            var env = new TestEnv();
            var client = env.PrepareClientBuilder().SetSocketTimeout(360*1000).SetConnectionTimeout(360*1000).Build();
            var listOutput = client.ListBuckets();
            foreach (var b in listOutput.Buckets)
            {
                if (!b.Name.StartsWith(Util.BucketPrefix))
                {
                    continue;
                }

                var deleteClient = env.PrepareClientBuilder().SetEndpoint(b.ExtranetEndpoint).SetRegion(b.Location).SetSocketTimeout(360*1000).SetConnectionTimeout(360*1000).Build();
                ClearDotNetSdkObj(deleteClient, b.Name);
                ClearDotNetSdkPart(deleteClient, b.Name);
                deleteClient.DeleteBucket(new DeleteBucketInput() { Bucket = b.Name });
            }
        }
    }
}