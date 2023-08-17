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

using System.Net;
using TOS.Config;

namespace TOS
{
    public class TosClientBuilder
    {
        private ConfigHolder _configHolder;
        private string _ak;
        private string _sk;
        private string _securityToken;
        private string _region;
        private string _endpoint;

        public static TosClientBuilder Builder()
        {
            return new TosClientBuilder();
        }

        private TosClientBuilder()
        {
            _configHolder = new ConfigHolder();
        }

        public TosClientBuilder SetAk(string ak)
        {
            this._ak = ak;
            return this;
        }

        public TosClientBuilder SetSk(string sk)
        {
            this._sk = sk;
            return this;
        }

        public TosClientBuilder SetSecurityToken(string securityToken)
        {
            this._securityToken = securityToken;
            return this;
        }

        public TosClientBuilder SetRegion(string region)
        {
            this._region = region;
            return this;
        }

        public TosClientBuilder SetEndpoint(string endpoint)
        {
            this._endpoint = endpoint;
            return this;
        }

        public TosClientBuilder SetRequestTimeout(int requestTimeout)
        {
            _configHolder.RequestTimeout = requestTimeout;
            return this;
        }

        public TosClientBuilder SetConnectionTimeout(int connectionTimeout)
        {
            _configHolder.ConnectionTimeout = connectionTimeout;
            return this;
        }

        public TosClientBuilder SetSocketTimeout(int socketTimeout)
        {
            _configHolder.SocketTimeout = socketTimeout;
            return this;
        }

        public TosClientBuilder SetMaxConnections(int maxConnections)
        {
            _configHolder.MaxConnections = maxConnections;
            return this;
        }

        public TosClientBuilder SetIdleConnectionTime(int idleConnectionTime)
        {
            _configHolder.IdleConnectionTime = idleConnectionTime;
            return this;
        }

        public TosClientBuilder SetEnableVerifySSL(bool enableVerifySSL)
        {
            _configHolder.EnableVerifySSL = enableVerifySSL;
            return this;
        }

        public TosClientBuilder SetSecurityProtocolType(SecurityProtocolType securityProtocolType)
        {
            _configHolder.SecurityProtocolType = securityProtocolType;
            return this;
        }

        public ITosClient Build()
        {
            this._configHolder.Credential = new Credential(this._ak, this._sk, this._securityToken);
            this._configHolder.RegionEndpoint = new RegionEndpoint(this._region, this._endpoint);
            return new TosClient(this._configHolder);
        }
    }
}