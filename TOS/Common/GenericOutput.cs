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

namespace TOS.Common
{
    public abstract class GenericOutput
    {
        
        protected RequestInfo _requestInfo;

        internal void Parse(HttpRequest request, HttpResponse response, RequestInfo requestInfo)
        {
            _requestInfo = requestInfo;
            this.Parse(request, response);
        }

        internal virtual void Parse(HttpRequest request, HttpResponse response)
        {
            // impl by subclass
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
    }
}