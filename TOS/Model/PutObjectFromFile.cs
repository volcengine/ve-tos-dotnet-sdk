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
using System.IO;
using TOS.Common;

namespace TOS.Model
{
    public class PutObjectFromFileInput : PutObjectBasicInput
    {
        public string FilePath { get; set; }

        internal sealed override HttpRequest Trans()
        {
            Utils.CheckFilePath(FilePath);
            HttpRequest request = base.Trans();
            if (ContentLength < 0)
            {
                request.Header[Constants.HeaderContentLength] = Convert.ToString(new FileInfo(FilePath).Length);
            }

            request.Body = new FileStream(FilePath, FileMode.Open, FileAccess.Read);
            request.AutoClose = true;

            return request;
        }
    }

    public class PutObjectFromFileOutput : PutObjectOutput
    {
    }
}