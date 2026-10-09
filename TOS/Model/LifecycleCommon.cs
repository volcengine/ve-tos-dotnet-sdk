/*
 * Copyright (2023) Volcengine
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using Newtonsoft.Json.Linq;
using TOS.Common;
using TOS.Error;

namespace TOS.Model
{
    public class LifecycleRule
    {
        public string ID { set; get; }

        public string Prefix { set; get; }

        public StatusType? Status { set; get; }

        public Transition[] Transitions { set; get; }

        public Expiration Expiration { set; get; }

        public NonCurrentVersionTransition[] NoncurrentVersionTransitions { set; get; }

        public NonCurrentVersionExpiration NoncurrentVersionExpiration { set; get; }

        public Tag[] Tags { set; get; }

        public AbortIncompleteMultipartUpload AbortIncompleteMultipartUpload { set; get; }

        public LifecycleRuleFilter Filter { set; get; }

        public AccessTimeTransition[] AccessTimeTransitions { set; get; }

        public NonCurrentVersionAccessTimeTransition[] NonCurrentVersionAccessTimeTransitions { set; get; }
    }

    public class Transition
    {
        public int? Days { set; get; }

        public DateTime? Date { set; get; }

        public StorageClassType? StorageClass { set; get; }
    }

    public class Expiration
    {
        public int? Days { set; get; }

        public DateTime? Date { set; get; }
    }

    public class NonCurrentVersionTransition
    {
        public int? NonCurrentDays { set; get; }

        public DateTime? NonCurrentDate { set; get; }

        public StorageClassType? StorageClass { set; get; }
    }

    public class NonCurrentVersionExpiration
    {
        public int? NonCurrentDays { set; get; }

        public DateTime? NonCurrentDate { set; get; }
    }

    public class AbortIncompleteMultipartUpload
    {
        public int? DaysAfterInitiation { set; get; }
    }

    public class AccessTimeTransition
    {
        public StorageClassType? StorageClass { set; get; }

        public int? Days { set; get; }
    }

    public class NonCurrentVersionAccessTimeTransition
    {
        public StorageClassType? StorageClass { set; get; }

        public int? NonCurrentDays { set; get; }
    }

    public class LifecycleRuleFilter
    {
        public long? ObjectSizeGreaterThan { set; get; }

        public StatusType? GreaterThanIncludeEqual { set; get; }

        public long? ObjectSizeLessThan { set; get; }

        public StatusType? LessThanIncludeEqual { set; get; }

        public BucketLifecycleNotFilter[] Not { set; get; }
    }

    public class BucketLifecycleNotFilter
    {
        public string Prefix { set; get; }

        public Tag[] Tags { set; get; }
    }

    internal static class LifecycleJsonCodec
    {
        internal static JObject ToJson(LifecycleRule[] rules)
        {
            ValidateRules(rules);

            JArray ruleArray = new JArray();
            foreach (LifecycleRule rule in rules)
            {
                ruleArray.Add(ToJson(rule));
            }

            JObject json = new JObject();
            json["Rules"] = ruleArray;
            return json;
        }

        internal static LifecycleRule[] ParseRules(JObject json)
        {
            JArray rules = json["Rules"] as JArray;
            if (rules == null)
            {
                return new LifecycleRule[0];
            }

            LifecycleRule[] result = new LifecycleRule[rules.Count];
            for (int i = 0; i < rules.Count; i++)
            {
                result[i] = ParseRule(rules[i] as JObject);
            }

            return result;
        }

        private static JObject ToJson(LifecycleRule rule)
        {
            JObject json = new JObject();
            AddString(json, "ID", rule.ID);
            AddString(json, "Prefix", rule.Prefix);
            AddEnum(json, "Status", rule.Status);

            if (rule.Transitions != null && rule.Transitions.Length > 0)
            {
                JArray transitions = new JArray();
                foreach (Transition transition in rule.Transitions)
                {
                    if (transition != null)
                    {
                        transitions.Add(ToJson(transition));
                    }
                }

                if (transitions.Count > 0)
                {
                    json["Transitions"] = transitions;
                }
            }

            if (rule.Expiration != null)
            {
                json["Expiration"] = ToJson(rule.Expiration);
            }

            if (rule.NoncurrentVersionTransitions != null && rule.NoncurrentVersionTransitions.Length > 0)
            {
                JArray transitions = new JArray();
                foreach (NonCurrentVersionTransition transition in rule.NoncurrentVersionTransitions)
                {
                    if (transition != null)
                    {
                        transitions.Add(ToJson(transition));
                    }
                }

                if (transitions.Count > 0)
                {
                    json["NoncurrentVersionTransitions"] = transitions;
                }
            }

            if (rule.NoncurrentVersionExpiration != null)
            {
                json["NoncurrentVersionExpiration"] = ToJson(rule.NoncurrentVersionExpiration);
            }

            AddTags(json, "Tags", rule.Tags);

            if (rule.AbortIncompleteMultipartUpload != null)
            {
                JObject abort = new JObject();
                AddInt(abort, "DaysAfterInitiation", rule.AbortIncompleteMultipartUpload.DaysAfterInitiation);
                json["AbortIncompleteMultipartUpload"] = abort;
            }

            if (rule.Filter != null)
            {
                json["Filter"] = ToJson(rule.Filter);
            }

            if (rule.AccessTimeTransitions != null && rule.AccessTimeTransitions.Length > 0)
            {
                JArray transitions = new JArray();
                foreach (AccessTimeTransition transition in rule.AccessTimeTransitions)
                {
                    if (transition != null)
                    {
                        JObject item = new JObject();
                        AddEnum(item, "StorageClass", transition.StorageClass);
                        AddInt(item, "Days", transition.Days);
                        transitions.Add(item);
                    }
                }

                if (transitions.Count > 0)
                {
                    json["AccessTimeTransitions"] = transitions;
                }
            }

            if (rule.NonCurrentVersionAccessTimeTransitions != null &&
                rule.NonCurrentVersionAccessTimeTransitions.Length > 0)
            {
                JArray transitions = new JArray();
                foreach (NonCurrentVersionAccessTimeTransition transition in
                         rule.NonCurrentVersionAccessTimeTransitions)
                {
                    if (transition != null)
                    {
                        JObject item = new JObject();
                        AddEnum(item, "StorageClass", transition.StorageClass);
                        AddInt(item, "NonCurrentDays", transition.NonCurrentDays);
                        transitions.Add(item);
                    }
                }

                if (transitions.Count > 0)
                {
                    json["NonCurrentVersionAccessTimeTransitions"] = transitions;
                }
            }

            return json;
        }

        private static JObject ToJson(Transition transition)
        {
            JObject json = new JObject();
            AddInt(json, "Days", transition.Days);
            AddDate(json, "Date", transition.Date);
            AddEnum(json, "StorageClass", transition.StorageClass);
            return json;
        }

        private static JObject ToJson(Expiration expiration)
        {
            JObject json = new JObject();
            AddInt(json, "Days", expiration.Days);
            AddDate(json, "Date", expiration.Date);
            return json;
        }

        private static JObject ToJson(NonCurrentVersionTransition transition)
        {
            JObject json = new JObject();
            AddInt(json, "NoncurrentDays", transition.NonCurrentDays);
            AddDate(json, "NoncurrentDate", transition.NonCurrentDate);
            AddEnum(json, "StorageClass", transition.StorageClass);
            return json;
        }

        private static JObject ToJson(NonCurrentVersionExpiration expiration)
        {
            JObject json = new JObject();
            AddInt(json, "NoncurrentDays", expiration.NonCurrentDays);
            AddDate(json, "NoncurrentDate", expiration.NonCurrentDate);
            return json;
        }

        private static JObject ToJson(LifecycleRuleFilter filter)
        {
            JObject json = new JObject();
            AddLong(json, "ObjectSizeGreaterThan", filter.ObjectSizeGreaterThan);
            AddEnum(json, "GreaterThanIncludeEqual", filter.GreaterThanIncludeEqual);
            AddLong(json, "ObjectSizeLessThan", filter.ObjectSizeLessThan);
            AddEnum(json, "LessThanIncludeEqual", filter.LessThanIncludeEqual);

            if (filter.Not != null && filter.Not.Length > 0)
            {
                JArray not = new JArray();
                foreach (BucketLifecycleNotFilter notFilter in filter.Not)
                {
                    if (notFilter != null)
                    {
                        JObject item = new JObject();
                        AddString(item, "Prefix", notFilter.Prefix);
                        AddTags(item, "Tags", notFilter.Tags);
                        not.Add(item);
                    }
                }

                if (not.Count > 0)
                {
                    json["Not"] = not;
                }
            }

            return json;
        }

        private static LifecycleRule ParseRule(JObject json)
        {
            if (json == null)
            {
                return new LifecycleRule
                {
                    Transitions = new Transition[0],
                    NoncurrentVersionTransitions = new NonCurrentVersionTransition[0],
                    Tags = new Tag[0],
                    AccessTimeTransitions = new AccessTimeTransition[0],
                    NonCurrentVersionAccessTimeTransitions = new NonCurrentVersionAccessTimeTransition[0]
                };
            }

            return new LifecycleRule
            {
                ID = json["ID"]?.Value<string>(),
                Prefix = json["Prefix"]?.Value<string>(),
                Status = ParseStatus(json["Status"]),
                Transitions = ParseTransitions(json["Transitions"] as JArray),
                Expiration = ParseExpiration(json["Expiration"] as JObject),
                NoncurrentVersionTransitions =
                    ParseNonCurrentVersionTransitions(json.GetValue("NoncurrentVersionTransitions", StringComparison.OrdinalIgnoreCase) as JArray),
                NoncurrentVersionExpiration =
                    ParseNonCurrentVersionExpiration(json.GetValue("NoncurrentVersionExpiration", StringComparison.OrdinalIgnoreCase) as JObject),
                Tags = ParseTags(json["Tags"] as JArray),
                AbortIncompleteMultipartUpload =
                    ParseAbortIncompleteMultipartUpload(json["AbortIncompleteMultipartUpload"] as JObject),
                Filter = ParseFilter(json["Filter"] as JObject),
                AccessTimeTransitions = ParseAccessTimeTransitions(json["AccessTimeTransitions"] as JArray),
                NonCurrentVersionAccessTimeTransitions = ParseNonCurrentVersionAccessTimeTransitions(
                    json.GetValue("NonCurrentVersionAccessTimeTransitions", StringComparison.OrdinalIgnoreCase) as JArray)
            };
        }

        private static Transition[] ParseTransitions(JArray json)
        {
            if (json == null)
            {
                return new Transition[0];
            }

            Transition[] result = new Transition[json.Count];
            for (int i = 0; i < json.Count; i++)
            {
                JObject item = json[i] as JObject;
                result[i] = new Transition
                {
                    Days = item?["Days"]?.Value<int?>(),
                    Date = ParseDate(item?["Date"]),
                    StorageClass = ParseStorageClass(item?["StorageClass"])
                };
            }

            return result;
        }

        private static Expiration ParseExpiration(JObject json)
        {
            if (json == null)
            {
                return null;
            }

            return new Expiration
            {
                Days = json["Days"]?.Value<int?>(),
                Date = ParseDate(json["Date"])
            };
        }

        private static NonCurrentVersionTransition[] ParseNonCurrentVersionTransitions(JArray json)
        {
            if (json == null)
            {
                return new NonCurrentVersionTransition[0];
            }

            NonCurrentVersionTransition[] result = new NonCurrentVersionTransition[json.Count];
            for (int i = 0; i < json.Count; i++)
            {
                JObject item = json[i] as JObject;
                result[i] = new NonCurrentVersionTransition
                {
                    NonCurrentDays = item?.GetValue("NoncurrentDays", StringComparison.OrdinalIgnoreCase)?.Value<int?>(),
                    NonCurrentDate = ParseDate(item?.GetValue("NoncurrentDate", StringComparison.OrdinalIgnoreCase)),
                    StorageClass = ParseStorageClass(item?["StorageClass"])
                };
            }

            return result;
        }

        private static NonCurrentVersionExpiration ParseNonCurrentVersionExpiration(JObject json)
        {
            if (json == null)
            {
                return null;
            }

            return new NonCurrentVersionExpiration
            {
                NonCurrentDays = json.GetValue("NoncurrentDays", StringComparison.OrdinalIgnoreCase)?.Value<int?>(),
                NonCurrentDate = ParseDate(json.GetValue("NoncurrentDate", StringComparison.OrdinalIgnoreCase))
            };
        }

        private static AbortIncompleteMultipartUpload ParseAbortIncompleteMultipartUpload(JObject json)
        {
            if (json == null)
            {
                return null;
            }

            return new AbortIncompleteMultipartUpload
            {
                DaysAfterInitiation = json["DaysAfterInitiation"]?.Value<int?>()
            };
        }

        private static LifecycleRuleFilter ParseFilter(JObject json)
        {
            if (json == null)
            {
                return null;
            }

            return new LifecycleRuleFilter
            {
                ObjectSizeGreaterThan = json["ObjectSizeGreaterThan"]?.Value<long?>(),
                GreaterThanIncludeEqual = ParseStatus(json["GreaterThanIncludeEqual"]),
                ObjectSizeLessThan = json["ObjectSizeLessThan"]?.Value<long?>(),
                LessThanIncludeEqual = ParseStatus(json["LessThanIncludeEqual"]),
                Not = ParseNotFilters(json["Not"] as JArray)
            };
        }

        private static BucketLifecycleNotFilter[] ParseNotFilters(JArray json)
        {
            if (json == null)
            {
                return new BucketLifecycleNotFilter[0];
            }

            BucketLifecycleNotFilter[] result = new BucketLifecycleNotFilter[json.Count];
            for (int i = 0; i < json.Count; i++)
            {
                JObject item = json[i] as JObject;
                result[i] = new BucketLifecycleNotFilter
                {
                    Prefix = item?["Prefix"]?.Value<string>(),
                    Tags = ParseTags(item?["Tags"] as JArray)
                };
            }

            return result;
        }

        private static Tag[] ParseTags(JArray json)
        {
            if (json == null)
            {
                return new Tag[0];
            }

            Tag[] result = new Tag[json.Count];
            for (int i = 0; i < json.Count; i++)
            {
                JObject item = json[i] as JObject;
                result[i] = new Tag
                {
                    Key = item?["Key"]?.Value<string>(),
                    Value = item?["Value"]?.Value<string>()
                };
            }

            return result;
        }

        private static AccessTimeTransition[] ParseAccessTimeTransitions(JArray json)
        {
            if (json == null)
            {
                return new AccessTimeTransition[0];
            }

            AccessTimeTransition[] result = new AccessTimeTransition[json.Count];
            for (int i = 0; i < json.Count; i++)
            {
                JObject item = json[i] as JObject;
                result[i] = new AccessTimeTransition
                {
                    StorageClass = ParseStorageClass(item?["StorageClass"]),
                    Days = item?["Days"]?.Value<int?>()
                };
            }

            return result;
        }

        private static NonCurrentVersionAccessTimeTransition[] ParseNonCurrentVersionAccessTimeTransitions(
            JArray json)
        {
            if (json == null)
            {
                return new NonCurrentVersionAccessTimeTransition[0];
            }

            NonCurrentVersionAccessTimeTransition[] result =
                new NonCurrentVersionAccessTimeTransition[json.Count];
            for (int i = 0; i < json.Count; i++)
            {
                JObject item = json[i] as JObject;
                result[i] = new NonCurrentVersionAccessTimeTransition
                {
                    StorageClass = ParseStorageClass(item?["StorageClass"]),
                    NonCurrentDays = item?.GetValue("NonCurrentDays", StringComparison.OrdinalIgnoreCase)?.Value<int?>()
                };
            }

            return result;
        }

        private static void ValidateRules(LifecycleRule[] rules)
        {
            if (rules == null || rules.Length == 0)
            {
                throw new TosClientException("empty rules for put lifecycle");
            }

            foreach (LifecycleRule rule in rules)
            {
                if (rule == null)
                {
                    throw new TosClientException("null rule is set for put lifecycle");
                }

                if (!rule.Status.HasValue || string.IsNullOrEmpty(Enums.TransEnum(rule.Status.Value)))
                {
                    throw new TosClientException("invalid lifecycle rule status");
                }

                ValidateTransitions(rule.Transitions);
                ValidateExpiration(rule.Expiration);
                ValidateNonCurrentTransitions(rule.NoncurrentVersionTransitions);
                ValidateNonCurrentExpiration(rule.NoncurrentVersionExpiration);
                ValidateAbort(rule.AbortIncompleteMultipartUpload);
                ValidateAccessTimeTransitions(rule.AccessTimeTransitions);
                ValidateNonCurrentAccessTimeTransitions(rule.NonCurrentVersionAccessTimeTransitions);
                ValidateFilter(rule.Filter);
                ValidateTags(rule.Tags, "lifecycle rule");
            }
        }

        private static void ValidateTransitions(Transition[] transitions)
        {
            if (transitions == null)
            {
                return;
            }

            foreach (Transition transition in transitions)
            {
                if (transition == null)
                {
                    throw new TosClientException("null lifecycle transition is set");
                }

                ValidateSchedule(transition.Days, transition.Date, "lifecycle transition");
                ValidateStorageClass(transition.StorageClass, "lifecycle transition");
            }
        }

        private static void ValidateExpiration(Expiration expiration)
        {
            if (expiration != null)
            {
                ValidateSchedule(expiration.Days, expiration.Date, "lifecycle expiration");
            }
        }

        private static void ValidateNonCurrentTransitions(NonCurrentVersionTransition[] transitions)
        {
            if (transitions == null)
            {
                return;
            }

            foreach (NonCurrentVersionTransition transition in transitions)
            {
                if (transition == null)
                {
                    throw new TosClientException("null noncurrent version transition is set");
                }

                ValidateSchedule(transition.NonCurrentDays, transition.NonCurrentDate,
                    "noncurrent version transition");
                ValidateStorageClass(transition.StorageClass, "noncurrent version transition");
            }
        }

        private static void ValidateNonCurrentExpiration(NonCurrentVersionExpiration expiration)
        {
            if (expiration != null)
            {
                ValidateSchedule(expiration.NonCurrentDays, expiration.NonCurrentDate,
                    "noncurrent version expiration");
            }
        }

        private static void ValidateAbort(AbortIncompleteMultipartUpload abort)
        {
            if (abort != null && (!abort.DaysAfterInitiation.HasValue || abort.DaysAfterInitiation.Value <= 0))
            {
                throw new TosClientException("invalid days after initiation for lifecycle rule");
            }
        }

        private static void ValidateAccessTimeTransitions(AccessTimeTransition[] transitions)
        {
            if (transitions == null)
            {
                return;
            }

            foreach (AccessTimeTransition transition in transitions)
            {
                if (transition == null)
                {
                    throw new TosClientException("null access time transition is set");
                }

                if (!transition.Days.HasValue || transition.Days.Value <= 0)
                {
                    throw new TosClientException("invalid days for access time transition");
                }

                ValidateStorageClass(transition.StorageClass, "access time transition");
            }
        }

        private static void ValidateNonCurrentAccessTimeTransitions(
            NonCurrentVersionAccessTimeTransition[] transitions)
        {
            if (transitions == null)
            {
                return;
            }

            foreach (NonCurrentVersionAccessTimeTransition transition in transitions)
            {
                if (transition == null)
                {
                    throw new TosClientException("null noncurrent access time transition is set");
                }

                if (!transition.NonCurrentDays.HasValue || transition.NonCurrentDays.Value <= 0)
                {
                    throw new TosClientException("invalid days for noncurrent access time transition");
                }

                ValidateStorageClass(transition.StorageClass, "noncurrent access time transition");
            }
        }

        private static void ValidateFilter(LifecycleRuleFilter filter)
        {
            if (filter == null)
            {
                return;
            }

            if (filter.ObjectSizeGreaterThan.HasValue && filter.ObjectSizeGreaterThan.Value < 0 ||
                filter.ObjectSizeLessThan.HasValue && filter.ObjectSizeLessThan.Value < 0)
            {
                throw new TosClientException("invalid object size for lifecycle rule filter");
            }

            ValidateOptionalStatus(filter.GreaterThanIncludeEqual);
            ValidateOptionalStatus(filter.LessThanIncludeEqual);

            if (filter.Not == null)
            {
                return;
            }

            foreach (BucketLifecycleNotFilter notFilter in filter.Not)
            {
                if (notFilter == null)
                {
                    throw new TosClientException("null not filter is set for lifecycle rule");
                }

                ValidateTags(notFilter.Tags, "lifecycle rule not filter");
                if (string.IsNullOrEmpty(notFilter.Prefix) &&
                    (notFilter.Tags == null || notFilter.Tags.Length == 0))
                {
                    throw new TosClientException("empty not filter is set for lifecycle rule");
                }
            }
        }

        private static void ValidateTags(Tag[] tags, string name)
        {
            if (tags == null)
            {
                return;
            }

            foreach (Tag tag in tags)
            {
                if (tag == null || string.IsNullOrEmpty(tag.Key) || string.IsNullOrEmpty(tag.Value))
                {
                    throw new TosClientException("invalid tag is set for " + name);
                }
            }
        }

        private static void ValidateSchedule(int? days, DateTime? date, string name)
        {
            if (days.HasValue == date.HasValue)
            {
                throw new TosClientException(name + " must set exactly one of days and date");
            }

            if (days.HasValue && days.Value <= 0)
            {
                throw new TosClientException("invalid days for " + name);
            }
        }

        private static void ValidateStorageClass(StorageClassType? storageClass, string name)
        {
            if (!storageClass.HasValue || string.IsNullOrEmpty(Enums.TransEnum(storageClass.Value)))
            {
                throw new TosClientException("invalid storage class for " + name);
            }
        }

        private static void ValidateOptionalStatus(StatusType? status)
        {
            if (status.HasValue && string.IsNullOrEmpty(Enums.TransEnum(status.Value)))
            {
                throw new TosClientException("invalid status for lifecycle rule filter");
            }
        }

        private static void AddString(JObject json, string name, string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                json[name] = value;
            }
        }

        private static void AddInt(JObject json, string name, int? value)
        {
            if (value.HasValue)
            {
                json[name] = value.Value;
            }
        }

        private static void AddLong(JObject json, string name, long? value)
        {
            if (value.HasValue)
            {
                json[name] = value.Value;
            }
        }

        private static void AddDate(JObject json, string name, DateTime? value)
        {
            if (value.HasValue)
            {
                DateTime date = value.Value.Kind == DateTimeKind.Unspecified
                    ? DateTime.SpecifyKind(value.Value, DateTimeKind.Utc)
                    : value.Value.ToUniversalTime();
                json[name] = date
                    .ToString(Constants.Iso8601DateFormat, Constants.DefaultCultureInfo);
            }
        }

        private static void AddEnum(JObject json, string name, StatusType? value)
        {
            if (value.HasValue)
            {
                string enumValue = Enums.TransEnum(value.Value);
                if (!string.IsNullOrEmpty(enumValue))
                {
                    json[name] = enumValue;
                }
            }
        }

        private static void AddEnum(JObject json, string name, StorageClassType? value)
        {
            if (value.HasValue)
            {
                string enumValue = Enums.TransEnum(value.Value);
                if (!string.IsNullOrEmpty(enumValue))
                {
                    json[name] = enumValue;
                }
            }
        }

        private static void AddTags(JObject json, string name, Tag[] tags)
        {
            if (tags == null || tags.Length == 0)
            {
                return;
            }

            JArray tagArray = new JArray();
            foreach (Tag tag in tags)
            {
                if (tag != null)
                {
                    JObject item = new JObject();
                    item["Key"] = tag.Key;
                    item["Value"] = tag.Value;
                    tagArray.Add(item);
                }
            }

            if (tagArray.Count > 0)
            {
                json[name] = tagArray;
            }
        }

        private static StatusType? ParseStatus(JToken token)
        {
            Enum value = Enums.ParseEnum<StatusType>(token?.Value<string>());
            return value == null ? (StatusType?)null : (StatusType)value;
        }

        private static StorageClassType? ParseStorageClass(JToken token)
        {
            Enum value = Enums.ParseEnum<StorageClassType>(token?.Value<string>());
            return value == null ? (StorageClassType?)null : (StorageClassType)value;
        }

        private static DateTime? ParseDate(JToken token)
        {
            DateTime? value = Utils.ParseJTokenDate(token, Constants.Iso8601DateFormat);
            if (!value.HasValue)
            {
                return null;
            }

            return value.Value.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(value.Value, DateTimeKind.Utc)
                : value.Value.ToUniversalTime();
        }
    }
}
