using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using TOS.Model;

namespace TestTOS
{
    [TestFixture]
    public class TestGetObjectToFile
    {
        [Test]
        public void TestNormal()
        {
            var env = new TestEnv();
            var client = env.PrepareClientBuilder().SetSocketTimeout(360 * 1000).SetConnectionTimeout(360 * 1000)
                .Build();
            var bucket = Util.GenerateBucketName("get-obj-to-file-basic");
            var key = Util.GenerateObjectName("normal");
            var keyFolder = Util.GenerateRandomStr(10) + "/";
            var sampleFilePath = "./temp" + Util.GenerateRandomStr(10) + ".txt";
            var data = "hello world";

            var createBucketInput = new CreateBucketInput
            {
                Bucket = bucket
            };
            Assert.DoesNotThrow(() => client.CreateBucket(createBucketInput));

            var filePaths = new List<string>()
            {
                "./temp/1.txt", Directory.GetCurrentDirectory() + "/temp/2.txt",
                "./temp/aaa/bbb/ccc/ddd/3.txt", "./temp",
                "./temp2/aaa/bbb/ccc/", "./temp2/aaa/bbb/ccc/1.txt",
                "./temp/a/b.file", "./temp/a/b/c.file", "./temp/a/d/e/f/g.file",
                "./temp/a/b/d.file", "./temp/a/g/", "./temp/a/f"
            };
                
            var realFilePaths = new List<string>()
            {
                "./temp/1.txt", Directory.GetCurrentDirectory() + "/temp/2.txt",
                "./temp/aaa/bbb/ccc/ddd/3.txt", "./temp/"+key,
                "./temp2/aaa/bbb/ccc/"+key, "./temp2/aaa/bbb/ccc/1.txt",
                "./temp/a/b.file", "./temp/a/b/c.file", "./temp/a/d/e/f/g.file",
                "./temp/a/b/d.file", "./temp/a/g/"+key, "./temp/a/f"
            };
            
            var folderPaths = new List<string>() { "./temp/", "./temp/123/456/", "./temp", "./temp/888" };
            var realFilePathForFolderPaths = new List<string>() { "./temp/"+key, "./temp/123/456/"+key, "./temp/"+key, "./temp/888"};

            try
            {
                Util.CreateRandomStringFile(sampleFilePath, 10000);
                var sourceFileMd5 = Util.CalculateMd5FromFile(sampleFilePath);

                var putObjectFromFileInput = new PutObjectFromFileInput
                {
                    Bucket = bucket,
                    Key = key,
                    FilePath = sampleFilePath
                };
                var putObjectFromFileOutput = client.PutObjectFromFile(putObjectFromFileInput);
                Assert.Greater(putObjectFromFileOutput.RequestID.Length, 0);
                Assert.Greater(putObjectFromFileOutput.ETag.Length, 0);

                // 验证下载文件
                for (int i = 0; i < filePaths.Count; i++)
                {
                    var filePath = filePaths[i];
                    var realFilePath = realFilePaths[i];
                    var input = new GetObjectToFileInput()
                    {
                        Bucket = bucket,
                        Key = key,
                        FilePath = filePath,
                        ResponseContentDisposition = "attachment; filename=中文%&!@#$%^&*()202411.2029.txt"
                    };
                    
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                    }

                    var output = client.GetObjectToFile(input);
                    Assert.Greater(output.RequestID.Length, 0);
                    Assert.Greater(output.ETag.Length, 0);
                    Assert.AreEqual(input.ResponseContentDisposition, output.ContentDisposition);
                    Assert.AreEqual(sourceFileMd5, Util.CalculateMd5FromFile(realFilePath));
                }

                foreach (var realFilePath in realFilePaths)
                {
                    Assert.True(File.Exists(realFilePath));
                }
                
                // 验证下载文件夹
                for (int i = 0; i < folderPaths.Count; i++)
                {
                    var filePath = folderPaths[i];
                    var realFilePath = realFilePathForFolderPaths[i];
                    
                    var input = new GetObjectToFileInput()
                    {
                        Bucket = bucket,
                        Key = key,
                        FilePath = filePath
                    };
                    
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                    }

                    var output = client.GetObjectToFile(input);
                    Assert.Greater(output.RequestID.Length, 0);
                    Assert.Greater(output.ETag.Length, 0);
                    Console.WriteLine(realFilePath);
                    Assert.True(File.Exists(realFilePath));
                }
                
                Assert.True(Directory.Exists(folderPaths[0]));
                Assert.True(Directory.Exists(folderPaths[1]));
                Assert.True(Directory.Exists(folderPaths[2]));
                Assert.True(File.Exists(folderPaths[3]));
            }
            finally
            {
                if (File.Exists(sampleFilePath))
                {
                    File.Delete(sampleFilePath);
                }

                foreach (var r in realFilePaths)
                {
                    if (File.Exists(r))
                    {
                        File.Delete(r);
                    }
                }
                
                foreach (var r in realFilePathForFolderPaths)
                {
                    if (File.Exists(r))
                    {
                        File.Delete(r);
                    }
                }
            }
        }
    }
}