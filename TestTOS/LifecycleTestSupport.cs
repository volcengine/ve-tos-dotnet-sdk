using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using TOS.Common;
using TOS.Model;

namespace TestTOS
{
    internal static class LifecycleTestSupport
    {
        internal static void AssertMetadataPreserved(SetObjectMetaInput expected, HeadObjectOutput actual)
        {
            Assert.That(actual.ContentType, Is.EqualTo(expected.ContentType));
            Assert.That(actual.CacheControl, Is.EqualTo(expected.CacheControl));
            Assert.That(actual.Expires, Is.Not.Null);
            // The existing HTTP Expires parser returns local time; compare instants, not clock display values.
            Assert.That(actual.Expires.Value.ToUniversalTime(), Is.EqualTo(expected.Expires.Value.ToUniversalTime()));
            Assert.That(actual.Meta.Count, Is.EqualTo(expected.Meta.Count));
            foreach (KeyValuePair<string, string> entry in expected.Meta)
            {
                Assert.That(actual.Meta["x-tos-meta-" + entry.Key], Is.EqualTo(entry.Value));
            }
        }

        internal static void AssertRuleEqual(LifecycleRule expected, LifecycleRule actual)
        {
            Assert.That(actual, Is.Not.Null);
            // Compare public model properties independently of the SDK's wire codec.
            // Missing collections are represented as empty arrays by SDK response models.
            Assert.That(RuleSnapshot(actual), Is.EqualTo(RuleSnapshot(expected)));
        }

        private static string RuleSnapshot(LifecycleRule rule)
        {
            JObject json = JObject.FromObject(rule);
            foreach (string name in new[] { "Transitions", "NoncurrentVersionTransitions", "Tags",
                "AccessTimeTransitions", "NonCurrentVersionAccessTimeTransitions" })
            {
                if (json[name].Type == JTokenType.Null)
                {
                    json[name] = new JArray();
                }
            }

            JObject filter = json["Filter"] as JObject;
            if (filter != null)
            {
                if (filter["Not"].Type == JTokenType.Null)
                {
                    filter["Not"] = new JArray();
                }

                foreach (JToken not in (JArray)filter["Not"])
                {
                    JObject entry = not as JObject;
                    if (entry != null && entry["Tags"].Type == JTokenType.Null)
                    {
                        entry["Tags"] = new JArray();
                    }
                }
            }

            return json.ToString();
        }

        internal static object Trans(GenericInput input)
        {
            return Invoke(input.GetType().GetMethod("Trans", BindingFlags.Instance | BindingFlags.NonPublic),
                input, null);
        }

        internal static T Parse<T>(IDictionary<string, string> header, string body, object request)
            where T : GenericOutput, new()
        {
            Type requestType = typeof(GenericOutput).Assembly.GetType("TOS.Common.HttpRequest");
            Type responseType = typeof(GenericOutput).Assembly.GetType("TOS.Common.HttpResponse");
            request = request ?? Activator.CreateInstance(requestType, true);
            object response = Activator.CreateInstance(responseType,
                BindingFlags.Instance | BindingFlags.NonPublic, null, new object[] { 200 }, null);
            Set(response, "Header", new Dictionary<string, string>(header, StringComparer.OrdinalIgnoreCase));
            Set(response, "Body", new MemoryStream(Encoding.UTF8.GetBytes(body)));
            RequestInfo info = new RequestInfo();
            Set(info, "StatusCode", 200);
            Set(info, "RequestID", "request-id");
            Set(info, "ID2", "id-2");
            Set(info, "Header", Get(response, "Header"));
            T output = new T();
            MethodInfo parse = typeof(GenericOutput).GetMethod("Parse",
                BindingFlags.Instance | BindingFlags.NonPublic, null,
                new[] { requestType, responseType, typeof(RequestInfo) }, null);
            Invoke(parse, output, new[] { request, response, info });
            return output;
        }

        internal static object Get(object instance, string name)
        {
            return Property(instance, name).GetGetMethod(true).Invoke(instance, null);
        }

        internal static void CheckResponse(object client, int statusCode, string body, Type outputType)
        {
            Type responseType = typeof(GenericOutput).Assembly.GetType("TOS.Common.HttpResponse");
            object response = Activator.CreateInstance(responseType,
                BindingFlags.Instance | BindingFlags.NonPublic, null, new object[] { statusCode }, null);
            Set(response, "Header", new Dictionary<string, string>
            {
                { "x-tos-request-id", "request-id" }, { "x-tos-id-2", "id-2" }
            });
            using (MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(body)))
            {
                Set(response, "Body", stream);
                Invoke(client.GetType().GetMethod("CheckResponse", BindingFlags.Instance | BindingFlags.NonPublic),
                    client, new[] { response, outputType });
            }
        }

        internal static IDictionary<string, string> Dictionary(object instance, string name)
        {
            return (IDictionary<string, string>)Get(instance, name);
        }

        internal static string Body(object request)
        {
            Stream stream = (Stream)Get(request, "Body");
            stream.Position = 0;
            return new StreamReader(stream, Encoding.UTF8).ReadToEnd();
        }

        private static void Set(object instance, string name, object value)
        {
            Property(instance, name).GetSetMethod(true).Invoke(instance, new[] { value });
        }

        private static PropertyInfo Property(object instance, string name)
        {
            PropertyInfo property = instance.GetType().GetProperty(name,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.That(property, Is.Not.Null);
            return property;
        }

        private static object Invoke(MethodInfo method, object instance, object[] args)
        {
            Assert.That(method, Is.Not.Null);
            try
            {
                return method.Invoke(instance, args);
            }
            catch (TargetInvocationException exception)
            {
                if (exception.InnerException != null)
                {
                    throw exception.InnerException;
                }

                throw;
            }
        }
    }
}
