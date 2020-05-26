using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleObjectBrowser.Services
{
    public static class EntryExtensions
    {
        public static async Task<IEnumerable<IBlob>> ListAllBlobsAsync(this IEntry directory)
        {
            var response = await directory.Bucket.ListAllEntries(directory.Key, false);
            return response.OfType<IBlob>();
        }
    }

    public interface IEntry
    {
        IStorageBucket Bucket { get; }
        string Key { get; }
        bool IsDirectory { get; }
    }

    public interface IBlob : IEntry
    {
        bool ContentTypeIsInferred { get; }
        string ContentType { get; }
        DateTimeOffset? LastModified { get; }
        long Length { get; }
        string Tier { get; }

        Task DownloadToStreamAsync(Stream target, IProgress<long> progress, CancellationToken token);
        Uri GetLink(TimeSpan lifeTime);
    }
}