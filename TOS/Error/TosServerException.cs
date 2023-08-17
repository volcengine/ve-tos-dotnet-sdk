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

using System.Collections.Generic;
using TOS.Model;

namespace TOS.Error
{
    public class TosServerException : TosException
    {
        private RequestInfo _requestInfo;
        private string _code;
        private string _hostId;
        private string _resource;

        internal TosServerException(string message, RequestInfo requestInfo) : base(message)
        {
            _requestInfo = requestInfo;
        }

        public string RequestID
        {
            get { return this._requestInfo.RequestID; }
        }

        public string ID2
        {
            get { return this._requestInfo.ID2; }
        }

        public int StatusCode
        {
            get { return this._requestInfo.StatusCode; }
        }

        public IDictionary<string, string> Header
        {
            get { return this._requestInfo.Header; }
        }

        public string Code
        {
            get { return this._code; }
            internal set { this._code = value; }
        }

        public string HostID
        {
            get { return this._hostId; }
            internal set { this._hostId = value; }
        }

        public string Resource
        {
            get { return this._resource; }
            internal set { this._resource = value; }
        }

        public override string ToString()
        {
            return "RequestID: " + this.RequestID + ", StatusCode: " + this.StatusCode +
                   ", Code: " + this.Code + ", Message: " + this.Message;
        }
    }
}