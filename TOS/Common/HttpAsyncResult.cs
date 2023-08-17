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
using System.Net;
using System.Threading;

namespace TOS.Common
{
    internal class HttpAsyncResult : IAsyncResult, IDisposable
    {
        private volatile bool _disposed;
        private bool _isCompleted;
        private HttpContext _context;
        private Exception _ex;
        private AsyncCallback _callback;

        private object _state;

        // full request done 
        private ManualResetEvent _requestFinished;

        // only connect done
        private ManualResetEvent _connectFinished;

        internal HttpAsyncResult(AsyncCallback callback, object state, HttpContext context)
        {
            _callback = callback;
            _state = state;
            _context = context;
            _requestFinished = new ManualResetEvent(false);
            _connectFinished = new ManualResetEvent(false);
        }

        internal void GetRequestStreamCallback(IAsyncResult ar)
        {
            Stream src = this._context.HttpRequest.Body;
            if (src == null)
            {
                src = new MemoryStream();
            }

            HttpWebRequest req = this._context.HttpWebRequest;

            try
            {
                using (Stream dst = new SocketTimeoutStream(req.EndGetRequestStream(ar), this._context.SocketTimeout))
                {
                    _connectFinished.Set();
                    if (req.SendChunked)
                    {
                        Utils.WriteTo(src, dst, Constants.DefaultBufferSize);
                    }
                    else
                    {
                        Utils.WriteTo(src, dst, req.ContentLength,
                            Constants.DefaultBufferSize);
                    }
                }

                req.BeginGetResponse(this.GetResponseCallback, null);
            }
            catch (Exception ex)
            {
                this.Abort(ex);
            }
        }

        internal void GetResponseCallback(IAsyncResult ar)
        {
            _connectFinished.Set();
            try
            {
                HttpWebResponse resp = this._context.HttpWebRequest.EndGetResponse(ar) as HttpWebResponse;
                this.Set(new HttpResponse(resp, this._context.SocketTimeout));
            }
            catch (WebException ex)
            {
                HttpWebResponse resp = ex.Response as HttpWebResponse;
                if (resp == null)
                {
                    this.Abort(ex);
                }
                else
                {
                    this.Set(new HttpResponse(resp, this._context.SocketTimeout));
                }
            }
            catch (Exception ex)
            {
                this.Abort(ex);
            }
        }

        internal void Abort(Exception ex)
        {
            if (this._context.HttpWebRequest != null)
            {
                try
                {
                    this._context.HttpWebRequest.Abort();
                }
                catch (Exception exx)
                {
                    // ignore exception
                }
            }

            this.Set(ex);
        }

        internal HttpResponse Get(int millisecondsTimeout)
        {
            if (millisecondsTimeout <= 0)
            {
                this._requestFinished.WaitOne();
            }
            else if (!this._requestFinished.WaitOne(millisecondsTimeout))
            {
                throw new TimeoutException("request timeout");
            }

            if (this._ex != null)
            {
                throw this._ex;
            }

            return this._context.HttpResponse;
        }

        internal void CheckConnect(int connectionTimeout)
        {
            if (!this._connectFinished.WaitOne(connectionTimeout))
            {
                this.Abort(new TimeoutException("connection timeout"));
            }
        }

        private void Set(HttpResponse response)
        {
            if (this._disposed)
            {
                return;
            }

            this._context.HttpResponse = response;
            this.Notify();
        }

        private void Set(Exception ex)
        {
            if (this._disposed)
            {
                return;
            }

            Interlocked.CompareExchange(ref this._ex, ex, null);
            this.Notify();
        }

        private void Notify()
        {
            this._isCompleted = true;
            this._requestFinished.Set();
            this._callback?.Invoke(this);
        }


        public void Dispose()
        {
            try
            {
                if (_disposed)
                {
                    return;
                }

                this._requestFinished.Set();
                this._connectFinished.Set();
                this._requestFinished.Close();
                this._connectFinished.Close();
                _disposed = true;
            }
            finally
            {
                GC.SuppressFinalize(this);
            }
        }

        public object AsyncState
        {
            get { return this._state; }
        }

        public WaitHandle AsyncWaitHandle
        {
            get { return this._requestFinished; }
        }

        public bool CompletedSynchronously { get; }

        public bool IsCompleted
        {
            get { return _isCompleted; }
        }
    }
}