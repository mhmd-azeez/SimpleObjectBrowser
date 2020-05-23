using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleObjectBrowser.Services
{
    public interface IStorageBucket
    {
        string Name { get; }

        Task<IEnumerable<IEntry>> ListEntriesAsync(string prefix, bool heirarchical);
        Task UploadBlob(string fullName, Stream stream, string contentType, CancellationToken token, IProgress<long> progress);
        Task DeleteBlobs(IEnumerable<string> keys, CancellationToken token);
    }
}