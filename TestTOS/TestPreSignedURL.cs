using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using NUnit.Framework;
using TOS.Model;

namespace TestTOS
{
    namespace TestTOS
    {
        [TestFixture]
        public class TestPreSignedURL
        {
            [Test]
            public void TestNormal()
            {
                var env = new TestEnv();
                var client = env.PrepareClient();
                var bucket = Util.GenerateBucketName("pre-signed-url-basic");
                var key = "folder/" + Util.GenerateRandomStr(10);
                var data = "hello world";
                
                var specialString = new List<string>() { };
                specialString.Add("/test01/!-_.*'()"); // 无需编码特殊字符
                specialString.Add("/test02/&$@=;+    ,?"); // 需要编码特殊字符，包含连续多个空格
                specialString.Add("/test03/\t\n\r\b\f\x007"); // 包含ASCII码控制字符
                specialString.Add("/test04/\uD83D\uDE0A?/\uD83D\uDE2D文本"); // 包含中文、emoji表情
                specialString.Add("/test05/[\\{^}%`~<>#|]\""); // 不建议使用的字符
                
                // 这里 .Net 的库发送请求时，会把点号和斜杠按照文件系统中的语义处理，预期如下的用例不能过
                // specialString.Add("./test06/./test"); // ./开头以及中间包含./
                // specialString.Add("../test07/../test"); // ../开头以及中间包含../
                // specialString.Add("/test08/."); // /.结尾
                // specialString.Add("/test09/.."); // /..结尾
                // specialString.Add("/test10///.."); // 包含多个连续的//
                
                var createBucketInput = new CreateBucketInput
                {
                    Bucket = bucket
                };
                Assert.DoesNotThrow(() => client.CreateBucket(createBucketInput));
                
                foreach (var ss in specialString)
                {
                    var tmpKey = key + ss;
                    var preSignedUrlInput = new PreSignedURLInput()
                    {
                        HttpMethod = HttpMethodType.HttpMethodPut,
                        Bucket = bucket,
                        Key = tmpKey,
                        Header = new Dictionary<string, string>() { { "Content-Type", "text/plain" } }
                    };
                    var preSignedUrlOutput = client.PreSignedURL(preSignedUrlInput);

                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(preSignedUrlOutput.SignedUrl);
                    request.Method = WebRequestMethods.Http.Put;
                    var body = System.Text.Encoding.UTF8.GetBytes(data);
                    request.ContentLength = data.Length;
                    request.ContentType = "text/plain";
                    using (Stream requestStream = request.GetRequestStream())
                    {
                        requestStream.Write(body, 0, data.Length);
                    }

                    HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                    Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
                    response.Close();

                    preSignedUrlInput = new PreSignedURLInput()
                    {
                        HttpMethod = HttpMethodType.HttpMethodGet,
                        Bucket = bucket,
                        Key = tmpKey,
                    };
                    preSignedUrlOutput = client.PreSignedURL(preSignedUrlInput);
                    request = (HttpWebRequest)WebRequest.Create(preSignedUrlOutput.SignedUrl);
                    request.Method = WebRequestMethods.Http.Get;
                    response = (HttpWebResponse)request.GetResponse();
                    Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
                    Assert.AreEqual(data, Util.ReadStreamAsString(response.GetResponseStream()));
                    response.Close();

                    preSignedUrlInput = new PreSignedURLInput()
                    {
                        HttpMethod = HttpMethodType.HttpMethodDelete,
                        Bucket = bucket,
                        Key = tmpKey,
                    };
                    preSignedUrlOutput = client.PreSignedURL(preSignedUrlInput);
                    request = (HttpWebRequest)WebRequest.Create(preSignedUrlOutput.SignedUrl);
                    request.Method = "DELETE";
                    response = (HttpWebResponse)request.GetResponse();
                    Assert.AreEqual(HttpStatusCode.NoContent, response.StatusCode);
                    response.Close();

                    preSignedUrlInput = new PreSignedURLInput()
                    {
                        HttpMethod = HttpMethodType.HttpMethodGet,
                        Bucket = bucket,
                        Key = tmpKey,
                    };
                    preSignedUrlOutput = client.PreSignedURL(preSignedUrlInput);
                    request = (HttpWebRequest)WebRequest.Create(preSignedUrlOutput.SignedUrl);
                    request.Method = WebRequestMethods.Http.Get;
                    try
                    {
                        response = (HttpWebResponse)request.GetResponse();
                    }
                    catch (WebException e)
                    {
                        Assert.AreEqual("The remote server returned an error: (404) Not Found.", e.Message);
                    }
                    finally
                    {
                        response.Close();
                    }


                    preSignedUrlInput = new PreSignedURLInput()
                    {
                        HttpMethod = HttpMethodType.HttpMethodPut,
                        Bucket = bucket,
                        Key = tmpKey,
                        Header = new Dictionary<string, string>() { { "Content-Type", "text/plain" } }
                    };

                    try
                    {
                        preSignedUrlOutput = client.PreSignedURL(preSignedUrlInput);
                        request = (HttpWebRequest)WebRequest.Create(preSignedUrlOutput.SignedUrl);
                        request.Method = WebRequestMethods.Http.Put;
                        request.ContentLength = data.Length;
                        using (Stream requestStream = request.GetRequestStream())
                        {
                            requestStream.Write(body, 0, data.Length);
                        }

                        response = (HttpWebResponse)request.GetResponse();
                    }
                    catch (WebException e)
                    {
                        Assert.AreEqual("The remote server returned an error: (403) Forbidden.", e.Message);
                    }
                    finally
                    {
                        response.Close();
                    }
                }
            }
        }
    }
}