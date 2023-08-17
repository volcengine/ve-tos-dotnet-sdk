using System.Collections.Generic;
using System.IO;
using System.Net;
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

                var createBucketInput = new CreateBucketInput
                {
                    Bucket = bucket
                };
                Assert.DoesNotThrow(() => client.CreateBucket(createBucketInput));
                
                var preSignedUrlInput = new PreSignedURLInput()
                {
                    HttpMethod = HttpMethodType.HttpMethodPut,
                    Bucket = bucket,
                    Key = key,
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
                    Key = key,
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
                    Key = key,
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
                    Key = key,
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
                    Key = key,
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