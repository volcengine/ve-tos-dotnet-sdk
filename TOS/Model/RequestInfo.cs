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

namespace TOS.Model
{
    public class RequestInfo
    {
        private string _requestId;
        private string _id2;
        private int _statusCode;
        private IDictionary<string, string> _header;

        public string RequestID
        {
            get { return this._requestId; }
            internal set { this._requestId = value; }
        }

        public string ID2
        {
            get { return this._id2; }
            internal set { this._id2 = value; }
        }

        public int StatusCode
        {
            get { return this._statusCode; }
            internal set { this._statusCode = value; }
        }

        public IDictionary<string, string> Header
        {
            get { return this._header; }
            internal set { this._header = value; }
        }
    }
}