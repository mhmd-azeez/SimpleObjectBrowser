using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleObjectBrowser.Services
{
    public static class StorageBucketExtensions
    {
        public static async Task<IEnumerable<IEntry>> ListAllEntries(this IStorageBucket bucket, string prefix, bool heirarchical)
        {
            var query = new ListQuery
            {
                Heirarchical = heirarchical,
                Prefix = prefix,
                PageSize = 2500
            };

            var first = await bucket.ListEntriesAsync(query);
            var all = new List<IEntry>();
            all.AddRange(first.Result);

            while (first.HasNextPage())
            {
                first = await first.GetNextPage();
                all.AddRange(first.Result);
            }

            return all;
        }
    }

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