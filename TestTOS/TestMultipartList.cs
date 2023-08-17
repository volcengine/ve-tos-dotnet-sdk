using System.Collections.Generic;
using NUnit.Framework;
using TOS;
using TOS.Error;
using TOS.Model;

namespace TestTOS
{
    [TestFixture]
    public class TestMultipartList
    {
        [Test]
        public void TestNormal()
        {
            var env = new TestEnv();
            var client = env.PrepareClient();
            var bucket = Util.GenerateBucketName("multipart-list-basic");
            var key = Util.GenerateObjectName("normal");
            var prefix1 = Util.GenerateRandomStr(5) + "/";
            var prefix2 = Util.GenerateRandomStr(5) + "/";
            
            var createBucketInput = new CreateBucketInput
            {
                Bucket = bucket
            };
            Assert.DoesNotThrow(() => client.CreateBucket(createBucketInput));
            
            var keys = new List<string>() { };
            var uploadIds = new List<string>() { };
            var keysAddUploadIds = new List<string>() { };
            for (int i = 0; i < 100; i++)
            {
                keys.Add(prefix1 + Util.GenerateObjectName("normal"));
            }
            
            foreach (var k in keys)
            {
                // 初始化段
                var createMultipartUploadInput = new CreateMultipartUploadInput
                {
                    Bucket = bucket,
                    Key = k
                };
                var createMultipartUploadOutput = client.CreateMultipartUpload(createMultipartUploadInput);
                Assert.Greater(createMultipartUploadOutput.RequestID.Length, 0);
                Assert.Greater(createMultipartUploadOutput.UploadID.Length, 0);
                uploadIds.Add(createMultipartUploadOutput.UploadID);
                keysAddUploadIds.Add(k + createMultipartUploadOutput.UploadID);
            }
            
            var keysAddUploadIdsFromServer = new List<string>() { };
            var listObjectsInput = new ListMultipartUploadsInput()
            {
                Bucket = bucket,
                MaxUploads = 10,
                Prefix = prefix1
            };
            while (true)
            {
                var listMultipartUploadsOutput = client.ListMultipartUploads(listObjectsInput);
                foreach (var upload in listMultipartUploadsOutput.Uploads)
                {
                    keysAddUploadIdsFromServer.Add(upload.Key+upload.UploadID);
                }

                if (!listMultipartUploadsOutput.IsTruncated)
                {
                    break;
                }
                
                listObjectsInput.KeyMarker = listMultipartUploadsOutput.NextKeyMarker;
                listObjectsInput.UploadIDMarker = listMultipartUploadsOutput.UploadIDMarker;
            }
            
            var keysAddUploadIds2 = keysAddUploadIds.GetRange(50, 50);
            keysAddUploadIds.Sort();
            keysAddUploadIdsFromServer.Sort();
            Assert.AreEqual(keysAddUploadIds, keysAddUploadIdsFromServer);
            
            
            // 取消前50个段
            for (int i = 0; i < 50; i++)
            {
                var input = new AbortMultipartUploadInput()
                {
                    Bucket = bucket,
                    Key = keys[i],
                    UploadID = uploadIds[i]
                };
                var output = client.AbortMultipartUpload(input);
                Assert.Greater(output.RequestID.Length, 0);
            }
                
            // 查询后50个段
            keys = keys.GetRange(50, 50);
            keysAddUploadIdsFromServer = new List<string>() { };

            listObjectsInput = new ListMultipartUploadsInput()
            {
                Bucket = bucket,
                MaxUploads = 10,
                Prefix = prefix1
            };
            while (true)
            {
                var listMultipartUploadsOutput = client.ListMultipartUploads(listObjectsInput);
                foreach (var upload in listMultipartUploadsOutput.Uploads)
                {
                    keysAddUploadIdsFromServer.Add(upload.Key+upload.UploadID);
                }

                if (!listMultipartUploadsOutput.IsTruncated)
                {
                    break;
                }
                
                listObjectsInput.KeyMarker = listMultipartUploadsOutput.NextKeyMarker;
                listObjectsInput.UploadIDMarker = listMultipartUploadsOutput.UploadIDMarker;
            }
            
            keysAddUploadIds2.Sort();
            keysAddUploadIdsFromServer.Sort();
            Assert.AreEqual(keysAddUploadIds2, keysAddUploadIdsFromServer);
            
            keys = new List<string>() { };
            keysAddUploadIds = new List<string>() { };
            var infixs = new List<string>()
            {
                "abc/","abc/123/","bcd/","bcd/456/","cde/","cde/789/"
            };

            foreach (var infix in infixs)
            {
                for (int i = 0; i < 3; i++)
                {
                    var tmpKey = prefix2 + infix + Util.GenerateObjectName("normal");
                    keys.Add(tmpKey);
                    
                    // 初始化段
                    var createMultipartUploadInput = new CreateMultipartUploadInput
                    {
                        Bucket = bucket,
                        Key = tmpKey
                    };
                    var createMultipartUploadOutput = client.CreateMultipartUpload(createMultipartUploadInput);
                    Assert.Greater(createMultipartUploadOutput.RequestID.Length, 0);
                    Assert.Greater(createMultipartUploadOutput.UploadID.Length, 0);
                    uploadIds.Add(createMultipartUploadOutput.UploadID);
                    keysAddUploadIds.Add(tmpKey + createMultipartUploadOutput.UploadID);
                }
            }
            
            keysAddUploadIdsFromServer = new List<string>(){};
            var commonPrefixes = new List<string>(){ prefix2 };

            while (commonPrefixes.Count > 0)
            {
                var prefix = commonPrefixes[0];
                commonPrefixes = commonPrefixes.GetRange(1, commonPrefixes.Count - 1);
                ListByPrefix(client, bucket, prefix, keysAddUploadIdsFromServer, commonPrefixes);
            }
            
            keysAddUploadIdsFromServer.Sort();
            keysAddUploadIds.Sort();
            Assert.AreEqual(keysAddUploadIds, keysAddUploadIdsFromServer);
        }
        
        private static void ListByPrefix(ITosClient client, string bucket, string prefix, List<string> keysAddUploadIdsFromServer,
            List<string> commonPrefixes)
        {
            var input = new ListMultipartUploadsInput()
            {
                Bucket = bucket,
                MaxUploads = 1000,
                Prefix = prefix,
                Delimiter = "/"
            };

            while (true)
            {
                var output = client.ListMultipartUploads(input);

                foreach (var upload in output.Uploads)
                {
                    keysAddUploadIdsFromServer.Add(upload.Key+upload.UploadID);
                }
                
                foreach (var commonPrefix in output.CommonPrefixes)
                {
                    commonPrefixes.Add(commonPrefix.Prefix);
                }

                if (!output.IsTruncated)
                {
                    break;
                }

                input.KeyMarker = output.NextKeyMarker;
                input.UploadIDMarker = output.UploadIDMarker;
            }
        }

        [Test]
        public void TestAbnormal()
        {
            var env = new TestEnv();
            var client = env.PrepareClient();
            var bucket = Util.GenerateBucketName("multipart-list-basic");
            var key = Util.GenerateObjectName("normal");
            
            var createBucketInput = new CreateBucketInput
            {
                Bucket = bucket
            };
            Assert.DoesNotThrow(() => client.CreateBucket(createBucketInput));

            var createMultipartUploadInput = new CreateMultipartUploadInput()
            {
                Bucket = "no-exist-bucket",
                Key = key
            };
            var ex = Assert.Throws<TosServerException>(() => client.CreateMultipartUpload(createMultipartUploadInput));
            Assert.AreEqual(404, ex.StatusCode);
            Assert.AreEqual("NoSuchBucket", ex.Code);

            var listMultipartUploadsInput = new ListMultipartUploadsInput()
            {
                Bucket = "no-exist-bucket",
                MaxUploads = 10
            };
            ex = Assert.Throws<TosServerException>(() => client.ListMultipartUploads(listMultipartUploadsInput));
            Assert.AreEqual(404, ex.StatusCode);
            Assert.AreEqual("NoSuchBucket", ex.Code);
            
            var uploadPartInput = new UploadPartInput()
            {
                Bucket = "no-exist-bucket",
                Key = "123",
                UploadID = "123",
                Content = Util.ConvertStringToStream("hello world")
            };
            ex = Assert.Throws<TosServerException>(() => client.UploadPart(uploadPartInput));
            Assert.AreEqual(404, ex.StatusCode);
            Assert.AreEqual("NoSuchBucket", ex.Code);
            
            var abortMultipartUploadInput = new AbortMultipartUploadInput()
            {
                Bucket = "no-exist-bucket",
                Key = "123",
                UploadID = "123",
            };
            ex = Assert.Throws<TosServerException>(() => client.AbortMultipartUpload(abortMultipartUploadInput));
            Assert.AreEqual(404, ex.StatusCode);
            Assert.AreEqual("NoSuchBucket", ex.Code);
            
            createMultipartUploadInput = new CreateMultipartUploadInput
            {
                Bucket = bucket,
                Key = key
            };
            var createMultipartUploadOutput = client.CreateMultipartUpload(createMultipartUploadInput);
            Assert.Greater(createMultipartUploadOutput.RequestID.Length, 0);
            Assert.Greater(createMultipartUploadOutput.UploadID.Length, 0);
            
            uploadPartInput = new UploadPartInput()
            {
                Bucket = bucket,
                Key = Util.GenerateRandomStr(400),
                UploadID = createMultipartUploadOutput.UploadID,
                Content = Util.ConvertStringToStream("hello world")
            };
            ex = Assert.Throws<TosServerException>(() => client.UploadPart(uploadPartInput));
            Assert.AreEqual(404, ex.StatusCode);
            Assert.AreEqual("NoSuchUpload", ex.Code);
            
            uploadPartInput = new UploadPartInput()
            {
                Bucket = bucket,
                Key = key,
                UploadID = "123",
                Content = Util.ConvertStringToStream("hello world")
            };
            ex = Assert.Throws<TosServerException>(() => client.UploadPart(uploadPartInput));
            Assert.AreEqual(404, ex.StatusCode);
            Assert.AreEqual("NoSuchUpload", ex.Code);
            
            var listPartsInput = new ListPartsInput()
            {
                Bucket = bucket,
                Key = Util.GenerateRandomStr(400),
                UploadID = createMultipartUploadOutput.UploadID,
            };
            ex = Assert.Throws<TosServerException>(() => client.ListParts(listPartsInput));
            Assert.AreEqual(404, ex.StatusCode);
            Assert.AreEqual("NoSuchUpload", ex.Code);
            
            listPartsInput = new ListPartsInput()
            {
                Bucket = bucket,
                Key = key,
                UploadID = "123",
            };
            ex = Assert.Throws<TosServerException>(() => client.ListParts(listPartsInput));
            Assert.AreEqual(404, ex.StatusCode);
            Assert.AreEqual("NoSuchUpload", ex.Code);
            
            var completeMultipartUploadInput = new CompleteMultipartUploadInput()
            {
                Bucket = bucket,
                Key = Util.GenerateRandomStr(400),
                UploadID = createMultipartUploadOutput.UploadID,
                Parts = new UploadedPart[]{new UploadedPart(){PartNumber = 1, ETag = "123"}}
            };
            ex = Assert.Throws<TosServerException>(() => client.CompleteMultipartUpload(completeMultipartUploadInput));
            Assert.AreEqual(404, ex.StatusCode);
            Assert.AreEqual("NoSuchUpload", ex.Code);
            
            completeMultipartUploadInput = new CompleteMultipartUploadInput()
            {
                Bucket = bucket,
                Key = key,
                UploadID = "123",
                Parts = new UploadedPart[]{new UploadedPart(){PartNumber = 1, ETag = "123"}}
            };
            ex = Assert.Throws<TosServerException>(() => client.CompleteMultipartUpload(completeMultipartUploadInput));
            Assert.AreEqual(404, ex.StatusCode);
            Assert.AreEqual("NoSuchUpload", ex.Code);
            
            abortMultipartUploadInput = new AbortMultipartUploadInput()
            {
                Bucket = bucket,
                Key = Util.GenerateRandomStr(400),
                UploadID = createMultipartUploadOutput.UploadID,
            };
            ex = Assert.Throws<TosServerException>(() => client.AbortMultipartUpload(abortMultipartUploadInput));
            Assert.AreEqual(404, ex.StatusCode);
            Assert.AreEqual("NoSuchUpload", ex.Code);
            
            abortMultipartUploadInput = new AbortMultipartUploadInput()
            {
                Bucket = bucket,
                Key = key,
                UploadID = "123",
            };
            ex = Assert.Throws<TosServerException>(() => client.AbortMultipartUpload(abortMultipartUploadInput));
            Assert.AreEqual(404, ex.StatusCode);
            Assert.AreEqual("NoSuchUpload", ex.Code);
        }
    }
}