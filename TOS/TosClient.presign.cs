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
using TOS.Error;
using TOS.Model;

namespace TOS
{
    internal partial class TosClient
    {
        public PreSignedURLOutput PreSignedURL(PreSignedURLInput input)
        {
            if (input == null)
            {
                throw new TosClientException("invalid input");
            }

            IDictionary<string, string> signedHeader;
            string signedUrl = this._httpClient.GenPreSignedURL(input.Trans(), input.Expires, out signedHeader);

            return new PreSignedURLOutput
            {
                SignedUrl = signedUrl,
                SignedHeader = signedHeader
            };
        }
    }
}