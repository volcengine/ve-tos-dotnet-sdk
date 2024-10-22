using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using TOS.Error;

namespace TOS.Common
{
    internal class SocketTimeoutStream : Stream
    {
        private Stream _origin;
        private int _socketTimeout;
        private SocketTimeoutAsyncResult result;
        private long _length = -1;
        private long _remaining = -1;

        internal SocketTimeoutStream(Stream origin, int socketTimeout)
        {
            _origin = origin ?? throw new TosClientException("invalid origin stream");
            if (socketTimeout <= 0)
            {
                throw new TosClientException("invalid socket timeout");
            }

            _socketTimeout = socketTimeout;
            result = new SocketTimeoutAsyncResult(_origin, _socketTimeout);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (_length >= 0)
            {
                if (_remaining == 0)
                {
                    return 0;
                }

                if (_remaining <= count)
                {
                    count = (int)_remaining;
                }
                
                result.BeginReadWriteOnce(buffer, offset, count, false);
                int ret = result.EndReadOnce();
                _remaining -= ret;
                return ret;
            }
            
            result.BeginReadWriteOnce(buffer, offset, count, false);
            return result.EndReadOnce();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            result.BeginReadWriteOnce(buffer, offset, count, true);
            result.EndWriteOnce();
        }


        public override void Flush()
        {
            this._origin.Flush();
        }

        public override void Close()
        {
            this._origin.Close();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return this._origin.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            if (value >= 0)
            {
                this._length = value;
                this._remaining = value;
            }
        }

        public override bool CanRead
        {
            get { return this._origin.CanRead; }
        }

        public override bool CanSeek
        {
            get { return this._origin.CanSeek; }
        }

        public override bool CanWrite
        {
            get { return this._origin.CanWrite; }
        }

        public override long Length
        {
            get
            {
                if (this._length >= 0)
                {
                    return this._length;
                }
                return this._origin.Length;
            }
        }

        public override long Position
        {
            get { return this._origin.Position; }
            set { this._origin.Position = value; }
        }
    }

    internal class SocketTimeoutAsyncResult : IAsyncResult
    {
        private static readonly object _writeFlag = new object();
        private Stream _origin;
        private int _socketTimeout;
        private ManualResetEvent _readWriteOnceFinished;
        private Exception _ex;
        private int _readOnce;

        internal SocketTimeoutAsyncResult(Stream origin, int socketTimeout)
        {
            _origin = origin;
            _socketTimeout = socketTimeout;
            _readWriteOnceFinished = new ManualResetEvent(false);
        }

        internal void BeginReadWriteOnce(byte[] buffer, int offset, int count, bool isWrite)
        {
            if (isWrite)
            {
                this._origin.BeginWrite(buffer, offset, count, this.ReadWriteOnceCallback, _writeFlag);
                return;
            }

            this._origin.BeginRead(buffer, offset, count, this.ReadWriteOnceCallback, null);
        }

        internal void ReadWriteOnceCallback(IAsyncResult ar)
        {
            try
            {
                if (ar.AsyncState != null)
                {
                    _origin.EndWrite(ar);
                }
                else
                {
                    _readOnce = _origin.EndRead(ar);
                }
            }
            catch (Exception ex)
            {
                this._ex = ex;
            }
            finally
            {
                _readWriteOnceFinished.Set();
            }
        }

        internal int EndReadOnce()
        {
            this.CheckException(false);
            int readOnce = _readOnce;
            _readOnce = 0;
            this._readWriteOnceFinished.Reset();
            return readOnce;
        }

        internal void EndWriteOnce()
        {
            this.CheckException(true);
            this._readWriteOnceFinished.Reset();
        }

        private void CheckException(bool isWrite)
        {
            if (!this._readWriteOnceFinished.WaitOne(_socketTimeout))
            {
                if (isWrite)
                {
                    throw new TimeoutException("socket timeout while writing");
                }

                throw new TimeoutException("socket timeout while reading");
            }

            if (this._ex != null)
            {
                throw this._ex;
            }
        }


        public object AsyncState { get; }
        public WaitHandle AsyncWaitHandle { get; }
        public bool CompletedSynchronously { get; }
        public bool IsCompleted { get; }
    }
}