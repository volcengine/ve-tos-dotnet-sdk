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

using System.IO;
using TOS.Common;
using TOS.Error;

namespace TOS.Model
{
    public class GetObjectToFileInput : GetObjectInput
    {
        public string FilePath { get; set; }

        internal sealed override HttpRequest Trans()
        {
            if (string.IsNullOrEmpty(FilePath))
            {
                throw new TosClientException("empty file path");
            }

            string filePath = FilePath;
            bool mkdir = false;
            if (Directory.Exists(filePath))
            {
                if (Key.EndsWith("/"))
                {
                    mkdir = true;
                }

                filePath += Path.DirectorySeparatorChar + Key;
            }
            else if (!File.Exists(filePath) &&
                     (filePath.EndsWith("/") || filePath.EndsWith(Path.DirectorySeparatorChar.ToString())))
            {
                if (Key.EndsWith("/"))
                {
                    mkdir = true;
                }

                filePath += Key;
            }

            if (mkdir)
            {
                if (File.Exists(filePath))
                {
                    throw new TosClientException("cannot create dir using file path: " + filePath);
                }
            }
            else if (Directory.Exists(filePath))
            {
                throw new TosClientException("cannot create file using file path: " + filePath);
            }

            HttpRequest request = base.Trans();
            request.AdditionalState = new GetObjectToFileState()
            {
                Mkdir = mkdir,
                FilePath = filePath
            };

            return request;
        }
    }

    public class GetObjectToFileOutput : GetObjectBasicOutput
    {
        internal sealed override void Parse(HttpRequest request, HttpResponse response)
        {
            base.Parse(request, response);
            GetObjectToFileState state = request.AdditionalState as GetObjectToFileState;
            if (state.Mkdir)
            {
                Utils.CreateDirectoryRecursively(state.FilePath);
            }
            else
            {
                Utils.CreateDirectoryRecursively(Path.GetDirectoryName(state.FilePath));
                using (Stream dst = new FileStream(state.FilePath, FileMode.Create, FileAccess.Write))
                {
                    Utils.WriteTo(response.Body, dst, Constants.DefaultBufferSize);
                }
            }
        }
    }

    internal class GetObjectToFileState
    {
        internal bool Mkdir { get; set; }
        internal string FilePath { get; set; }
    }
}