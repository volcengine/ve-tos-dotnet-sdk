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
using System.Net;
using System.Threading;
using TOS.Common;

namespace TOS.Config
{
    internal class ConfigHolder
    {
        private string _userAgent;
        private volatile Credential _credential;
        private int _requestTimeout = Timeout.Infinite;
        private int _connectionTimeout = Constants.DefaultConnectionTimeout;
        private int _socketTimeout = Constants.DefaultSocketTimeout;
        private int _maxConnections = Constants.DefaultMaxConnections;
        private int _idleConnectionTime = Constants.DefaultIdleConnectionTime;
        private bool _enableVerifySsl = true;

        internal ConfigHolder()
        {
            this._userAgent = "ve-tos-.net-sdk/" + Constants.Version;
            try
            {
                OperatingSystem os = Environment.OSVersion;
                this._userAgent += " (" + os.VersionString + ")";
            }
            catch (Exception ex)
            {
            }
        }

        internal RegionEndpoint RegionEndpoint { get; set; }

        internal string UserAgent
        {
            get { return this._userAgent; }
        }

        internal Credential Credential
        {
            get { return this._credential; }
            set { this._credential = value; }
        }

        internal int RequestTimeout
        {
            get { return this._requestTimeout; }
            set
            {
                if (value > 0)
                {
                    this._requestTimeout = value;
                }
            }
        }

        internal int ConnectionTimeout
        {
            get { return this._connectionTimeout; }
            set
            {
                if (value > 0)
                {
                    this._connectionTimeout = value;
                }
            }
        }

        internal int SocketTimeout
        {
            get { return this._socketTimeout; }
            set
            {
                if (value > 0)
                {
                    this._socketTimeout = value;
                }
            }
        }

        internal int MaxConnections
        {
            get { return this._maxConnections; }
            set
            {
                if (value > 0)
                {
                    this._maxConnections = value;
                }
            }
        }

        internal int IdleConnectionTime
        {
            get { return this._idleConnectionTime; }
            set
            {
                if (value > 0)
                {
                    this._idleConnectionTime = value;
                }
            }
        }

        internal bool EnableVerifySSL
        {
            get { return this._enableVerifySsl; }
            set { this._enableVerifySsl = value; }
        }

        internal SecurityProtocolType SecurityProtocolType { get; set; }
    }
}