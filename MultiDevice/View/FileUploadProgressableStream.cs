using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MultiDevice
{
    public class FileUploadProgressableStream : Stream
    {
        private readonly Stream _innerStream;
        private readonly IProgress<double> _progress;
        private readonly long _totalSize;
        private long _totalRead;

        public FileUploadProgressableStream(Stream innerStream, IProgress<double> progress, long totalSize)
        {
            _innerStream = innerStream;
            _progress  = progress;
            _totalSize = totalSize;
            _totalRead = 0;
        }

        public override bool CanRead  => _innerStream.CanRead;
        public override bool CanSeek  => _innerStream.CanSeek;
        public override bool CanWrite => _innerStream.CanWrite;
        public override long Length   => _innerStream.Length;
        public override long Position
        {
            get => _innerStream.Position;
            set => _innerStream.Position = value;
        }

        public override void Flush() => _innerStream.Flush();

        public override int Read(byte[] buffer, int offset, int count)
        {
            int bytesRead = _innerStream.Read(buffer, offset, count);
            _totalRead   += bytesRead;

            // 计算并报告进度
            double progressPercentage = (double)_totalRead / _totalSize * 100;
            _progress.Report(progressPercentage);

            return bytesRead;
        }

        public override long Seek(long offset, SeekOrigin origin) => _innerStream.Seek(offset, origin);
        public override void SetLength(long value) => _innerStream.SetLength(value);
        public override void Write(byte[] buffer, int offset, int count) => _innerStream.Write(buffer, offset, count);
    }
}
