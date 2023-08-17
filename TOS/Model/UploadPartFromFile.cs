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
using TOS.Error;

namespace TOS.Model
{
    public class UploadPartFromFileInput : UploadPartBasicInput
    {
        private long _partSize = -1;
        public string FilePath { get; set; }

        public long Offset { get; set; }

        public long PartSize
        {
            get { return _partSize; }

            set
            {
                if (value >= 0)
                {
                    _partSize = value;
                }
            }
        }

        internal sealed override HttpRequest Trans()
        {
            Utils.CheckFilePath(FilePath);
            FileInfo fileInfo = new FileInfo(FilePath);
            if (Offset < 0 || Offset > fileInfo.Length)
            {
                throw new TosClientException("invalid offset for upload part");
            }

            FileStream stream = new FileStream(FilePath, FileMode.Open, FileAccess.Read);
            if (Offset > 0)
            {
                stream.Seek(Offset, SeekOrigin.Begin);
            }

            HttpRequest request = base.Trans();
            if (PartSize >= 0)
            {
                request.Header[Constants.HeaderContentLength] = Convert.ToString(PartSize);
            }
            else
            {
                request.Header[Constants.HeaderContentLength] = Convert.ToString(fileInfo.Length - Offset);
            }

            request.AutoClose = true;
            request.Body = stream;

            return request;
        }
    }

    public class UploadPartFromFileOutput : UploadPartOutput
    {
    }
}