using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleObjectBrowser.Services
{
    public class ListQuery
    {
        public bool Heirarchical { get; set; }
        public string Prefix { get; set; }
        public int PageSize { get; set; }
    }

    public interface IStorageBucket
    {
        string Name { get; }

        Task<IPagedResult<IEnumerable<IEntry>>> ListEntriesAsync(ListQuery query);
        Task UploadBlob(string fullName, Stream stream, string contentType, CancellationToken token, IProgress<long> progress);
        Task DeleteBlobs(IEnumerable<string> keys, CancellationToken token);
    }
}