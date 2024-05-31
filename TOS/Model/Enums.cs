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
using System.Collections.Generic;
using System.Reflection;

namespace TOS.Model
{
    public enum HttpMethodType
    {
        [StringValue("GET")] HttpMethodGet,

        [StringValue("PUT")] HttpMethodPut,

        [StringValue("POST")] HttpMethodPost,

        [StringValue("DELETE")] HttpMethodDelete,

        [StringValue("HEAD")] HttpMethodHead
    }

    public enum ACLType
    {
        [StringValue("private")] ACLPrivate,

        [StringValue("public-read")] ACLPublicRead,

        [StringValue("public-read-write")] ACLPublicReadWrite,

        [StringValue("authenticated-read")] ACLAuthenticatedRead,

        [StringValue("bucket-owner-read")] ACLBucketOwnerRead,

        [StringValue("bucket-owner-full-control")]
        ACLBucketOwnerFullControl,
    }

    public enum StorageClassType
    {
        [StringValue("STANDARD")] StorageClassStandard,

        [StringValue("IA")] StorageClassIa,
        
        [StringValue("INTELLIGENT_TIERING")] StorageClassIntelligentTiering,

        [StringValue("ARCHIVE_FR")] StorageClassArchiveFr,
        
        [StringValue("ARCHIVE")] StorageClassArchive,

        [StringValue("COLD_ARCHIVE")] StorageClassColdArchive,

        [StringValue("DEEP_COLD_ARCHIVE")] StorageClassDeepColdArchive,
    }

    public enum AzRedundancyType
    {
        [StringValue("single-az")] AzRedundancySingleAz,

        [StringValue("multi-az")] AzRedundancyMultiAz
    }

    public enum MetadataDirectiveType
    {
        [StringValue("COPY")] MetadataDirectiveCopy,
        [StringValue("REPLACE")] MetadataDirectiveReplace
    }

    public enum PermissionType
    {
        [StringValue("READ")] PermissionRead,
        [StringValue("WRITE")] PermissionWrite,
        [StringValue("READ_ACP")] PermissionReadAcp,
        [StringValue("WRITE_ACP")] PermissionWriteAcp,
        [StringValue("FULL_CONTROL")] PermissionFullControl,
    }

    public enum GranteeType
    {
        [StringValue("Group")] GranteeGroup,
        [StringValue("CanonicalUser")] GranteeUser
    }

    public enum CannedType
    {
        [StringValue("AllUsers")] CannedAllUsers,
        [StringValue("AuthenticatedUsers")] CannedAuthenticatedUsers
    }
    
    public enum TierType
    {
        [StringValue("Standard")] Standard,
        [StringValue("Expedited")] Expedited,
        [StringValue("Bulk")] Bulk
    }
    
    public enum TaggingDirectiveType
    {
        [StringValue("Copy")] TaggingDirectiveCopy,
        [StringValue("Replace")] TaggingDirectiveReplace,
    }

    internal class StringValueAttribute : Attribute
    {
        private string _value;

        public StringValueAttribute(string value)
        {
            this._value = value;
        }

        public string StringValue
        {
            get { return this._value; }
        }
    }


    internal static class Enums
    {
        private static IDictionary<Type, IDictionary<string, Enum>> _valueEnumDict;

        private static IDictionary<Type, IDictionary<Enum, string>> _enumValueDict;

        static Enums()
        {
            _valueEnumDict = new Dictionary<Type, IDictionary<string, Enum>>();
            _enumValueDict = new Dictionary<Type, IDictionary<Enum, string>>();

            Type[] types =
            {
                typeof(HttpMethodType), typeof(ACLType), typeof(StorageClassType),
                typeof(AzRedundancyType), typeof(MetadataDirectiveType), typeof(PermissionType),
                typeof(GranteeType), typeof(CannedType), typeof(TaggingDirectiveType)
            };

            string ret;
            FieldInfo field;
            object[] attributes;
            StringValueAttribute attrib;
            IDictionary<string, Enum> valueEnum;
            IDictionary<Enum, string> enumValue;
            foreach (Type t in types)
            {
                valueEnum = new Dictionary<string, Enum>();
                enumValue = new Dictionary<Enum, string>();
                foreach (Enum value in Enum.GetValues(t))
                {
                    field = t.GetField(value.ToString());
                    attributes = field.GetCustomAttributes(false);
                    attrib = attributes.Length > 0 ? attributes[0] as StringValueAttribute : null;
                    ret = attrib != null ? attrib.StringValue : value.ToString();
                    valueEnum.Add(ret, value);
                    enumValue.Add(value, ret);
                }

                _valueEnumDict[t] = valueEnum;
                _enumValueDict[t] = enumValue;
            }
        }

        internal static Enum ParseEnum<U>(string input) where U : Enum
        {
            if (string.IsNullOrEmpty(input))
            {
                return null;
            }

            IDictionary<string, Enum> valueEnum;
            _valueEnumDict.TryGetValue(typeof(U), out valueEnum);
            if (valueEnum == null)
            {
                return null;
            }

            Enum ret;
            valueEnum.TryGetValue(input, out ret);
            return ret;
        }

        internal static string TransEnum(Enum value)
        {
            if (value == null)
            {
                return string.Empty;
            }

            IDictionary<Enum, string> enumValue;
            _enumValueDict.TryGetValue(value.GetType(), out enumValue);
            if (enumValue == null)
            {
                return string.Empty;
            }

            string ret;
            enumValue.TryGetValue(value, out ret);
            return ret;
        }
    }
}