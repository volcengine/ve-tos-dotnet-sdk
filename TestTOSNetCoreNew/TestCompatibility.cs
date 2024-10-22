using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using NUnit.Framework;
using TestTOS;
using TOS;
using TOS.Error;
using TOS.Model;

namespace TestTOSNewCore5
{
    public class MyStream : Stream
    {
        private int cnt = 5;
        private volatile bool _closed = false;
        private int _interval = -1;

        public MyStream()
        {
        }

        public MyStream(int interval)
        {
            _interval = interval;
        }

        public override void Flush()
        {
            throw new System.NotImplementedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (_closed)
            {
                throw new System.NotImplementedException();
            }

            if (cnt <= 0)
            {
                return 0;
            }

            cnt--;
            if (_interval > 0)
            {
                Thread.Sleep(_interval);
            }

            byte[] source = Encoding.UTF8.GetBytes("hello");
            if (count < source.Length)
            {
                Buffer.BlockCopy(source, 0, buffer, offset, count);
                return count;
            }

            source.CopyTo(buffer, 0);
            return source.Length;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new System.NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new System.NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new System.NotImplementedException();
        }

        public override void Close()
        {
            this._closed = true;
        }

        public override bool CanRead
        {
            get { return true; }
        }

        public override bool CanSeek { get; }
        public override bool CanWrite { get; }
        public override long Length { get; }
        public override long Position { get; set; }
    }

    public class StreamWrapper : Stream
    {
        private Stream _inner;

        public StreamWrapper(Stream inner)
        {
            _inner = inner;
        }

        public override void Flush()
        {
            _inner.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return _inner.Read(buffer, offset, count);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return _inner.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            _inner.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            _inner.Write(buffer, offset, count);
        }

        public override bool CanRead
        {
            get { return _inner.CanRead; }
        }

        public override bool CanSeek
        {
            get { return _inner.CanSeek; }
        }

        public override bool CanWrite
        {
            get { return _inner.CanWrite; }
        }

        public override long Length
        {
            get { return _inner.Length; }
        }

        public override long Position
        {
            get { return _inner.Position; }
            set { _inner.Position = value; }
        }
    }

    [TestFixture]
    public class TestCompatibility
    {
        [Test]
        public void TestPutObject()
        {
            var env = new TestEnv();
            var client = env.PrepareClient();
            var bucket = Util.GenerateBucketName("put-basic");

            try
            {
                var createBucketInput = new CreateBucketInput
                {
                    Bucket = bucket
                };
                Assert.DoesNotThrow(() => client.CreateBucket(createBucketInput));

                var key = Util.GenerateObjectName("object");
                // 带 content-length 上传
                var data = "helloworld";
                var putObjectInput = new PutObjectInput()
                {
                    Bucket = bucket,
                    Key = key,
                    Content = Util.ConvertStringToStream(data),
                    ContentLength = data.Length,
                };
                var putObjectOutput = client.PutObject(putObjectInput);
                Assert.AreEqual(200, putObjectOutput.StatusCode);

                var getObjectInput = new GetObjectInput()
                {
                    Bucket = bucket,
                    Key = key,
                };
                var getObjectOutput = client.GetObject(getObjectInput);
                Assert.AreEqual(200, getObjectOutput.StatusCode);
                Assert.Greater(getObjectOutput.RequestID.Length, 0);
                Assert.AreEqual(getObjectOutput.ContentLength, data.Length);
                Assert.AreEqual(data, Util.ReadStreamAsString(getObjectOutput.Content));

                // 上传 0 字节数据
                putObjectInput = new PutObjectInput()
                {
                    Bucket = bucket,
                    Key = key,
                };
                putObjectOutput = client.PutObject(putObjectInput);
                Assert.AreEqual(200, putObjectOutput.StatusCode);

                getObjectOutput = client.GetObject(getObjectInput);
                Assert.AreEqual(200, getObjectOutput.StatusCode);
                Assert.Greater(getObjectOutput.RequestID.Length, 0);
                Assert.AreEqual(getObjectOutput.ContentLength, 0);

                var sampleFilePath = "./" + Util.GenerateRandomStr(10) + ".txt";
                Util.CreateRandomStringFile(sampleFilePath, 10);
                // 上传本地文件
                var putObjectFromFileInput = new PutObjectFromFileInput()
                {
                    Bucket = bucket,
                    Key = key,
                    FilePath = sampleFilePath
                };


                FileInfo fi = new FileInfo(sampleFilePath);
                putObjectOutput = client.PutObjectFromFile(putObjectFromFileInput);
                Assert.AreEqual(200, putObjectOutput.StatusCode);


                getObjectOutput = client.GetObject(getObjectInput);
                Assert.AreEqual(200, getObjectOutput.StatusCode);
                Assert.Greater(getObjectOutput.RequestID.Length, 0);
                Assert.AreEqual(getObjectOutput.ContentLength, fi.Length);
                using (Stream fileStream = new FileStream(sampleFilePath, FileMode.Open, FileAccess.Read))
                {
                    Assert.AreEqual(Util.ReadStreamAsString(fileStream),
                        Util.ReadStreamAsString(getObjectOutput.Content));
                }

                // 测试 chunk 上传
                putObjectInput = new PutObjectInput()
                {
                    Bucket = bucket,
                    Key = key,
                    Content = new MyStream(),
                };
                putObjectOutput = client.PutObject(putObjectInput);
                Assert.AreEqual(200, putObjectOutput.StatusCode);

                getObjectOutput = client.GetObject(getObjectInput);
                Assert.AreEqual(200, getObjectOutput.StatusCode);
                Assert.Greater(getObjectOutput.RequestID.Length, 0);
                Assert.AreEqual(getObjectOutput.ContentLength, 25);
                Assert.AreEqual("hellohellohellohellohello", Util.ReadStreamAsString(getObjectOutput.Content));

                var md5 = Util.CalculateMd5("hellohel");
                // 测试 http basic 头域
                var expires = DateTime.Now.AddHours(1);
                putObjectInput = new PutObjectInput()
                {
                    Bucket = bucket,
                    Key = key,
                    Content = new MyStream(),
                    ContentLength = 8,
                    ContentMD5 = md5,
                    ContentType = "text/plain",
                    ContentEncoding = "test-encoding",
                    CacheControl = "test-cache-control",
                    ContentLanguage = "test-language",
                    ContentDisposition = "attachment; filename=\"中文%&!@#$%^&*()202411.2029.txt\"",
                    Expires = expires,
                    Meta = new Dictionary<string, string>() { { "aaa", "bbb" }, { "中文键", "中文值" } },
                };
                putObjectOutput = client.PutObject(putObjectInput);
                Assert.AreEqual(200, putObjectOutput.StatusCode);

                getObjectOutput = client.GetObject(getObjectInput);
                Assert.AreEqual(200, getObjectOutput.StatusCode);
                Assert.Greater(getObjectOutput.RequestID.Length, 0);
                Assert.AreEqual(getObjectOutput.ContentLength, 8);
                Assert.AreEqual(getObjectOutput.ContentType, "text/plain");
                Assert.AreEqual(getObjectOutput.CacheControl, "test-cache-control");
                Assert.AreEqual(getObjectOutput.ContentEncoding, "test-encoding");
                Assert.AreEqual(getObjectOutput.ContentLanguage, "test-language");
                CultureInfo info = CultureInfo.GetCultureInfo("en-US");
                string longDateFormat = "yyyyMMdd\\THHmmss\\Z";
                Assert.AreEqual(getObjectOutput.Expires?.ToString(longDateFormat, info),
                    expires.ToString(longDateFormat, info));
                Assert.AreEqual(getObjectOutput.ContentDisposition,
                    "attachment; filename=\"中文%&!@#$%^&*()202411.2029.txt\"");
                Assert.AreEqual("hellohel", Util.ReadStreamAsString(getObjectOutput.Content));

                // 测试 Range 获取以及 Response 重写
                expires = DateTime.Now.AddHours(2);
                getObjectInput.Range = "bytes=1-2";
                getObjectInput.ResponseCacheControl = "response-test-cache-control";
                getObjectInput.ResponseContentDisposition = "attachment; filename=\"1.txt\"";
                getObjectInput.ResponseContentEncoding = "response-test-encoding";
                getObjectInput.ResponseContentLanguage = "response-test-language";
                getObjectInput.ResponseContentType = "response-test-content-type";
                getObjectInput.ResponseExpires = expires;
                getObjectOutput = client.GetObject(getObjectInput);
                Assert.AreEqual(206, getObjectOutput.StatusCode);
                Assert.Greater(getObjectOutput.RequestID.Length, 0);
                Assert.AreEqual(getObjectOutput.ContentLength, 2);
                Assert.AreEqual("el", Util.ReadStreamAsString(getObjectOutput.Content));
                Assert.AreEqual(getObjectOutput.ContentType, "response-test-content-type");
                Assert.AreEqual(getObjectOutput.CacheControl, "response-test-cache-control");
                Assert.AreEqual(getObjectOutput.ContentEncoding, "response-test-encoding");
                Assert.AreEqual(getObjectOutput.ContentLanguage, "response-test-language");
                Assert.AreEqual(getObjectOutput.Expires?.ToString(longDateFormat, info),
                    expires.ToString(longDateFormat, info));
                Assert.AreEqual(getObjectOutput.ContentDisposition, "attachment; filename=\"1.txt\"");
            }
            finally
            {
                ClearEnv.ClearDotNetSdkObj(client, bucket);
                ClearEnv.ClearDotNetSdkPart(client, bucket);
                var deleteBucketInput = new DeleteBucketInput
                {
                    Bucket = bucket
                };
                client.DeleteBucket(deleteBucketInput);
            }
        }

        [Test]
        public void TestClientConfiguration()
        {
            var env = new TestEnv();
            var client0 = env.PrepareClient();
            var client1 = TosClientBuilder.Builder().SetAk(env.AccessKey).SetSk(env.SecretKey).SetEndpoint(env.EndPoint)
                .SetRegion(env.Region)
                .SetConnectionTimeout(1)
                .Build();

            var client2 = TosClientBuilder.Builder().SetAk(env.AccessKey).SetSk(env.SecretKey).SetEndpoint(env.EndPoint)
                .SetRegion(env.Region)
                .SetSocketTimeout(1000)
                .Build();

            var client3 = TosClientBuilder.Builder().SetAk(env.AccessKey).SetSk(env.SecretKey).SetEndpoint(env.EndPoint)
                .SetRegion(env.Region)
                .SetMaxConnections(1)
                .Build();

            try
            {
                client1.ListBuckets();
                Assert.Fail();
            }
            catch (TosException e)
            {
                Console.WriteLine(e.Message);
            }

            var bucket = Util.GenerateBucketName("put-basic");
            try
            {
                var listBucketsOutput = client0.ListBuckets();
                Assert.AreEqual(200, listBucketsOutput.StatusCode);
                Assert.True(listBucketsOutput.Buckets.Length >= 0);
                var createBucketInput = new CreateBucketInput
                {
                    Bucket = bucket
                };
                Assert.DoesNotThrow(() => client0.CreateBucket(createBucketInput));

                var putObjectInput = new PutObjectInput()
                {
                    Bucket = bucket,
                    Key = Util.GenerateObjectName("object"),
                    Content = new MyStream(2000),
                };
                try
                {
                    client2.PutObject(putObjectInput);
                    Assert.Fail();
                }
                catch (TosException e)
                {
                    Console.WriteLine(e.Message);
                }

                Stopwatch stopwatch = Stopwatch.StartNew();
                putObjectConurrently(client0, bucket);
                stopwatch.Stop();
                Assert.True(stopwatch.ElapsedMilliseconds >= 5000 && stopwatch.ElapsedMilliseconds < 10000);

                stopwatch = Stopwatch.StartNew();
                putObjectConurrently(client3, bucket);
                stopwatch.Stop();
                Assert.True(stopwatch.ElapsedMilliseconds >= 10000);
            }
            finally
            {
                ClearEnv.ClearDotNetSdkObj(client0, bucket);
                ClearEnv.ClearDotNetSdkPart(client0, bucket);
                var deleteBucketInput = new DeleteBucketInput
                {
                    Bucket = bucket
                };
                client0.DeleteBucket(deleteBucketInput);
            }
        }

        public void putObjectConurrently(ITosClient client, string bucket)
        {
            List<Thread> threads = new List<Thread>();
            for (int i = 0; i < 2; i++)
            {
                Thread t = new Thread(() =>
                {
                    var putObjectInput = new PutObjectInput()
                    {
                        Bucket = bucket,
                        Key = Util.GenerateObjectName("object"),
                        Content = new MyStream(1000),
                    };
                    var putObjectOutput = client.PutObject(putObjectInput);
                    Assert.AreEqual(200, putObjectOutput.StatusCode);
                });
                threads.Add(t);
            }

            foreach (var thread in threads)
            {
                thread.Start();
            }

            foreach (var thread in threads)
            {
                thread.Join();
            }
        }

        [Test]
        public void TestReadWriteStreaming()
        {
            var bigFilePath = "./bigfile.txt";
            FileInfo fileInfo = new FileInfo(bigFilePath);
            if (!File.Exists(bigFilePath))
            {
                Util.CreateRandomStringFile(bigFilePath, 1000000000);
            }

            Console.WriteLine(fileInfo.FullName);

            var env = new TestEnv();
            var client = env.PrepareClient();
            var bucket = Util.GenerateBucketName("put-basic");
            var key = Util.GenerateObjectName("object");
            try
            {
                var createBucketInput = new CreateBucketInput
                {
                    Bucket = bucket
                };
                Assert.DoesNotThrow(() => client.CreateBucket(createBucketInput));
                
                CancellationTokenSource source = new CancellationTokenSource();
                Thread uploadThread = new Thread(() =>
                {
                    try
                    {
                        using (var stream =
                               new StreamWrapper(new FileStream(bigFilePath, FileMode.Open, FileAccess.Read)))
                        {
                            client.PutObject(new PutObjectInput()
                            {
                                Bucket = bucket,
                                Key = key,
                                Content = stream,
                                Source = source,
                            });
                        }
                    }
                    catch (TosClientException ex)
                    {
                        Assert.AreEqual(ex.Message, "The operation was canceled.");
                    }
                });
                uploadThread.Start();
                Thread cancelThread = new Thread(() => 
                {
                    Thread.Sleep(2000);
                    source.Cancel();
                });
                cancelThread.Start();
                cancelThread.Join();
                uploadThread.Join();
                
                
                using (var stream =
                       new StreamWrapper(new FileStream(bigFilePath, FileMode.Open, FileAccess.Read)))
                {
                    var putObjectInput = new PutObjectInput()
                    {
                        Bucket = bucket,
                        Key = key,
                        Content = stream,
                    };
                    var putObjectOutput = client.PutObject(putObjectInput);
                    Assert.AreEqual(200, putObjectOutput.StatusCode);
                }
                
                Console.WriteLine("upload finished, start to download");
                var getObjectInput = new GetObjectInput()
                {
                    Bucket = bucket,
                    Key = key,
                };
                var getObjectOutput = client.GetObject(getObjectInput);
                Assert.AreEqual(200, getObjectOutput.StatusCode);
                Assert.Greater(getObjectOutput.RequestID.Length, 0);
                Assert.AreEqual(getObjectOutput.ContentLength, fileInfo.Length);
                Console.WriteLine(getObjectOutput.ETag);
                using (var fileStream =
                       new FileStream(bigFilePath, FileMode.Open, FileAccess.Read))
                {
                    Assert.AreEqual(Util.CalculateMd5(getObjectOutput.Content), Util.CalculateMd5(fileStream));
                }
            }
            finally
            {
                ClearEnv.ClearDotNetSdkObj(client, bucket);
                ClearEnv.ClearDotNetSdkPart(client, bucket);
                var deleteBucketInput = new DeleteBucketInput
                {
                    Bucket = bucket
                };
                client.DeleteBucket(deleteBucketInput);
            }
        }
    }
}