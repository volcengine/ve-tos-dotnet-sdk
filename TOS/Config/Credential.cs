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

namespace TOS.Config
{
    public class Credential
    {
        private string _ak;
        private string _sk;
        private string _securityToken;

        internal Credential(string ak, string sk, string securityToken)
        {
            string temp;
            if (!string.IsNullOrEmpty(ak) && !string.IsNullOrEmpty(temp = ak.Trim()))
            {
                this._ak = temp;
            }

            if (!string.IsNullOrEmpty(sk) && !string.IsNullOrEmpty(temp = sk.Trim()))
            {
                this._sk = temp;
            }

            if (!string.IsNullOrEmpty(securityToken) && !string.IsNullOrEmpty(temp = securityToken.Trim()))
            {
                this._securityToken = temp;
            }
        }

        internal string Ak
        {
            get { return this._ak; }
        }

        internal string Sk
        {
            get { return this._sk; }
        }

        internal string SecurityToken
        {
            get { return this._securityToken; }
        }

        internal bool IsAnonymous
        {
            get { return string.IsNullOrEmpty(this._ak) || string.IsNullOrEmpty(this._sk); }
        }
    }
}