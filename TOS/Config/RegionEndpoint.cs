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
using TOS.Common;
using TOS.Error;

namespace TOS.Config
{
    public class RegionEndpoint
    {
        private static readonly IDictionary<string, string> _endpoints = new Dictionary<string, string>();

        private string _domain;
        private string _schema;
        private string _region = Constants.DefaultRegion;

        static RegionEndpoint()
        {
            _endpoints[Constants.RegionBeijing] = "https://tos-cn-beijing.volces.com";
            _endpoints[Constants.RegionGuangzhou] = "https://tos-cn-guangzhou.volces.com";
            _endpoints[Constants.RegionShanghai] = "https://tos-cn-shanghai.volces.com";
        }

        internal RegionEndpoint(string region, string endpoint)
        {
            string temp;
            if (!string.IsNullOrEmpty(region) && !string.IsNullOrEmpty(temp = region.Trim()))
            {
                this._region = temp;
            }

            if (!string.IsNullOrEmpty(endpoint) && !string.IsNullOrEmpty(temp = endpoint.Trim()))
            {
                endpoint = temp;
            }

            if (string.IsNullOrEmpty(endpoint))
            {
                try
                {
                    endpoint = _endpoints[this._region];
                }
                catch (Exception ex)
                {
                    throw new TosClientException("no endpoint specified", ex);
                }
            }

            SchemaDomain schemaDomain = SplitEndpoint(endpoint);
            this._schema = schemaDomain.Schema;
            this._domain = schemaDomain.Domain;
        }

        internal string Region
        {
            get { return this._region; }
        }

        internal string GetHost(string bucket)
        {
            return this.GetHost(bucket, string.Empty);
        }

        internal string GetHost(string bucket, string domain)
        {
            if (string.IsNullOrEmpty(domain))
            {
                domain = this._domain;
            }

            if (string.IsNullOrEmpty(bucket))
            {
                return domain;
            }

            return bucket + "." + domain;
        }

        internal string GetEndpoint(string bucket, string key)
        {
            return this.GetEndpoint(bucket, key, string.Empty, string.Empty, false);
        }

        internal string GetEndpoint(string bucket, string key, string schema, string domain, bool mustAddKey)
        {
            if (string.IsNullOrEmpty(schema))
            {
                schema = this._schema;
            }

            if (string.IsNullOrEmpty(domain))
            {
                domain = this._domain;
            }

            string endpoint = schema;
            if (!string.IsNullOrEmpty(bucket) && !string.IsNullOrEmpty(bucket = bucket.Trim()))
            {
                endpoint += bucket + "." + domain;
                if (!string.IsNullOrEmpty(key))
                {
                    endpoint += "/" + Utils.UrlEncode(key, "/");
                }
            }
            else
            {
                endpoint += domain;
                if (mustAddKey && !string.IsNullOrEmpty(key))
                {
                    endpoint += "/" + Utils.UrlEncode(key, "/");
                }
            }

            return endpoint;
        }

        internal static SchemaDomain SplitEndpoint(string endpoint)
        {
            endpoint = endpoint.ToLower();
            while (endpoint.Length > 0 && endpoint[endpoint.Length - 1] == '/')
            {
                endpoint = endpoint.Substring(0, endpoint.Length - 1);
            }

            if (endpoint.Length == 0)
            {
                throw new TosClientException("invalid endpoint");
            }

            SchemaDomain schemaDomain = new SchemaDomain();
            if (endpoint.StartsWith(Constants.SchemaHttp))
            {
                schemaDomain.Schema = Constants.SchemaHttp;
            }
            else if (endpoint.StartsWith(Constants.SchemaHttps))
            {
                schemaDomain.Schema = Constants.SchemaHttps;
            }
            else
            {
                schemaDomain.Schema = Constants.SchemaHttps;
                endpoint = schemaDomain.Schema + endpoint;
            }

            UriBuilder ub = new UriBuilder(endpoint);
            if (Utils.IsIpAddress(ub.Host))
            {
                throw new TosClientException("ip address is not supported");
            }


            if ((schemaDomain.Schema == Constants.SchemaHttp && ub.Port == 80) ||
                (schemaDomain.Schema == Constants.SchemaHttps && ub.Port == 443))
            {
                schemaDomain.Domain = ub.Host;
            }
            else
            {
                schemaDomain.Domain = ub.Host + ":" + ub.Port;
            }

            return schemaDomain;
        }
    }

    internal class SchemaDomain
    {
        internal string Schema { get; set; }
        internal string Domain { get; set; }
    }
}