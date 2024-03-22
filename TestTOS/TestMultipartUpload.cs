using System;
using System.IO;
using System.Text;
using NUnit.Framework;
using TOS.Error;
using TOS.Model;

namespace TestTOS
{
    [TestFixture]
    public class TestMultipartUpload
    {
        [Test]
        public void TestNormal()
        {
            var env = new TestEnv();
            var client = env.PrepareClient();
            var bucket = Util.GenerateBucketName("multipart-upload-basic");
            var key1 = Util.GenerateObjectName("normal-1");
            var key2 = Util.GenerateObjectName("normal-2");
            var key3 = Util.GenerateObjectName("normal-3");
            var sampleFilePath = "./temp" + Util.GenerateRandomStr(10) + ".txt";
            var dstFilePath1 = sampleFilePath + "_bak1";
            var dstFilePath2 = sampleFilePath + "_bak2";
            var dstFilePath3 = sampleFilePath + "_bak3";

            var createBucketInput = new CreateBucketInput
            {
                Bucket = bucket
            };
            Assert.DoesNotThrow(() => client.CreateBucket(createBucketInput));

            // 初始化段
            var createMultipartUploadInput = new CreateMultipartUploadInput
            {
                Bucket = bucket,
                Key = key1
            };
            var createMultipartUploadOutput = client.CreateMultipartUpload(createMultipartUploadInput);
            Assert.Greater(createMultipartUploadOutput.RequestID.Length, 0);
            Assert.Greater(createMultipartUploadOutput.UploadID.Length, 0);
            var uploadId1 = createMultipartUploadOutput.UploadID;

            Util.CreateRandomStringFile(sampleFilePath, 10*1024*1024);
            var md5 = Util.CalculateMd5FromFile(sampleFilePath);
            var fileSize = new FileInfo(sampleFilePath).Length;
            var firstPartSize = 5 * 1024 * 1024;
            var partNumber1 = 1;
            var partNumber2 = 2;

            // 上传第一段
            string etag1;
            using (var fileStream = new FileStream(sampleFilePath, FileMode.Open, FileAccess.Read))
            {
                var uploadPartInput = new UploadPartInput
                {
                    Bucket = bucket,
                    Key = key1,
                    UploadID = uploadId1,
                    PartNumber = partNumber1,
                    Content = fileStream,
                    ContentLength = firstPartSize
                };

                var uploadPartOutput = client.UploadPart(uploadPartInput);
                Assert.Greater(uploadPartOutput.RequestID.Length, 0);
                etag1 = uploadPartOutput.ETag;
            }

            var parts = new UploadedPart[2];
            parts[0] = new UploadedPart { PartNumber = partNumber1, ETag = etag1 };

            // 上传第二段
            string etag2;
            using (var fileStream = new FileStream(sampleFilePath, FileMode.Open, FileAccess.Read))
            {
                fileStream.Seek(firstPartSize, SeekOrigin.Begin);
                var uploadPartInput = new UploadPartInput
                {
                    Bucket = bucket,
                    Key = key1,
                    UploadID = uploadId1,
                    PartNumber = partNumber2,
                    Content = fileStream,
                    ContentLength = fileSize - firstPartSize
                };
                var uploadPartOutput = client.UploadPart(uploadPartInput);
                Assert.Greater(uploadPartOutput.RequestID.Length, 0);
                etag2 = uploadPartOutput.ETag;
            }

            parts[1] = new UploadedPart { PartNumber = partNumber2, ETag = etag2 };


            var listPartsInput = new ListPartsInput
            {
                Bucket = bucket,
                Key = key1,
                UploadID = uploadId1
            };
            var listPartsOutput = client.ListParts(listPartsInput);
            Assert.AreEqual(2, listPartsOutput.Parts.Length);

            // 合并段
            var completeMultipartUploadInput = new CompleteMultipartUploadInput()
            {
                Bucket = bucket,
                Key = key1,
                UploadID = uploadId1,
                Parts = parts
            };
            var completeMultipartUploadOutput = client.CompleteMultipartUpload(completeMultipartUploadInput);
            Assert.Greater(completeMultipartUploadOutput.RequestID.Length, 0);
            
            var getObjectInput = new GetObjectInput
            {
                Bucket = bucket,
                Key = key1
            };
            var getObjectOutput = client.GetObject(getObjectInput);

            using (var fileStream = new FileStream(dstFilePath1, FileMode.Create, FileAccess.Write))
            {
                var dataStr = Util.ReadStreamAsString(getObjectOutput.Content);
                getObjectOutput.Content.Close();
                // 将数据转换为字节数组
                var buffer = Encoding.UTF8.GetBytes(dataStr);
                // 使用文件流写入数据
                fileStream.Write(buffer, 0, buffer.Length);
            }
            Assert.AreEqual(md5, Util.CalculateMd5FromFile(dstFilePath1));

            // 初始化段
            createMultipartUploadInput = new CreateMultipartUploadInput
            {
                Bucket = bucket,
                Key = key2
            };
            createMultipartUploadOutput = client.CreateMultipartUpload(createMultipartUploadInput);
            Assert.Greater(createMultipartUploadOutput.RequestID.Length, 0);
            Assert.Greater(createMultipartUploadOutput.UploadID.Length, 0);
            var uploadId2 = createMultipartUploadOutput.UploadID;
            
            // 上传第一段
            var uploadPartFromFileInput = new UploadPartFromFileInput()
            {
                Bucket = bucket,
                Key = key2,
                UploadID = uploadId2,
                PartNumber = partNumber1,
                FilePath = sampleFilePath,
                PartSize = firstPartSize
            };
            var uploadPartFromFileOutput = client.UploadPartFromFile(uploadPartFromFileInput);
            Assert.Greater(uploadPartFromFileOutput.RequestID.Length, 0);
            parts[0] = new UploadedPart { PartNumber = partNumber1, ETag = uploadPartFromFileOutput.ETag };

            // 上传第二段
            uploadPartFromFileInput = new UploadPartFromFileInput()
            {
                Bucket = bucket,
                Key = key2,
                UploadID = uploadId2,
                PartNumber = partNumber2,
                FilePath = sampleFilePath,
                Offset = firstPartSize
            };
            uploadPartFromFileOutput = client.UploadPartFromFile(uploadPartFromFileInput);
            Assert.Greater(uploadPartFromFileOutput.RequestID.Length, 0);
            parts[1] = new UploadedPart { PartNumber = partNumber2, ETag = uploadPartFromFileOutput.ETag };

            // 合并段
            completeMultipartUploadInput = new CompleteMultipartUploadInput()
            {
                Bucket = bucket,
                Key = key2,
                UploadID = uploadId2,
                Parts = parts
            };
            completeMultipartUploadOutput = client.CompleteMultipartUpload(completeMultipartUploadInput);
            Assert.Greater(completeMultipartUploadOutput.RequestID.Length, 0);
            
            getObjectInput = new GetObjectInput
            {
                Bucket = bucket,
                Key = key2
            };
            getObjectOutput = client.GetObject(getObjectInput);

            using (var fileStream = new FileStream(dstFilePath2, FileMode.Create, FileAccess.Write))
            {
                var dataStr = Util.ReadStreamAsString(getObjectOutput.Content);
                getObjectOutput.Content.Close();
                // 将数据转换为字节数组
                var buffer = Encoding.UTF8.GetBytes(dataStr);
                // 使用文件流写入数据
                fileStream.Write(buffer, 0, buffer.Length);
            }
            Assert.AreEqual(md5, Util.CalculateMd5FromFile(dstFilePath2));

            // 初始化段
            createMultipartUploadInput = new CreateMultipartUploadInput
            {
                Bucket = bucket,
                Key = key3
            };
            createMultipartUploadOutput = client.CreateMultipartUpload(createMultipartUploadInput);
            Assert.Greater(createMultipartUploadOutput.RequestID.Length, 0);
            Assert.Greater(createMultipartUploadOutput.UploadID.Length, 0);
            var uploadId3 = createMultipartUploadOutput.UploadID;
                
            // 上传第一段
            var offset = 0;
            var partSize = 5 * 1024 * 1024;
            var uploadPartCopyInput = new UploadPartCopyInput()
            {
                Bucket = bucket,
                Key = key3,
                UploadID = uploadId3,
                PartNumber = partNumber1,
                SrcBucket = bucket,
                SrcKey = key1,
                CopySourceRange = "bytes=" + offset + "-" + (partSize - 1).ToString()
            };
            var uploadPartCopyOutput = client.UploadPartCopy(uploadPartCopyInput);
            Assert.Greater(uploadPartCopyOutput.RequestID.Length, 0);
            parts[0] = new UploadedPart { PartNumber = partNumber1, ETag = uploadPartCopyOutput.ETag };
 
            // 上传第二段
            uploadPartCopyInput = new UploadPartCopyInput()
            {
                Bucket = bucket,
                Key = key3,
                UploadID = uploadId3,
                PartNumber = partNumber2,
                SrcBucket = bucket,
                SrcKey = key1,
                CopySourceRange = "bytes=" + partSize.ToString() + "-" + (fileSize - 1).ToString()
            };
            uploadPartCopyOutput = client.UploadPartCopy(uploadPartCopyInput);
            Assert.Greater(uploadPartCopyOutput.RequestID.Length, 0);
            parts[1] = new UploadedPart { PartNumber = partNumber2, ETag = uploadPartCopyOutput.ETag };
            
            // 合并段
            completeMultipartUploadInput = new CompleteMultipartUploadInput()
            {
                Bucket = bucket,
                Key = key3,
                UploadID = uploadId3,
                Parts = parts
            };
            completeMultipartUploadOutput = client.CompleteMultipartUpload(completeMultipartUploadInput);
            Assert.Greater(completeMultipartUploadOutput.RequestID.Length, 0);
            
            getObjectInput = new GetObjectInput
            {
                Bucket = bucket,
                Key = key3
            };
            getObjectOutput = client.GetObject(getObjectInput);

            using (var fileStream = new FileStream(dstFilePath3, FileMode.Create, FileAccess.Write))
            {
                var dataStr = Util.ReadStreamAsString(getObjectOutput.Content);
                getObjectOutput.Content.Close();
                // 将数据转换为字节数组
                var buffer = Encoding.UTF8.GetBytes(dataStr);
                // 使用文件流写入数据
                fileStream.Write(buffer, 0, buffer.Length);
            }
            Assert.AreEqual(md5, Util.CalculateMd5FromFile(dstFilePath3));
            
            if (File.Exists(dstFilePath1)) File.Delete(dstFilePath1);
            if (File.Exists(dstFilePath2)) File.Delete(dstFilePath2);
            if (File.Exists(dstFilePath3)) File.Delete(dstFilePath3);
            if (File.Exists(sampleFilePath)) File.Delete(sampleFilePath);
        }
        
        [Test]
        public void TestCallback()
        {
            var env = new TestEnv();
            var client = env.PrepareClient();
            var bucket = Util.GenerateBucketName("multipart-upload-basic");
            var key1 = Util.GenerateObjectName("normal-1");
            var key2 = Util.GenerateObjectName("normal-2");
            var key3 = Util.GenerateObjectName("normal-3");
            var sampleFilePath = "./temp" + Util.GenerateRandomStr(10) + ".txt";
            var dstFilePath1 = sampleFilePath + "_bak1";
            var dstFilePath2 = sampleFilePath + "_bak2";
            var dstFilePath3 = sampleFilePath + "_bak3";
            var invalidOriginInput =
                $"{{\"callbackUrl\":\"http://343545.xxxx.com\",\"callbackBody\":\"{{\\\"bucket\\\": ${{bucket}}, \\\"object\\\": ${{object}}, \\\"key1\\\": ${{x:key1}}}}\",\"callbackBodyType\":\"application/json\"}}";
            var originInput =
                $"{{\"callbackUrl\":\"{env.CallbackUrl}\",\"callbackBody\":\"{{\\\"bucket\\\": ${{bucket}}, \\\"object\\\": ${{object}}, \\\"key1\\\": ${{x:key1}}}}\",\"callbackBodyType\":\"application/json\"}}";
            var originVarInput = "{\"x:key1\" : \"ceshi\"}";

            var createBucketInput = new CreateBucketInput
            {
                Bucket = bucket
            };
            Assert.DoesNotThrow(() => client.CreateBucket(createBucketInput));

            // 初始化段
            var createMultipartUploadInput = new CreateMultipartUploadInput
            {
                Bucket = bucket,
                Key = key1
            };
            var createMultipartUploadOutput = client.CreateMultipartUpload(createMultipartUploadInput);
            Assert.Greater(createMultipartUploadOutput.RequestID.Length, 0);
            Assert.Greater(createMultipartUploadOutput.UploadID.Length, 0);
            var uploadId1 = createMultipartUploadOutput.UploadID;

            Util.CreateRandomStringFile(sampleFilePath, 10*1024*1024);
            var md5 = Util.CalculateMd5FromFile(sampleFilePath);
            var fileSize = new FileInfo(sampleFilePath).Length;
            var firstPartSize = 5 * 1024 * 1024;
            var partNumber1 = 1;
            var partNumber2 = 2;

            // 上传第一段
            string etag1;
            using (var fileStream = new FileStream(sampleFilePath, FileMode.Open, FileAccess.Read))
            {
                var uploadPartInput = new UploadPartInput
                {
                    Bucket = bucket,
                    Key = key1,
                    UploadID = uploadId1,
                    PartNumber = partNumber1,
                    Content = fileStream,
                    ContentLength = firstPartSize
                };

                var uploadPartOutput = client.UploadPart(uploadPartInput);
                Assert.Greater(uploadPartOutput.RequestID.Length, 0);
                etag1 = uploadPartOutput.ETag;
            }

            var parts = new UploadedPart[2];
            parts[0] = new UploadedPart { PartNumber = partNumber1, ETag = etag1 };

            // 上传第二段
            string etag2;
            using (var fileStream = new FileStream(sampleFilePath, FileMode.Open, FileAccess.Read))
            {
                fileStream.Seek(firstPartSize, SeekOrigin.Begin);
                var uploadPartInput = new UploadPartInput
                {
                    Bucket = bucket,
                    Key = key1,
                    UploadID = uploadId1,
                    PartNumber = partNumber2,
                    Content = fileStream,
                    ContentLength = fileSize - firstPartSize
                };
                var uploadPartOutput = client.UploadPart(uploadPartInput);
                Assert.Greater(uploadPartOutput.RequestID.Length, 0);
                etag2 = uploadPartOutput.ETag;
            }

            parts[1] = new UploadedPart { PartNumber = partNumber2, ETag = etag2 };


            var listPartsInput = new ListPartsInput
            {
                Bucket = bucket,
                Key = key1,
                UploadID = uploadId1
            };
            var listPartsOutput = client.ListParts(listPartsInput);
            Assert.AreEqual(2, listPartsOutput.Parts.Length);

            // 合并段，回调失败
            var completeMultipartUploadInput = new CompleteMultipartUploadInput()
            {
                Bucket = bucket,
                Key = key1,
                UploadID = uploadId1,
                Parts = parts,
                Callback = Convert.ToBase64String(Encoding.UTF8.GetBytes(invalidOriginInput)),
                CallbackVar = Convert.ToBase64String(Encoding.UTF8.GetBytes(originVarInput))
            };
            
            // 回调失败
            var exServer = Assert.Throws<TosServerException>(() => client.CompleteMultipartUpload(completeMultipartUploadInput));
            Assert.AreEqual(203, exServer.StatusCode);
            Assert.AreEqual("CallbackFailed", exServer.Code);
            
            var getObjectInput = new GetObjectInput
            {
                Bucket = bucket,
                Key = key1
            };
            var getObjectOutput = client.GetObject(getObjectInput);

            using (var fileStream = new FileStream(dstFilePath1, FileMode.Create, FileAccess.Write))
            {
                var dataStr = Util.ReadStreamAsString(getObjectOutput.Content);
                getObjectOutput.Content.Close();
                // 将数据转换为字节数组
                var buffer = Encoding.UTF8.GetBytes(dataStr);
                // 使用文件流写入数据
                fileStream.Write(buffer, 0, buffer.Length);
            }
            Assert.AreEqual(md5, Util.CalculateMd5FromFile(dstFilePath1));

            // 初始化段
            createMultipartUploadInput = new CreateMultipartUploadInput
            {
                Bucket = bucket,
                Key = key2
            };
            createMultipartUploadOutput = client.CreateMultipartUpload(createMultipartUploadInput);
            Assert.Greater(createMultipartUploadOutput.RequestID.Length, 0);
            Assert.Greater(createMultipartUploadOutput.UploadID.Length, 0);
            var uploadId2 = createMultipartUploadOutput.UploadID;
            
            // 上传第一段
            var uploadPartFromFileInput = new UploadPartFromFileInput()
            {
                Bucket = bucket,
                Key = key2,
                UploadID = uploadId2,
                PartNumber = partNumber1,
                FilePath = sampleFilePath,
                PartSize = firstPartSize
            };
            var uploadPartFromFileOutput = client.UploadPartFromFile(uploadPartFromFileInput);
            Assert.Greater(uploadPartFromFileOutput.RequestID.Length, 0);
            parts[0] = new UploadedPart { PartNumber = partNumber1, ETag = uploadPartFromFileOutput.ETag };

            // 上传第二段
            uploadPartFromFileInput = new UploadPartFromFileInput()
            {
                Bucket = bucket,
                Key = key2,
                UploadID = uploadId2,
                PartNumber = partNumber2,
                FilePath = sampleFilePath,
                Offset = firstPartSize
            };
            uploadPartFromFileOutput = client.UploadPartFromFile(uploadPartFromFileInput);
            Assert.Greater(uploadPartFromFileOutput.RequestID.Length, 0);
            parts[1] = new UploadedPart { PartNumber = partNumber2, ETag = uploadPartFromFileOutput.ETag };

            // 合并段
            completeMultipartUploadInput = new CompleteMultipartUploadInput()
            {
                Bucket = bucket,
                Key = key2,
                UploadID = uploadId2,
                Parts = parts,
                Callback = Convert.ToBase64String(Encoding.UTF8.GetBytes(originInput)),
                CallbackVar = Convert.ToBase64String(Encoding.UTF8.GetBytes(originVarInput))
            };
            var completeMultipartUploadOutput = client.CompleteMultipartUpload(completeMultipartUploadInput);
            Assert.Greater(completeMultipartUploadOutput.RequestID.Length, 0);
            Assert.AreNotEqual("",completeMultipartUploadOutput.CallbackResult);
            Assert.True(completeMultipartUploadOutput.CallbackResult.Contains("ok"));
            
            getObjectInput = new GetObjectInput
            {
                Bucket = bucket,
                Key = key2
            };
            getObjectOutput = client.GetObject(getObjectInput);

            using (var fileStream = new FileStream(dstFilePath2, FileMode.Create, FileAccess.Write))
            {
                var dataStr = Util.ReadStreamAsString(getObjectOutput.Content);
                getObjectOutput.Content.Close();
                // 将数据转换为字节数组
                var buffer = Encoding.UTF8.GetBytes(dataStr);
                // 使用文件流写入数据
                fileStream.Write(buffer, 0, buffer.Length);
            }
            Assert.AreEqual(md5, Util.CalculateMd5FromFile(dstFilePath2));

            // 初始化段
            createMultipartUploadInput = new CreateMultipartUploadInput
            {
                Bucket = bucket,
                Key = key3
            };
            createMultipartUploadOutput = client.CreateMultipartUpload(createMultipartUploadInput);
            Assert.Greater(createMultipartUploadOutput.RequestID.Length, 0);
            Assert.Greater(createMultipartUploadOutput.UploadID.Length, 0);
            var uploadId3 = createMultipartUploadOutput.UploadID;
                
            // 上传第一段
            var offset = 0;
            var partSize = 5 * 1024 * 1024;
            var uploadPartCopyInput = new UploadPartCopyInput()
            {
                Bucket = bucket,
                Key = key3,
                UploadID = uploadId3,
                PartNumber = partNumber1,
                SrcBucket = bucket,
                SrcKey = key1,
                CopySourceRange = "bytes=" + offset + "-" + (partSize - 1).ToString()
            };
            var uploadPartCopyOutput = client.UploadPartCopy(uploadPartCopyInput);
            Assert.Greater(uploadPartCopyOutput.RequestID.Length, 0);
            parts[0] = new UploadedPart { PartNumber = partNumber1, ETag = uploadPartCopyOutput.ETag };
 
            // 上传第二段
            uploadPartCopyInput = new UploadPartCopyInput()
            {
                Bucket = bucket,
                Key = key3,
                UploadID = uploadId3,
                PartNumber = partNumber2,
                SrcBucket = bucket,
                SrcKey = key1,
                CopySourceRange = "bytes=" + partSize.ToString() + "-" + (fileSize - 1).ToString()
            };
            uploadPartCopyOutput = client.UploadPartCopy(uploadPartCopyInput);
            Assert.Greater(uploadPartCopyOutput.RequestID.Length, 0);
            parts[1] = new UploadedPart { PartNumber = partNumber2, ETag = uploadPartCopyOutput.ETag };
            
            // 合并段
            completeMultipartUploadInput = new CompleteMultipartUploadInput()
            {
                Bucket = bucket,
                Key = key3,
                UploadID = uploadId3,
                Parts = parts
            };
            completeMultipartUploadOutput = client.CompleteMultipartUpload(completeMultipartUploadInput);
            Assert.Greater(completeMultipartUploadOutput.RequestID.Length, 0);
            
            getObjectInput = new GetObjectInput
            {
                Bucket = bucket,
                Key = key3
            };
            getObjectOutput = client.GetObject(getObjectInput);

            using (var fileStream = new FileStream(dstFilePath3, FileMode.Create, FileAccess.Write))
            {
                var dataStr = Util.ReadStreamAsString(getObjectOutput.Content);
                getObjectOutput.Content.Close();
                // 将数据转换为字节数组
                var buffer = Encoding.UTF8.GetBytes(dataStr);
                // 使用文件流写入数据
                fileStream.Write(buffer, 0, buffer.Length);
            }
            Assert.AreEqual(md5, Util.CalculateMd5FromFile(dstFilePath3));
            
            if (File.Exists(dstFilePath1)) File.Delete(dstFilePath1);
            if (File.Exists(dstFilePath2)) File.Delete(dstFilePath2);
            if (File.Exists(dstFilePath3)) File.Delete(dstFilePath3);
            if (File.Exists(sampleFilePath)) File.Delete(sampleFilePath);
        }
    }
}