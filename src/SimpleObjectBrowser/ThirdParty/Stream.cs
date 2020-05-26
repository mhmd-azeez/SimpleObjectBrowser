using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace System
{
    public static class StreamExtensions
    {
        // from: https://github.com/microsoft/referencesource/blob/a7bd3242bd7732dec4aebb21fbc0f6de61c2545e/mscorlib/system/io/stream.cs#L162
        public static async Task CopyToAsync(this Stream source, Stream destination, IProgress<long> progress, CancellationToken cancellationToken)
        {
            const int bufferSize = 81920;

            byte[] buffer = new byte[bufferSize];
            int bytesRead;

            long bytesTranferred = 0;

            while ((bytesRead = await source.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false)) != 0)
            {
                await destination.WriteAsync(buffer, 0, bytesRead, cancellationToken).ConfigureAwait(false);

                bytesTranferred += bytesRead;

                progress.Report(bytesTranferred);
            }
        }
    }
}
