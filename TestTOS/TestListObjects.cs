using System.Collections.Generic;
using NUnit.Framework;
using TOS;
using TOS.Error;
using TOS.Model;

namespace TestTOS
{
    [TestFixture]
    public class TestListObjectToFile
    {
        [Test]
        public void TestNormal()
        {
            var env = new TestEnv();
            var client = env.PrepareClientBuilder().SetSocketTimeout(360 * 1000).SetConnectionTimeout(360 * 1000)
                .Build();
            var bucket = Util.GenerateBucketName("list-obj-basic");
            var prefix1 = Util.GenerateRandomStr(5) + "/";
            var prefix2 = Util.GenerateRandomStr(5) + "/";
            var data = "hello world";

            var keys = new List<string>() { };
            for (int i = 0; i < 100; i++)
            {
                keys.Add(prefix1 + Util.GenerateObjectName("normal"));
            }

            var createBucketInput = new CreateBucketInput
            {
                Bucket = bucket
            };
            Assert.DoesNotThrow(() => client.CreateBucket(createBucketInput));

            foreach (var k in keys)
            {
                Util.PutObjectFromStringAndCheckResponse(client, bucket, k, data);
            }
            
            var keysFromServer = new List<string>() { };
            var listObjectsInput = new ListObjectsInput()
            {
                Bucket = bucket,
                MaxKeys = 10,
                Prefix = prefix1
            };
            while (true)
            {
                var listObjectsOutput = client.ListObjects(listObjectsInput);
                foreach (var l in listObjectsOutput.Contents)
                {
                    keysFromServer.Add(l.Key);
                    Assert.Greater(l.HashCrc64ecma, 0);
                }

                if (!listObjectsOutput.IsTruncated)
                {
                    break;
                }

                listObjectsInput.Marker = listObjectsOutput.NextMarker;
            }
            
            keysFromServer.Sort();
            keys.Sort();
            Assert.AreEqual(keys, keysFromServer);

            var toBeDeleteObject = new ObjectTobeDeleted[50];
            for (int i = 50; i < 100; i++)
            {
                toBeDeleteObject[i - 50] = new ObjectTobeDeleted() { Key = keys[i] };
            }
            // 删除后50个对象
            var deleteMultiObjectsInput = new DeleteMultiObjectsInput()
            {
                Bucket = bucket,
                Objects = toBeDeleteObject
            };
            
            var deleteMultiObjectsOutput = client.DeleteMultiObjects(deleteMultiObjectsInput);
            Assert.Greater(deleteMultiObjectsOutput.RequestID.Length, 0);
            Assert.AreEqual(50, deleteMultiObjectsOutput.Deleted.Length);
            Assert.AreEqual(0, deleteMultiObjectsOutput.Error.Length);

            keys = keys.GetRange(0, 50);
            keysFromServer =  new List<string>() { };
            listObjectsInput = new ListObjectsInput()
            {
                Bucket = bucket,
                MaxKeys = 10,
                Prefix = prefix1
            };
            while (true)
            {
                var listObjectsOutput = client.ListObjects(listObjectsInput);
                foreach (var l in listObjectsOutput.Contents)
                {
                    keysFromServer.Add(l.Key);
                    Assert.Greater(l.HashCrc64ecma, 0);
                }

                if (!listObjectsOutput.IsTruncated)
                {
                    break;
                }

                listObjectsInput.Marker = listObjectsOutput.NextMarker;
            }
            keysFromServer.Sort();
            keys.Sort();
            Assert.AreEqual(keys, keysFromServer);
            
            keys = new List<string>() { };
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
                    Util.PutObjectFromStringAndCheckResponse(client, bucket, tmpKey, data);
                }
            }
            
            keysFromServer = new List<string>(){};
            var commonPrefixes = new List<string>(){ prefix2 };

            while (commonPrefixes.Count > 0)
            {
                var prefix = commonPrefixes[0];
                commonPrefixes = commonPrefixes.GetRange(1, commonPrefixes.Count - 1);
                ListByPrefix(client, bucket, prefix, keysFromServer, commonPrefixes);
            }
            
            keysFromServer.Sort();
            keys.Sort();
            Assert.AreEqual(keys, keysFromServer);
        }

        private static void ListByPrefix(ITosClient client, string bucket, string prefix, List<string> keysFromServer,
            List<string> commonPrefixes)
        {
            var input = new ListObjectsInput()
            {
                Bucket = bucket,
                MaxKeys = 1000,
                Prefix = prefix,
                Delimiter = "/"
            };

            while (true)
            {
                var output = client.ListObjects(input);

                foreach (var content in output.Contents)
                {
                    keysFromServer.Add(content.Key);
                }
                
                foreach (var commonPrefix in output.CommonPrefixes)
                {
                    commonPrefixes.Add(commonPrefix.Prefix);
                }

                if (!output.IsTruncated)
                {
                    break;
                }

                input.Marker = output.NextMarker;
            }
        }

        [Test]
        public void TestAbnormal()
        {
            var env = new TestEnv();
            var client = env.PrepareClientBuilder().SetSocketTimeout(360 * 1000).SetConnectionTimeout(360 * 1000)
                .Build();
            
            var listObjectsInput = new ListObjectsInput()
            {
                Bucket = "no-exist-bucket",
            };
            
            var ex = Assert.Throws<TosServerException>(() => client.ListObjects(listObjectsInput));
            Assert.AreEqual(404, ex.StatusCode);
            Assert.AreEqual("NoSuchBucket", ex.Code);
            
            var listObjectVersionsInput = new ListObjectVersionsInput()
            {
                Bucket = "no-exist-bucket",
            };
            
            ex = Assert.Throws<TosServerException>(() => client.ListObjectVersions(listObjectVersionsInput));
            Assert.AreEqual(404, ex.StatusCode);
            Assert.AreEqual("NoSuchBucket", ex.Code);

            var deleteMultiObjectsInput = new DeleteMultiObjectsInput()
            {
                Bucket = "no-exist-bucket",
                Objects = new [] { new ObjectTobeDeleted() { Key = "testKey" } }
            };
            
            ex = Assert.Throws<TosServerException>(() => client.DeleteMultiObjects(deleteMultiObjectsInput));
            Assert.AreEqual(404, ex.StatusCode);
            Assert.AreEqual("NoSuchBucket", ex.Code);
        }
    }
}