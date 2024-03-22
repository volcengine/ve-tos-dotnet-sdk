using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using NUnit.Framework;
using TOS;
using TOS.Error;
using TOS.Model;

namespace TestTOS
{
    public class Util
    {
        public const string BucketPrefix = "dotnetsdk-bkt";
        public const string ObjectPrefix = "dotnetsdk-obj";

        private const int DefaultDuration = 2000;
        private const int DefaultMaxWaitTime = 2000;

        public static void CheckBucketMeta(ITosClient client, string bucket, string region,
            StorageClassType? storageClass,
            AzRedundancyType? azRedundancy = AzRedundancyType.AzRedundancySingleAz)
        {
            var headBucketInput = new HeadBucketInput
            {
                Bucket = bucket
            };
            var headBucketOutput = client.HeadBucket(headBucketInput);
            Assert.NotNull(headBucketOutput);
            Assert.AreEqual(200, headBucketOutput.StatusCode);
            Assert.AreEqual(region, headBucketOutput.Region);
            Assert.AreEqual(storageClass, headBucketOutput.StorageClass);
            Assert.AreEqual(azRedundancy, headBucketOutput.AzRedundancy);
        }

        public static Stream ConvertStringToStream(string input)
        {
            byte[] byteArray = Encoding.UTF8.GetBytes(input);
            return new MemoryStream(byteArray);
        }

        public static string ReadStreamAsString(Stream stream)
        {
            using (var reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }
        }

        public static void WaitUntilObjectExist(ITosClient client, string bucket, string key,
            int duration = DefaultDuration, int maxWaitTime = DefaultMaxWaitTime)
        {
            var startTime = DateTime.Now;
            var headObjectInput = new HeadObjectInput()
            {
                Bucket = bucket,
                Key = key
            };

            while (true)
            {
                try
                {
                    var headObjectOutput = client.HeadObject(headObjectInput);
                    Assert.AreEqual(200, headObjectOutput.StatusCode);
                    break;
                }
                catch (TosServerException ex)
                {
                    if (ex.StatusCode != 404)
                    {
                        break;
                    }
                }

                Thread.Sleep(duration);
                TimeSpan elapsedTime = DateTime.Now - startTime;
                Assert.LessOrEqual(30, elapsedTime.TotalSeconds);
            }
        }

        public static void PutRandomObject(ITosClient client, string bucket, string key, int size)
        {
            var value = GenerateRandomStr(size);
            var putObjectInput = new PutObjectInput()
            {
                Bucket = bucket,
                Key = key,
                Content = ConvertStringToStream(value)
            };
            var putObjectOutput = client.PutObject(putObjectInput);
            Assert.AreEqual(200, putObjectOutput.StatusCode);
        }

        public static void PutRandomObjectWithCallback(ITosClient client, string bucket, string key, int size, 
            string callbackInput, string callbackVarInput)
        {
            var value = GenerateRandomStr(size);
            var putObjectInput = new PutObjectInput()
            {
                Bucket = bucket,
                Key = key,
                Content = ConvertStringToStream(value),
                Callback = Convert.ToBase64String(Encoding.UTF8.GetBytes(callbackInput)),
                CallbackVar = Convert.ToBase64String(Encoding.UTF8.GetBytes(callbackVarInput))
            };
            var putObjectOutput = client.PutObject(putObjectInput);
            Assert.AreEqual(200, putObjectOutput.StatusCode);
            Assert.AreNotEqual("",putObjectOutput.CallbackResult);
            Assert.True(putObjectOutput.CallbackResult.Contains("ok"));
        }

        public static string CalculateMd5(string s)
        {
            using (MD5 md5 = MD5.Create())
            {
                byte[] inputBytes = Encoding.UTF8.GetBytes(s);
                byte[] hashBytes = md5.ComputeHash(inputBytes);
                return Convert.ToBase64String(hashBytes);
            }
        }

        public static string CalculateMd5FromFile(string filePath)
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(filePath))
                {
                    byte[] hashBytes = md5.ComputeHash(stream);
                    return Convert.ToBase64String(hashBytes);
                }
            }
        }

        public static string GenerateRandomStr(int len)
        {
            const string chars = "abcdefghijklmnopqrstuvwxyz0123456789";
            var random = new Random();
            var stringBuilder = new StringBuilder(len);

            for (int i = 0; i < len; i++)
            {
                int index = random.Next(chars.Length);
                stringBuilder.Append(chars[index]);
            }

            return stringBuilder.ToString();
        }

        public static string GenerateBucketName(string bucket)
        {
            return BucketPrefix + "-" + bucket + "-" + GenerateRandomStr(8);
        }

        public static string GenerateObjectName(string obj)
        {
            return BucketPrefix + "-" + obj + "-" + GenerateRandomStr(8);
        }

        public static PutObjectOutput PutObjectFromStreamAndCheckResponse(ITosClient client, string bucket, string key,
            Stream content)
        {
            var putObjectInput = new PutObjectInput()
            {
                Bucket = bucket,
                Key = key,
                Content = content
            };
            var putObjectOutput = client.PutObject(putObjectInput);
            Assert.Greater(putObjectOutput.RequestID.Length, 0);
            Assert.Greater(putObjectOutput.ETag.Length, 0);
            Assert.AreEqual(200, putObjectOutput.StatusCode);

            return putObjectOutput;
        }

        public static PutObjectOutput PutObjectFromStringAndCheckResponse(ITosClient client, string bucket, string key,
            string data)
        {
            var putObjectInput = new PutObjectInput()
            {
                Bucket = bucket,
                Key = key,
                Content = Util.ConvertStringToStream(data)
            };
            var putObjectOutput = client.PutObject(putObjectInput);
            Assert.Greater(putObjectOutput.RequestID.Length, 0);
            Assert.Greater(putObjectOutput.ETag.Length, 0);
            Assert.AreEqual(200, putObjectOutput.StatusCode);

            return putObjectOutput;
        }

        public static GetObjectOutput GetObjectAndCheckResponse(ITosClient client, string bucket, string key,
            string data)
        {
            var getObjectInput = new GetObjectInput()
            {
                Bucket = bucket,
                Key = key
            };
            var getObjectOutput = client.GetObject(getObjectInput);
            Assert.Greater(getObjectOutput.RequestID.Length, 0);
            Assert.AreEqual(200, getObjectOutput.StatusCode);
            Assert.AreEqual(data, Util.ReadStreamAsString(getObjectOutput.Content));
            getObjectOutput.Content.Close();

            return getObjectOutput;
        }

        public static void DeleteObjectAndCheckResponse(ITosClient client, string bucket, string key)
        {
            var deleteObjectInput = new DeleteObjectInput()
            {
                Bucket = bucket,
                Key = key
            };
            var deleteObjectOutput = client.DeleteObject(deleteObjectInput);
            Assert.Greater(deleteObjectOutput.RequestID.Length, 0);
        }

        public static void CreateRandomStringFile(string filePath, int characterCount)
        {
            if (File.Exists(filePath))
            {
                return;
            }

            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentException("File path cannot be empty.");

            if (characterCount <= 0)
                throw new ArgumentException("Character count must be greater than zero.");

            string randomString = GenerateRandomStr(characterCount);

            try
            {
                File.WriteAllText(filePath, randomString);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to write file: {ex.Message}");
            }
        }
    }
}