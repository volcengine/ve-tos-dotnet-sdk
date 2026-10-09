using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using TOS;
using TOS.Common;
using TOS.Error;
using TOS.Model;

namespace TestTOS
{
    [TestFixture]
    public class TestBucketLifecycle
    {
        [TestCase("prefix")]
        [TestCase("status")]
        [TestCase("storage")]
        [TestCase("filter")]
        [TestCase("array")]
        [TestCase("date")]
        public void TestFullRuleAssertionDetectsNestedDifferences(string field)
        {
            LifecycleRule expected = ComparisonRule();
            LifecycleRule actual = ComparisonRule();
            switch (field)
            {
                case "prefix": actual.Prefix = "other/"; break;
                case "status": actual.Status = StatusType.StatusDisabled; break;
                case "storage": actual.NoncurrentVersionTransitions[0].StorageClass = StorageClassType.StorageClassArchive; break;
                case "filter": actual.Filter.GreaterThanIncludeEqual = StatusType.StatusDisabled; break;
                case "array": actual.NoncurrentVersionTransitions = new NonCurrentVersionTransition[0]; break;
                case "date": actual.Transitions[0].Date = actual.Transitions[0].Date.Value.AddDays(1); break;
            }

            Assert.Throws<AssertionException>(() => LifecycleTestSupport.AssertRuleEqual(expected, actual));
        }

        [Test]
        public void TestFullRuleAssertionAcceptsEmptyResponseCollections()
        {
            LifecycleRule expected = ComparisonRule();
            LifecycleRule actual = ComparisonRule();
            actual.Tags = new Tag[0];
            actual.AccessTimeTransitions = new AccessTimeTransition[0];
            actual.NonCurrentVersionAccessTimeTransitions = new NonCurrentVersionAccessTimeTransition[0];
            actual.Filter.Not = new BucketLifecycleNotFilter[0];
            Assert.DoesNotThrow(() => LifecycleTestSupport.AssertRuleEqual(expected, actual));
            Assert.That(expected.Tags, Is.Null, "Assertions must not mutate the expected model");
        }

        private static LifecycleRule ComparisonRule()
        {
            return new LifecycleRule
            {
                ID = "compare", Prefix = "test/", Status = StatusType.StatusEnabled,
                Transitions = new[]
                {
                    new Transition { Date = new DateTime(2035, 1, 1, 0, 0, 0, DateTimeKind.Utc), StorageClass = StorageClassType.StorageClassIa }
                },
                NoncurrentVersionTransitions = new[]
                {
                    new NonCurrentVersionTransition { NonCurrentDays = 30, StorageClass = StorageClassType.StorageClassIa }
                },
                Filter = new LifecycleRuleFilter { ObjectSizeGreaterThan = 1000, GreaterThanIncludeEqual = StatusType.StatusEnabled }
            };
        }

        [TestCase("Bucket")]
        [TestCase("ObjectSet")]
        [TestCase("ByTag")]
        public void TestFullRuleAssertionAcceptsPrefixOnlyNotRoundTrip(string scope)
        {
            LifecycleRule expected = new LifecycleRule
            {
                ID = "not", Status = StatusType.StatusEnabled, Expiration = new Expiration { Days = 30 },
                Filter = new LifecycleRuleFilter
                {
                    Not = new[]
                    {
                        new BucketLifecycleNotFilter { Prefix = "excluded/" },
                        new BucketLifecycleNotFilter { Tags = new[] { new Tag { Key = "keep", Value = "true" } } },
                        new BucketLifecycleNotFilter { Prefix = "empty-tags/", Tags = new Tag[0] }
                    }
                }
            };
            // Independent response fixture: the service may omit empty Tags inside any Not entry.
            const string body = "{\"Rules\":[{\"ID\":\"not\",\"Status\":\"Enabled\",\"Expiration\":{\"Days\":30}," +
                "\"Filter\":{\"Not\":[{\"Prefix\":\"excluded/\"},{\"Tags\":[{\"Key\":\"keep\",\"Value\":\"true\"}]}," +
                "{\"Prefix\":\"empty-tags/\"}]}}]}";
            IDictionary<string, string> headers = new Dictionary<string, string>();
            LifecycleRule actual = scope == "Bucket"
                ? LifecycleTestSupport.Parse<GetBucketLifecycleOutput>(headers, body, null).Rules[0]
                : scope == "ObjectSet"
                    ? LifecycleTestSupport.Parse<GetObjectSetLifecycleOutput>(headers, body, null).Rules[0]
                    : LifecycleTestSupport.Parse<GetObjectSetLifecycleByTagOutput>(headers,
                        "{\"ObjectSetTagRules\":[" + body + "]}", null).ObjectSetTagRules[0].Rules[0];

            Assert.DoesNotThrow(() => LifecycleTestSupport.AssertRuleEqual(expected, actual));
            Assert.DoesNotThrow(() => LifecycleTestSupport.AssertRuleEqual(actual, expected));
            Assert.That(expected.Filter.Not[0].Tags, Is.Null, "Do not mutate the caller's model");
            Assert.That(actual.Filter.Not[0].Tags, Is.Empty, "Do not mutate the parsed model");
        }

        [TestCase("tag-key")]
        [TestCase("tag-value")]
        [TestCase("tags-null")]
        [TestCase("tags-empty")]
        [TestCase("prefix")]
        [TestCase("missing-entry")]
        [TestCase("null-entry")]
        public void TestFullRuleAssertionStillDetectsNotFilterDifferences(string change)
        {
            LifecycleRule expected = ComparisonRule();
            LifecycleRule actual = ComparisonRule();
            foreach (LifecycleRule rule in new[] { expected, actual })
            {
                rule.Filter.Not = new[]
                {
                    new BucketLifecycleNotFilter { Prefix = "excluded/", Tags = new[] { new Tag { Key = "keep", Value = "true" } } }
                };
            }

            switch (change)
            {
                case "tag-key": actual.Filter.Not[0].Tags[0].Key = "other"; break;
                case "tag-value": actual.Filter.Not[0].Tags[0].Value = "false"; break;
                case "tags-null": actual.Filter.Not[0].Tags = null; break;
                case "tags-empty": actual.Filter.Not[0].Tags = new Tag[0]; break;
                case "prefix": actual.Filter.Not[0].Prefix = "other/"; break;
                case "missing-entry": actual.Filter.Not = new BucketLifecycleNotFilter[0]; break;
                case "null-entry": actual.Filter.Not[0] = null; break;
            }

            Assert.Throws<AssertionException>(() => LifecycleTestSupport.AssertRuleEqual(expected, actual));
        }

        [Test]
        public void TestPutBucketLifecycleRequest()
        {
            PutBucketLifecycleInput input = new PutBucketLifecycleInput
            {
                Bucket = "example-bucket",
                AllowSameActionOverlap = true,
                Rules = new[]
                {
                    new LifecycleRule
                    {
                        ID = "rule-1",
                        Prefix = "logs/",
                        Status = StatusType.StatusEnabled,
                        Transitions = new[]
                        {
                            new Transition
                            {
                                Date = new DateTime(2030, 1, 1, 0, 0, 0),
                                StorageClass = StorageClassType.StorageClassIa
                            }
                        },
                        Expiration = new Expiration { Days = 365 },
                        NoncurrentVersionTransitions = new[]
                        {
                            new NonCurrentVersionTransition
                            {
                                NonCurrentDays = 30,
                                StorageClass = StorageClassType.StorageClassArchive
                            }
                        },
                        NoncurrentVersionExpiration = new NonCurrentVersionExpiration
                        {
                            NonCurrentDate = new DateTime(2031, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                        },
                        Tags = new[] { new Tag { Key = "env", Value = "prod" } },
                        AbortIncompleteMultipartUpload = new AbortIncompleteMultipartUpload
                        {
                            DaysAfterInitiation = 7
                        },
                        Filter = new LifecycleRuleFilter
                        {
                            ObjectSizeGreaterThan = 1024,
                            GreaterThanIncludeEqual = StatusType.StatusEnabled,
                            ObjectSizeLessThan = 1048576,
                            LessThanIncludeEqual = StatusType.StatusDisabled,
                            Not = new[]
                            {
                                new BucketLifecycleNotFilter
                                {
                                    Prefix = "logs/keep/",
                                    Tags = new[] { new Tag { Key = "keep", Value = "true" } }
                                }
                            }
                        },
                        AccessTimeTransitions = new[]
                        {
                            new AccessTimeTransition
                            {
                                Days = 60,
                                StorageClass = StorageClassType.StorageClassIa
                            }
                        },
                        NonCurrentVersionAccessTimeTransitions = new[]
                        {
                            new NonCurrentVersionAccessTimeTransition
                            {
                                NonCurrentDays = 90,
                                StorageClass = StorageClassType.StorageClassColdArchive
                            }
                        }
                    }
                }
            };

            object request = Trans(input);
            Assert.That(GetPropertyValue(request, "Operation"), Is.EqualTo("PutBucketLifecycle"));
            Assert.That(GetPropertyValue(request, "Method"), Is.EqualTo(HttpMethodType.HttpMethodPut));
            Assert.That(GetPropertyValue(request, "Bucket"), Is.EqualTo("example-bucket"));

            IDictionary<string, string> query = GetStringDictionary(request, "Query");
            Assert.That(query.ContainsKey("lifecycle"), Is.True);

            IDictionary<string, string> header = GetStringDictionary(request, "Header");
            Assert.That(header["x-tos-allow-same-action-overlap"], Is.EqualTo("true"));
            Assert.That(header.ContainsKey("Content-MD5"), Is.True);

            string body = ReadBody(request);
            Assert.That(header["Content-Length"], Is.EqualTo(Encoding.UTF8.GetByteCount(body).ToString()));
            Assert.That(header["Content-MD5"], Is.EqualTo(Base64Md5(Encoding.UTF8.GetBytes(body))));
            StringAssert.Contains("\"Status\": \"Enabled\"", body);
            StringAssert.Contains("\"Date\": \"2030-01-01T00:00:00Z\"", body);
            StringAssert.Contains("\"NoncurrentVersionTransitions\"", body);
            StringAssert.Contains("\"NoncurrentVersionExpiration\"", body);
            StringAssert.Contains("\"AbortIncompleteMultipartUpload\"", body);
            StringAssert.Contains("\"NonCurrentVersionAccessTimeTransitions\"", body);
            StringAssert.Contains("\"Not\"", body);
        }

        [Test]
        public void TestLifecycleRoundTripWithDaySchedules()
        {
            LifecycleRule source = new LifecycleRule
            {
                ID = "days-rule",
                Prefix = "days/",
                Status = StatusType.StatusEnabled,
                Transitions = new[]
                {
                    new Transition
                    {
                        Days = 30,
                        StorageClass = StorageClassType.StorageClassIa
                    }
                },
                Expiration = new Expiration { Days = 365 },
                NoncurrentVersionTransitions = new[]
                {
                    new NonCurrentVersionTransition
                    {
                        NonCurrentDays = 60,
                        StorageClass = StorageClassType.StorageClassArchive
                    }
                },
                NoncurrentVersionExpiration = new NonCurrentVersionExpiration { NonCurrentDays = 90 },
                Tags = new[] { new Tag { Key = "type", Value = "days" } },
                Filter = new LifecycleRuleFilter
                {
                    ObjectSizeGreaterThan = 1024,
                    GreaterThanIncludeEqual = StatusType.StatusEnabled,
                    ObjectSizeLessThan = 1048576,
                    LessThanIncludeEqual = StatusType.StatusDisabled
                }
            };

            LifecycleRule result = RoundTrip(source, false);
            Assert.That(result.ID, Is.EqualTo(source.ID));
            Assert.That(result.Prefix, Is.EqualTo(source.Prefix));
            Assert.That(result.Status, Is.EqualTo(StatusType.StatusEnabled));
            Assert.That(result.Transitions[0].Days, Is.EqualTo(30));
            Assert.That(result.Transitions[0].Date, Is.Null);
            Assert.That(result.Expiration.Days, Is.EqualTo(365));
            Assert.That(result.Expiration.Date, Is.Null);
            Assert.That(result.NoncurrentVersionTransitions[0].NonCurrentDays, Is.EqualTo(60));
            Assert.That(result.NoncurrentVersionTransitions[0].NonCurrentDate, Is.Null);
            Assert.That(result.NoncurrentVersionExpiration.NonCurrentDays, Is.EqualTo(90));
            Assert.That(result.Tags[0].Value, Is.EqualTo("days"));
            Assert.That(result.Filter.ObjectSizeGreaterThan, Is.EqualTo(1024));
            Assert.That(result.Filter.GreaterThanIncludeEqual, Is.EqualTo(StatusType.StatusEnabled));
            Assert.That(result.Filter.ObjectSizeLessThan, Is.EqualTo(1048576));
            Assert.That(result.Filter.LessThanIncludeEqual, Is.EqualTo(StatusType.StatusDisabled));
        }

        [Test]
        public void TestLifecycleRoundTripWithDateSchedules()
        {
            DateTime transitionDate = new DateTime(2030, 12, 31, 0, 0, 0, DateTimeKind.Utc);
            DateTime expirationDate = new DateTime(2031, 12, 31, 0, 0, 0, DateTimeKind.Utc);
            DateTime noncurrentTransitionDate = new DateTime(2032, 12, 31, 0, 0, 0, DateTimeKind.Utc);
            DateTime noncurrentExpirationDate = new DateTime(2033, 12, 31, 0, 0, 0, DateTimeKind.Utc);

            LifecycleRule source = new LifecycleRule
            {
                ID = "date-rule",
                Prefix = "date/",
                Status = StatusType.StatusEnabled,
                Transitions = new[]
                {
                    new Transition
                    {
                        Date = transitionDate,
                        StorageClass = StorageClassType.StorageClassIa
                    }
                },
                Expiration = new Expiration { Date = expirationDate },
                NoncurrentVersionTransitions = new[]
                {
                    new NonCurrentVersionTransition
                    {
                        NonCurrentDate = noncurrentTransitionDate,
                        StorageClass = StorageClassType.StorageClassArchive
                    }
                },
                NoncurrentVersionExpiration = new NonCurrentVersionExpiration
                {
                    NonCurrentDate = noncurrentExpirationDate
                }
            };

            LifecycleRule result = RoundTrip(source, true);
            Assert.That(result.Transitions[0].Days, Is.Null);
            Assert.That(result.Transitions[0].Date, Is.EqualTo(transitionDate));
            Assert.That(result.Expiration.Days, Is.Null);
            Assert.That(result.Expiration.Date, Is.EqualTo(expirationDate));
            Assert.That(result.NoncurrentVersionTransitions[0].NonCurrentDays, Is.Null);
            Assert.That(result.NoncurrentVersionTransitions[0].NonCurrentDate,
                Is.EqualTo(noncurrentTransitionDate));
            Assert.That(result.NoncurrentVersionExpiration.NonCurrentDays, Is.Null);
            Assert.That(result.NoncurrentVersionExpiration.NonCurrentDate,
                Is.EqualTo(noncurrentExpirationDate));
        }

        [Test]
        public void TestPutBucketLifecycleMultipleRulesAndOverlapFalse()
        {
            PutBucketLifecycleInput input = new PutBucketLifecycleInput
            {
                Bucket = "example-bucket",
                AllowSameActionOverlap = false,
                Rules = new[]
                {
                    new LifecycleRule
                    {
                        ID = "rule-1",
                        Prefix = "logs/",
                        Status = StatusType.StatusEnabled,
                        Expiration = new Expiration { Days = 30 }
                    },
                    new LifecycleRule
                    {
                        ID = "rule-2",
                        Prefix = "logs/archive/",
                        Status = StatusType.StatusDisabled,
                        Expiration = new Expiration { Days = 60 }
                    }
                }
            };

            object request = Trans(input);
            IDictionary<string, string> header = GetStringDictionary(request, "Header");
            Assert.That(header["x-tos-allow-same-action-overlap"], Is.EqualTo("false"));

            GetBucketLifecycleOutput output = ParseGetOutput(ReadBody(request), false);
            Assert.That(output.AllowSameActionOverlap, Is.False);
            Assert.That(output.Rules.Length, Is.EqualTo(2));
            Assert.That(output.Rules[0].ID, Is.EqualTo("rule-1"));
            Assert.That(output.Rules[1].ID, Is.EqualTo("rule-2"));
            Assert.That(output.Rules[1].Status, Is.EqualTo(StatusType.StatusDisabled));
            Assert.That(output.Rules[1].Expiration.Days, Is.EqualTo(60));
        }

        [Test]
        public void TestGetBucketLifecycleResponse()
        {
            const string body = "{\"Rules\":[{" +
                                "\"ID\":\"rule-1\",\"Prefix\":\"logs/\",\"Status\":\"Enabled\"," +
                                "\"Transitions\":[{\"Days\":30,\"StorageClass\":\"IA\"}]," +
                                "\"Expiration\":{\"Date\":\"2030-01-01T00:00:00Z\"}," +
                                "\"NoncurrentVersionTransitions\":[{" +
                                "\"NoncurrentDate\":\"2031-01-01T00:00:00Z\"," +
                                "\"StorageClass\":\"ARCHIVE\"}]," +
                                "\"NoncurrentVersionExpiration\":{\"NoncurrentDays\":90}," +
                                "\"Tags\":[{\"Key\":\"env\",\"Value\":\"prod\"}]," +
                                "\"AbortIncompleteMultipartUpload\":{\"DaysAfterInitiation\":7}," +
                                "\"Filter\":{\"ObjectSizeGreaterThan\":1024," +
                                "\"GreaterThanIncludeEqual\":\"Enabled\",\"Not\":[{" +
                                "\"Prefix\":\"logs/keep/\",\"Tags\":[{\"Key\":\"keep\",\"Value\":\"true\"}]}]}," +
                                "\"AccessTimeTransitions\":[{\"Days\":60,\"StorageClass\":\"IA\"}]," +
                                "\"NonCurrentVersionAccessTimeTransitions\":[{" +
                                "\"NonCurrentDays\":120,\"StorageClass\":\"COLD_ARCHIVE\"}]}]}";

            GetBucketLifecycleOutput output = ParseGetOutput(body, true);
            Assert.That(output.AllowSameActionOverlap, Is.True);
            Assert.That(output.Rules.Length, Is.EqualTo(1));

            LifecycleRule rule = output.Rules[0];
            Assert.That(rule.ID, Is.EqualTo("rule-1"));
            Assert.That(rule.Status, Is.EqualTo(StatusType.StatusEnabled));
            Assert.That(rule.Transitions[0].Days, Is.EqualTo(30));
            Assert.That(rule.Transitions[0].StorageClass, Is.EqualTo(StorageClassType.StorageClassIa));
            Assert.That(rule.Expiration.Date.Value.Year, Is.EqualTo(2030));
            Assert.That(rule.NoncurrentVersionTransitions[0].NonCurrentDate.Value.Year, Is.EqualTo(2031));
            Assert.That(rule.NoncurrentVersionExpiration.NonCurrentDays, Is.EqualTo(90));
            Assert.That(rule.Tags[0].Value, Is.EqualTo("prod"));
            Assert.That(rule.AbortIncompleteMultipartUpload.DaysAfterInitiation, Is.EqualTo(7));
            Assert.That(rule.Filter.ObjectSizeGreaterThan, Is.EqualTo(1024));
            Assert.That(rule.Filter.Not[0].Prefix, Is.EqualTo("logs/keep/"));
            Assert.That(rule.AccessTimeTransitions[0].Days, Is.EqualTo(60));
            Assert.That(rule.NonCurrentVersionAccessTimeTransitions[0].NonCurrentDays, Is.EqualTo(120));
        }

        [Test]
        public void TestGetBucketLifecycleResponseDefaults()
        {
            GetBucketLifecycleOutput output = ParseGetOutput(
                "{\"Rules\":[{\"ID\":\"minimal\",\"Status\":\"Disabled\"}]}", null);

            Assert.That(output.AllowSameActionOverlap, Is.False);
            Assert.That(output.Rules.Length, Is.EqualTo(1));
            LifecycleRule rule = output.Rules[0];
            Assert.That(rule.Status, Is.EqualTo(StatusType.StatusDisabled));
            Assert.That(rule.Transitions, Is.Empty);
            Assert.That(rule.NoncurrentVersionTransitions, Is.Empty);
            Assert.That(rule.Tags, Is.Empty);
            Assert.That(rule.AccessTimeTransitions, Is.Empty);
            Assert.That(rule.NonCurrentVersionAccessTimeTransitions, Is.Empty);
            Assert.That(rule.Expiration, Is.Null);
            Assert.That(rule.NoncurrentVersionExpiration, Is.Null);
            Assert.That(rule.AbortIncompleteMultipartUpload, Is.Null);
            Assert.That(rule.Filter, Is.Null);

            output = ParseGetOutput("{}", null);
            Assert.That(output.Rules, Is.Empty);
            Assert.That(output.AllowSameActionOverlap, Is.False);

            output = ParseGetOutput("{\"Rules\":[null]}", null);
            Assert.That(output.Rules.Length, Is.EqualTo(1));
            Assert.That(output.Rules[0].Transitions, Is.Empty);
            Assert.That(output.Rules[0].NoncurrentVersionTransitions, Is.Empty);
            Assert.That(output.Rules[0].Tags, Is.Empty);
            Assert.That(output.Rules[0].AccessTimeTransitions, Is.Empty);
            Assert.That(output.Rules[0].NonCurrentVersionAccessTimeTransitions, Is.Empty);
        }

        [Test]
        public void TestGetBucketLifecycleUnknownEnumsRemainUnset()
        {
            const string body = "{\"Rules\":[{\"Status\":\"FutureStatus\"," +
                                "\"Transitions\":[{\"Days\":30,\"StorageClass\":\"FUTURE_CLASS\"}]}]}";
            GetBucketLifecycleOutput output = ParseGetOutput(body, "not-a-boolean");

            Assert.That(output.AllowSameActionOverlap, Is.False);
            Assert.That(output.Rules[0].Status, Is.Null);
            Assert.That(output.Rules[0].Transitions[0].StorageClass, Is.Null);
        }

        [Test]
        public void TestLifecycleGetAndDeleteRequest()
        {
            object getRequest = Trans(new GetBucketLifecycleInput { Bucket = "example-bucket" });
            Assert.That(GetPropertyValue(getRequest, "Operation"), Is.EqualTo("GetBucketLifecycle"));
            Assert.That(GetPropertyValue(getRequest, "Bucket"), Is.EqualTo("example-bucket"));
            Assert.That(GetPropertyValue(getRequest, "Method"), Is.EqualTo(HttpMethodType.HttpMethodGet));
            Assert.That(GetStringDictionary(getRequest, "Query").Count, Is.EqualTo(1));
            Assert.That(GetStringDictionary(getRequest, "Query").ContainsKey("lifecycle"), Is.True);
            Assert.That(GetPropertyValue(getRequest, "Body"), Is.Null);

            object deleteRequest = Trans(new DeleteBucketLifecycleInput { Bucket = "example-bucket" });
            Assert.That(GetPropertyValue(deleteRequest, "Operation"), Is.EqualTo("DeleteBucketLifecycle"));
            Assert.That(GetPropertyValue(deleteRequest, "Bucket"), Is.EqualTo("example-bucket"));
            Assert.That(GetPropertyValue(deleteRequest, "Method"), Is.EqualTo(HttpMethodType.HttpMethodDelete));
            Assert.That(GetStringDictionary(deleteRequest, "Query").Count, Is.EqualTo(1));
            Assert.That(GetStringDictionary(deleteRequest, "Query").ContainsKey("lifecycle"), Is.True);
            Assert.That(GetPropertyValue(deleteRequest, "Body"), Is.Null);
        }

        [Test]
        public void TestPutBucketLifecycleRejectsInvalidRules()
        {
            AssertValidation(new PutBucketLifecycleInput
            {
                Bucket = "example-bucket",
                Rules = null
            }, "empty rules");

            AssertValidation(new PutBucketLifecycleInput
            {
                Bucket = "example-bucket",
                Rules = new LifecycleRule[0]
            }, "empty rules");

            AssertValidation(PutInput(null), "null rule");
            AssertValidation(PutInput(new LifecycleRule()), "invalid lifecycle rule status");
            AssertValidation(PutInput(new LifecycleRule { Status = (StatusType)999 }),
                "invalid lifecycle rule status");
        }

        [Test]
        public void TestPutBucketLifecycleRejectsInvalidCurrentSchedules()
        {
            AssertValidation(PutInput(EnabledRule(new Transition
            {
                StorageClass = StorageClassType.StorageClassIa
            })), "must set exactly one");

            AssertValidation(PutInput(EnabledRule(new Transition
            {
                Days = 30,
                Date = new DateTime(2030, 1, 1),
                StorageClass = StorageClassType.StorageClassIa
            })), "must set exactly one");

            AssertValidation(PutInput(EnabledRule(new Transition
            {
                Days = 0,
                StorageClass = StorageClassType.StorageClassIa
            })), "invalid days");

            AssertValidation(PutInput(EnabledRule(new Transition { Days = 30 })),
                "invalid storage class");
            AssertValidation(PutInput(EnabledRule(new Transition
            {
                Days = 30,
                StorageClass = (StorageClassType)999
            })), "invalid storage class");

            AssertValidation(PutInput(new LifecycleRule
            {
                Status = StatusType.StatusEnabled,
                Expiration = new Expiration()
            }), "must set exactly one");

            AssertValidation(PutInput(new LifecycleRule
            {
                Status = StatusType.StatusEnabled,
                Expiration = new Expiration
                {
                    Days = 30,
                    Date = new DateTime(2030, 1, 1)
                }
            }), "must set exactly one");

            AssertValidation(PutInput(new LifecycleRule
            {
                Status = StatusType.StatusEnabled,
                Expiration = new Expiration { Days = -1 }
            }), "invalid days");
        }

        [Test]
        public void TestPutBucketLifecycleRejectsInvalidNoncurrentSchedules()
        {
            AssertValidation(PutInput(new LifecycleRule
            {
                Status = StatusType.StatusEnabled,
                NoncurrentVersionTransitions = new[]
                {
                    new NonCurrentVersionTransition
                    {
                        StorageClass = StorageClassType.StorageClassArchive
                    }
                }
            }), "must set exactly one");

            AssertValidation(PutInput(new LifecycleRule
            {
                Status = StatusType.StatusEnabled,
                NoncurrentVersionTransitions = new[]
                {
                    new NonCurrentVersionTransition
                    {
                        NonCurrentDays = 30,
                        NonCurrentDate = new DateTime(2030, 1, 1),
                        StorageClass = StorageClassType.StorageClassArchive
                    }
                }
            }), "must set exactly one");

            AssertValidation(PutInput(new LifecycleRule
            {
                Status = StatusType.StatusEnabled,
                NoncurrentVersionTransitions = new[]
                {
                    new NonCurrentVersionTransition { NonCurrentDays = 30 }
                }
            }), "invalid storage class");

            AssertValidation(PutInput(new LifecycleRule
            {
                Status = StatusType.StatusEnabled,
                NoncurrentVersionExpiration = new NonCurrentVersionExpiration()
            }), "must set exactly one");

            AssertValidation(PutInput(new LifecycleRule
            {
                Status = StatusType.StatusEnabled,
                NoncurrentVersionExpiration = new NonCurrentVersionExpiration
                {
                    NonCurrentDays = 30,
                    NonCurrentDate = new DateTime(2030, 1, 1)
                }
            }), "must set exactly one");
        }

        [Test]
        public void TestPutBucketLifecycleRejectsInvalidAccessAndAbortActions()
        {
            AssertValidation(PutInput(new LifecycleRule
            {
                Status = StatusType.StatusEnabled,
                AbortIncompleteMultipartUpload = new AbortIncompleteMultipartUpload()
            }), "invalid days after initiation");

            AssertValidation(PutInput(new LifecycleRule
            {
                Status = StatusType.StatusEnabled,
                AccessTimeTransitions = new[]
                {
                    new AccessTimeTransition { StorageClass = StorageClassType.StorageClassIa }
                }
            }), "invalid days for access time transition");

            AssertValidation(PutInput(new LifecycleRule
            {
                Status = StatusType.StatusEnabled,
                AccessTimeTransitions = new[] { new AccessTimeTransition { Days = 30 } }
            }), "invalid storage class");

            AssertValidation(PutInput(new LifecycleRule
            {
                Status = StatusType.StatusEnabled,
                NonCurrentVersionAccessTimeTransitions = new[]
                {
                    new NonCurrentVersionAccessTimeTransition
                    {
                        StorageClass = StorageClassType.StorageClassIa
                    }
                }
            }), "invalid days for noncurrent access time transition");

            AssertValidation(PutInput(new LifecycleRule
            {
                Status = StatusType.StatusEnabled,
                NonCurrentVersionAccessTimeTransitions = new[]
                {
                    new NonCurrentVersionAccessTimeTransition { NonCurrentDays = 30 }
                }
            }), "invalid storage class");
        }

        [Test]
        public void TestPutBucketLifecycleRejectsInvalidFiltersAndTags()
        {
            AssertValidation(PutInput(new LifecycleRule
            {
                Status = StatusType.StatusEnabled,
                Filter = new LifecycleRuleFilter { ObjectSizeGreaterThan = -1 }
            }), "invalid object size");

            AssertValidation(PutInput(new LifecycleRule
            {
                Status = StatusType.StatusEnabled,
                Filter = new LifecycleRuleFilter { ObjectSizeLessThan = -1 }
            }), "invalid object size");

            AssertValidation(PutInput(new LifecycleRule
            {
                Status = StatusType.StatusEnabled,
                Filter = new LifecycleRuleFilter { GreaterThanIncludeEqual = (StatusType)999 }
            }), "invalid status");

            AssertValidation(PutInput(new LifecycleRule
            {
                Status = StatusType.StatusEnabled,
                Filter = new LifecycleRuleFilter
                {
                    Not = new BucketLifecycleNotFilter[] { null }
                }
            }), "null not filter");

            AssertValidation(PutInput(new LifecycleRule
            {
                Status = StatusType.StatusEnabled,
                Filter = new LifecycleRuleFilter
                {
                    Not = new[] { new BucketLifecycleNotFilter() }
                }
            }), "empty not filter");

            AssertValidation(PutInput(new LifecycleRule
            {
                Status = StatusType.StatusEnabled,
                Tags = new Tag[] { null }
            }), "invalid tag");

            AssertValidation(PutInput(new LifecycleRule
            {
                Status = StatusType.StatusEnabled,
                Tags = new[] { new Tag { Key = string.Empty, Value = "value" } }
            }), "invalid tag");

            AssertValidation(PutInput(new LifecycleRule
            {
                Status = StatusType.StatusEnabled,
                Filter = new LifecycleRuleFilter
                {
                    Not = new[]
                    {
                        new BucketLifecycleNotFilter
                        {
                            Prefix = "excluded/",
                            Tags = new[] { new Tag { Key = "key", Value = string.Empty } }
                        }
                    }
                }
            }), "invalid tag");
        }

        [Test]
        public void TestPutBucketLifecycleRejectsNullNestedItems()
        {
            AssertValidation(PutInput(new LifecycleRule
            {
                Status = StatusType.StatusEnabled,
                Transitions = new Transition[] { null }
            }), "null lifecycle transition");

            AssertValidation(PutInput(new LifecycleRule
            {
                Status = StatusType.StatusEnabled,
                NoncurrentVersionTransitions = new NonCurrentVersionTransition[] { null }
            }), "null noncurrent version transition");

            AssertValidation(PutInput(new LifecycleRule
            {
                Status = StatusType.StatusEnabled,
                AccessTimeTransitions = new AccessTimeTransition[] { null }
            }), "null access time transition");

            AssertValidation(PutInput(new LifecycleRule
            {
                Status = StatusType.StatusEnabled,
                NonCurrentVersionAccessTimeTransitions =
                    new NonCurrentVersionAccessTimeTransition[] { null }
            }), "null noncurrent access time transition");
        }

        [Test]
        public void TestPutBucketLifecycleAcceptsPrefixOnlyNotFilter()
        {
            object request = Trans(PutInput(new LifecycleRule
            {
                Status = StatusType.StatusEnabled,
                Expiration = new Expiration { Days = 30 },
                Filter = new LifecycleRuleFilter
                {
                    Not = new[]
                    {
                        new BucketLifecycleNotFilter { Prefix = "excluded/" }
                    }
                }
            }));

            StringAssert.Contains("\"Prefix\": \"excluded/\"", ReadBody(request));
        }

        [Test]
        public void TestLifecycleInterfacesAreAdditive()
        {
            Assert.That(typeof(ITosClient).GetMethod("PutBucketLifecycle"), Is.Null);
            Assert.That(typeof(ITosClient).IsAssignableFrom(typeof(ITosClientV2)), Is.True);
            Assert.That(typeof(ITosLifecycleClient).IsAssignableFrom(typeof(ITosClientV2)), Is.True);
            Assert.That(typeof(TosClientBuilder).GetMethod("BuildV2"), Is.Not.Null);
        }

        [TestCase("NonCurrentVersionAccessTimeTransitions", "NonCurrentDays")]
        [TestCase("NoncurrentVersionAccessTimeTransitions", "NoncurrentDays")]
        [TestCase("NoncurrentVersionAccessTimeTransitions", "NonCurrentDays")]
        [TestCase("NonCurrentVersionAccessTimeTransitions", "NoncurrentDays")]
        public void TestAccessTimeResponseCasingAcrossLifecycleApis(string transitions, string days)
        {
            string body = "{\"Rules\":[{\"ID\":\"access-time\",\"" + transitions +
                "\":[{\"StorageClass\":\"IA\",\"" + days + "\":20}]}]}";
            IDictionary<string, string> header = new Dictionary<string, string>();
            GetBucketLifecycleOutput bucket = LifecycleTestSupport.Parse<GetBucketLifecycleOutput>(header, body, null);
            GetObjectSetLifecycleOutput objectSet = LifecycleTestSupport.Parse<GetObjectSetLifecycleOutput>(header, body, null);
            GetObjectSetLifecycleByTagOutput byTag = LifecycleTestSupport.Parse<GetObjectSetLifecycleByTagOutput>(
                header, "{\"ObjectSetTagRules\":[" + body + "]}", null);
            foreach (LifecycleRule rule in new[] { bucket.Rules[0], objectSet.Rules[0], byTag.ObjectSetTagRules[0].Rules[0] })
            {
                Assert.That(rule.NonCurrentVersionAccessTimeTransitions.Length, Is.EqualTo(1));
                Assert.That(rule.NonCurrentVersionAccessTimeTransitions[0].NonCurrentDays, Is.EqualTo(20));
                Assert.That(rule.NonCurrentVersionAccessTimeTransitions[0].StorageClass, Is.EqualTo(StorageClassType.StorageClassIa));
            }
        }

        [Test]
        public void TestNoncurrentResponseCasingRoundTrip(
            [Values("Bucket", "ObjectSet", "ByTag")] string scope,
            [Values("Noncurrent", "NonCurrent")] string containerPrefix,
            [Values("Noncurrent", "NonCurrent")] string fieldPrefix,
            [Values("Days", "Date")] string field)
        {
            string value = field == "Days" ? "30" : "\"2035-01-01T00:00:00Z\"";
            string ruleJson = "{\"Status\":\"Enabled\",\"" + containerPrefix +
                "VersionTransitions\":[{\"StorageClass\":\"IA\",\"" + fieldPrefix + field + "\":" + value +
                "}],\"" + containerPrefix + "VersionExpiration\":{\"" + fieldPrefix + field + "\":" + value + "}}";
            LifecycleRule rule = ParseLifecycleRule(scope, ruleJson);
            DateTime? date = field == "Date" ? new DateTime(2035, 1, 1, 0, 0, 0, DateTimeKind.Utc) : (DateTime?)null;
            int? days = field == "Days" ? 30 : (int?)null;
            Assert.That(rule.NoncurrentVersionTransitions.Length, Is.EqualTo(1));
            Assert.That(rule.NoncurrentVersionTransitions[0].NonCurrentDays, Is.EqualTo(days));
            Assert.That(rule.NoncurrentVersionTransitions[0].NonCurrentDate, Is.EqualTo(date));
            Assert.That(rule.NoncurrentVersionTransitions[0].StorageClass, Is.EqualTo(StorageClassType.StorageClassIa));
            Assert.That(rule.NoncurrentVersionExpiration, Is.Not.Null);
            Assert.That(rule.NoncurrentVersionExpiration.NonCurrentDays, Is.EqualTo(days));
            Assert.That(rule.NoncurrentVersionExpiration.NonCurrentDate, Is.EqualTo(date));

            // A documented GET response must be reusable in PUT, which keeps the canonical wire names.
            JObject request = JObject.Parse(LifecycleTestSupport.Body(Trans(LifecyclePutInput(scope, new[] { rule }))));
            JObject written = (JObject)(scope == "ByTag"
                ? request["ObjectSetTagRules"][0]["Rules"][0] : request["Rules"][0]);
            JObject expected = JObject.Parse("{\"Status\":\"Enabled\",\"NoncurrentVersionTransitions\":[{" +
                "\"StorageClass\":\"IA\",\"Noncurrent" + field + "\":" + value +
                "}],\"NoncurrentVersionExpiration\":{\"Noncurrent" + field + "\":" + value + "}}");
            Assert.That(JToken.DeepEquals(written, expected), Is.True, written.ToString());
            LifecycleTestSupport.AssertRuleEqual(rule, ParseLifecycleRule(scope, written.ToString()));
        }

        [Test]
        public void TestNoncurrentResponseEmptyValues(
            [Values("Bucket", "ObjectSet", "ByTag")] string scope,
            [Values("missing", "null", "empty", "missing-fields", "null-fields", "null-item")] string shape)
        {
            string ruleJson = "{}";
            if (shape == "null")
            {
                ruleJson = "{\"NonCurrentVersionTransitions\":null,\"NonCurrentVersionExpiration\":null}";
            }
            else if (shape == "empty")
            {
                ruleJson = "{\"NonCurrentVersionTransitions\":[],\"NonCurrentVersionExpiration\":{}}";
            }
            else if (shape == "missing-fields" || shape == "null-fields" || shape == "null-item")
            {
                string item = shape == "null-item" ? "null" : shape == "missing-fields" ? "{}" :
                    "{\"NonCurrentDays\":null,\"NonCurrentDate\":null}";
                ruleJson = "{\"NonCurrentVersionTransitions\":[" + item + "],\"NonCurrentVersionExpiration\":" + item + "}";
            }

            LifecycleRule rule = ParseLifecycleRule(scope, ruleJson);
            Assert.That(rule.NoncurrentVersionTransitions, Is.Not.Null);
            bool hasItem = shape == "missing-fields" || shape == "null-fields" || shape == "null-item";
            Assert.That(rule.NoncurrentVersionTransitions.Length, Is.EqualTo(hasItem ? 1 : 0));
            if (hasItem)
            {
                Assert.That(rule.NoncurrentVersionTransitions[0].NonCurrentDays, Is.Null);
                Assert.That(rule.NoncurrentVersionTransitions[0].NonCurrentDate, Is.Null);
                Assert.That(rule.NoncurrentVersionTransitions[0].StorageClass, Is.Null);
            }
            if (shape == "empty" || shape == "missing-fields" || shape == "null-fields")
            {
                Assert.That(rule.NoncurrentVersionExpiration, Is.Not.Null);
                Assert.That(rule.NoncurrentVersionExpiration.NonCurrentDays, Is.Null);
                Assert.That(rule.NoncurrentVersionExpiration.NonCurrentDate, Is.Null);
            }
            else
            {
                Assert.That(rule.NoncurrentVersionExpiration, Is.Null);
            }
        }

        [Test]
        public void TestNoncurrentCanonicalContainerTakesPrecedence(
            [Values("Bucket", "ObjectSet", "ByTag")] string scope,
            [Values(false, true)] bool canonicalNull,
            [Values(false, true)] bool aliasFirst)
        {
            string canonical = "\"NoncurrentVersionTransitions\":" +
                (canonicalNull ? "null" : "[{\"NoncurrentDays\":30}]") +
                ",\"NoncurrentVersionExpiration\":" + (canonicalNull ? "null" : "{\"NoncurrentDays\":60}");
            const string alias = "\"NonCurrentVersionTransitions\":[{\"NoncurrentDays\":90}]," +
                "\"NonCurrentVersionExpiration\":{\"NoncurrentDays\":120}";
            LifecycleRule rule = ParseLifecycleRule(scope, "{" +
                (aliasFirst ? alias + "," + canonical : canonical + "," + alias) + "}");
            Assert.That(rule.NoncurrentVersionTransitions.Length, Is.EqualTo(canonicalNull ? 0 : 1));
            if (canonicalNull)
            {
                Assert.That(rule.NoncurrentVersionExpiration, Is.Null);
            }
            else
            {
                Assert.That(rule.NoncurrentVersionTransitions[0].NonCurrentDays, Is.EqualTo(30));
                Assert.That(rule.NoncurrentVersionExpiration.NonCurrentDays, Is.EqualTo(60));
            }
        }

        [Test]
        public void TestNoncurrentCanonicalFieldsTakePrecedence(
            [Values("Bucket", "ObjectSet", "ByTag")] string scope,
            [Values(false, true)] bool canonicalNull,
            [Values(false, true)] bool aliasFirst)
        {
            string canonical = "\"NoncurrentDays\":" + (canonicalNull ? "null" : "30") +
                ",\"NoncurrentDate\":" + (canonicalNull ? "null" : "\"2035-01-01T00:00:00Z\"");
            const string alias = "\"NonCurrentDays\":90,\"NonCurrentDate\":\"2040-01-01T00:00:00Z\"";
            string fields = "{" + (aliasFirst ? alias + "," + canonical : canonical + "," + alias) + "}";
            // Parser-only fixture: conflicting aliases must not override an exact name, even when its value is null.
            LifecycleRule rule = ParseLifecycleRule(scope, "{\"NoncurrentVersionTransitions\":[" + fields +
                "],\"NoncurrentVersionExpiration\":" + fields + "}");
            int? days = canonicalNull ? (int?)null : 30;
            DateTime? date = canonicalNull ? (DateTime?)null : new DateTime(2035, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            Assert.That(rule.NoncurrentVersionTransitions[0].NonCurrentDays, Is.EqualTo(days));
            Assert.That(rule.NoncurrentVersionTransitions[0].NonCurrentDate, Is.EqualTo(date));
            Assert.That(rule.NoncurrentVersionExpiration.NonCurrentDays, Is.EqualTo(days));
            Assert.That(rule.NoncurrentVersionExpiration.NonCurrentDate, Is.EqualTo(date));
        }

        [Test]
        public void TestSharedLifecycleValidationUsesScopeNeutralMessages(
            [Values("Bucket", "ObjectSet", "ByTag")] string scope,
            [Values("null", "empty", "null-rule")] string shape)
        {
            LifecycleRule[] rules = shape == "null" ? null : shape == "empty" ? new LifecycleRule[0] : new LifecycleRule[] { null };
            TosClientException exception = Assert.Throws<TosClientException>(() => Trans(LifecyclePutInput(scope, rules)));
            Assert.That(exception.Message, Is.EqualTo(shape == "null-rule"
                ? "null rule is set for put lifecycle" : "empty rules for put lifecycle"));
        }

        private static LifecycleRule ParseLifecycleRule(string scope, string ruleJson)
        {
            string body = "{\"Rules\":[" + ruleJson + "]}";
            IDictionary<string, string> headers = new Dictionary<string, string>();
            return scope == "Bucket"
                ? LifecycleTestSupport.Parse<GetBucketLifecycleOutput>(headers, body, null).Rules[0]
                : scope == "ObjectSet"
                    ? LifecycleTestSupport.Parse<GetObjectSetLifecycleOutput>(headers, body, null).Rules[0]
                    : LifecycleTestSupport.Parse<GetObjectSetLifecycleByTagOutput>(headers,
                        "{\"ObjectSetTagRules\":[" + body + "]}", null).ObjectSetTagRules[0].Rules[0];
        }

        private static GenericInput LifecyclePutInput(string scope, LifecycleRule[] rules)
        {
            if (scope == "Bucket")
            {
                return new PutBucketLifecycleInput { Bucket = "example-bucket", Rules = rules };
            }
            if (scope == "ObjectSet")
            {
                return new PutObjectSetLifecycleInput { Bucket = "example-bucket", ObjectSetName = "test/set", Rules = rules };
            }
            return new PutObjectSetLifecycleByTagInput
            {
                Bucket = "example-bucket",
                ObjectSetTagRules = new[]
                {
                    new ObjectSetTagLifecycleRule { Tag = new Tag { Key = "lifecycle", Value = "test" }, Rules = rules }
                }
            };
        }

        private static object Trans(GenericInput input)
        {
            MethodInfo transMethod = input.GetType().GetMethod("Trans",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(transMethod);
            try
            {
                return transMethod.Invoke(input, null);
            }
            catch (TargetInvocationException ex)
            {
                if (ex.InnerException != null)
                {
                    throw ex.InnerException;
                }

                throw;
            }
        }

        private static LifecycleRule RoundTrip(LifecycleRule rule, bool allowSameActionOverlap)
        {
            object request = Trans(PutInput(rule, allowSameActionOverlap));
            GetBucketLifecycleOutput output = ParseGetOutput(ReadBody(request), allowSameActionOverlap);
            Assert.That(output.Rules.Length, Is.EqualTo(1));
            Assert.That(output.AllowSameActionOverlap, Is.EqualTo(allowSameActionOverlap));
            return output.Rules[0];
        }

        private static PutBucketLifecycleInput PutInput(LifecycleRule rule)
        {
            return PutInput(rule, false);
        }

        private static PutBucketLifecycleInput PutInput(LifecycleRule rule, bool allowSameActionOverlap)
        {
            return new PutBucketLifecycleInput
            {
                Bucket = "example-bucket",
                Rules = new[] { rule },
                AllowSameActionOverlap = allowSameActionOverlap
            };
        }

        private static LifecycleRule EnabledRule(Transition transition)
        {
            return new LifecycleRule
            {
                Status = StatusType.StatusEnabled,
                Transitions = new[] { transition }
            };
        }

        private static void AssertValidation(PutBucketLifecycleInput input, string expectedMessage)
        {
            TosClientException exception = Assert.Throws<TosClientException>(() => Trans(input));
            StringAssert.Contains(expectedMessage, exception.Message);
        }

        private static GetBucketLifecycleOutput ParseGetOutput(string responseBody, bool allowSameActionOverlap)
        {
            return ParseGetOutput(responseBody, allowSameActionOverlap ? "true" : "false");
        }

        private static GetBucketLifecycleOutput ParseGetOutput(string responseBody, string allowSameActionOverlap)
        {
            Type requestType = typeof(GenericOutput).Assembly.GetType("TOS.Common.HttpRequest");
            Type responseType = typeof(GenericOutput).Assembly.GetType("TOS.Common.HttpResponse");
            object request = CreateInstance(requestType, Type.EmptyTypes, new object[0]);
            object response = CreateInstance(responseType, new[] { typeof(int) }, new object[] { 200 });
            SetPropertyValue(response, "Body", new MemoryStream(Encoding.UTF8.GetBytes(responseBody)));
            IDictionary<string, string> header = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (allowSameActionOverlap != null)
            {
                header["x-tos-allow-same-action-overlap"] = allowSameActionOverlap;
            }

            SetPropertyValue(response, "Header", header);

            RequestInfo requestInfo = new RequestInfo();
            SetPropertyValue(requestInfo, "StatusCode", 200);
            SetPropertyValue(requestInfo, "RequestID", "request-id");
            SetPropertyValue(requestInfo, "ID2", "id-2");
            SetPropertyValue(requestInfo, "Header", new Dictionary<string, string>());

            GetBucketLifecycleOutput output = new GetBucketLifecycleOutput();
            MethodInfo parseMethod = typeof(GenericOutput).GetMethod("Parse",
                BindingFlags.Instance | BindingFlags.NonPublic,
                null,
                new[] { requestType, responseType, typeof(RequestInfo) },
                null);
            Assert.NotNull(parseMethod);
            parseMethod.Invoke(output, new[] { request, response, requestInfo });
            return output;
        }

        private static object CreateInstance(Type type, Type[] parameterTypes, object[] args)
        {
            Assert.NotNull(type);
            ConstructorInfo constructor = type.GetConstructor(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                parameterTypes,
                null);
            Assert.NotNull(constructor);
            return constructor.Invoke(args);
        }

        private static IDictionary<string, string> GetStringDictionary(object instance, string name)
        {
            return (IDictionary<string, string>)GetPropertyValue(instance, name);
        }

        private static string ReadBody(object request)
        {
            Stream body = (Stream)GetPropertyValue(request, "Body");
            body.Position = 0;
            return new StreamReader(body, Encoding.UTF8).ReadToEnd();
        }

        private static object GetPropertyValue(object instance, string name)
        {
            PropertyInfo property = GetProperty(instance, name);
            MethodInfo getter = property.GetGetMethod(true);
            Assert.NotNull(getter);
            return getter.Invoke(instance, null);
        }

        private static void SetPropertyValue(object instance, string name, object value)
        {
            PropertyInfo property = GetProperty(instance, name);
            MethodInfo setter = property.GetSetMethod(true);
            Assert.NotNull(setter);
            setter.Invoke(instance, new[] { value });
        }

        private static PropertyInfo GetProperty(object instance, string name)
        {
            PropertyInfo property = instance.GetType().GetProperty(name,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.NotNull(property);
            return property;
        }

        private static string Base64Md5(byte[] input)
        {
            return Convert.ToBase64String(MD5.Create().ComputeHash(input));
        }
    }
}
