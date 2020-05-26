using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Azure.Storage.Core.Util;

using SimpleObjectBrowser.ViewModels;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleObjectBrowser.Services
{
    public interface ICredential
    {
        string Name { get; }
        IStorageAccount Connect();
    }

    public class AzureBlobStorageConnectionStringCredential : ICredential
    {
        public string Name { get; set; }
        public string ConnectionString { get; set; }
        public IStorageAccount Connect()
        {
            return new AzureBlobStorageAccount(CloudStorageAccount.Parse(ConnectionString), Name);
        }
    }

    public class AzureBlobStorageAccount : IStorageAccount
    {
        private readonly CloudStorageAccount _nativeAccount;
        private readonly CloudBlobClient _blobClient;

        public AzureBlobStorageAccount(CloudStorageAccount nativeAccount, string name)
        {
            _nativeAccount = nativeAccount;
            _blobClient = _nativeAccount.CreateCloudBlobClient();
            Name = name ?? _nativeAccount.Credentials.AccountName;
        }

        public string Name { get; }

        public AccountType Type => AccountType.AzureBlobStorage;

        public async Task<IEnumerable<IStorageBucket>> ListBucketsAsync()
        {
            var segmentedResult = await _blobClient.ListContainersSegmentedAsync(
                string.Empty, ContainerListingDetails.Metadata, null, null, null, null);

            return segmentedResult.Results.Select(c => new AzureBlobStorageContainer(this, c));
        }
    }

    public class AzureBlobStorageContainer : IStorageBucket
    {
        public IStorageAccount Account { get; }

        private readonly CloudBlobContainer _nativeContainer;

        public string Name { get; }

        public AzureBlobStorageContainer(AzureBlobStorageAccount azureBlobStorageAccount, CloudBlobContainer native)
        {
            Account = azureBlobStorageAccount;
            _nativeContainer = native;
            Name = _nativeContainer.Name;
        }

        public Task<IPagedResult<IEnumerable<IEntry>>> ListEntriesAsync(ListQuery query)
        {
            return ListEntriesAsync(query, null, null);
        }

        private async Task<IPagedResult<IEnumerable<IEntry>>> ListEntriesAsync(ListQuery query, BlobContinuationToken token, IPagedResult<IEnumerable<IEntry>> previous)
        {
            var segmentedResult = await _nativeContainer.ListBlobsSegmentedAsync(
                query.Prefix, query.Heirarchical == false, BlobListingDetails.Metadata, query.PageSize, token, null, null);

            var entries = new List<IEntry>();

            foreach (var directory in segmentedResult.Results.OfType<CloudBlobDirectory>())
            {
                entries.Add(new AzureDirectory(this, directory.Prefix));
            }

            foreach (var blob in segmentedResult.Results.OfType<CloudBlockBlob>())
            {
                entries.Add(new AzureBlobStorageBlob(this, blob));
            }

            Func<IPagedResult<IEnumerable<IEntry>>, Task<IPagedResult<IEnumerable<IEntry>>>> next = null;
            if (segmentedResult.ContinuationToken != null)
                next = prev => ListEntriesAsync(query, segmentedResult.ContinuationToken, prev);

            var currentPageNumber = (previous?.PageNumber ?? 0) + 1;

            return new PagedResult<IEnumerable<IEntry>>(
                query.PageSize,
                currentPageNumber,
                entries,
                () => ListEntriesAsync(query, token, previous),
                previous,
                next);
        }

        public async Task UploadBlob(string fullName, Stream stream, string contentType, CancellationToken token, IProgress<long> progress)
        {
            var blob = _nativeContainer.GetBlockBlobReference(fullName);
            blob.Properties.ContentType = contentType;

            var progressHandler = new Progress<StorageProgress>();
            progressHandler.ProgressChanged += (s, args) => progress.Report(args.BytesTransferred);

            await blob.UploadFromStreamAsync(
                stream,
                AccessCondition.GenerateEmptyCondition(),
                new BlobRequestOptions(),
                new OperationContext(),
                progressHandler,
                token);
        }

        public async Task DeleteBlobs(IEnumerable<string> keys, CancellationToken token)
        {
            foreach (var key in keys)
            {
                token.ThrowIfCancellationRequested();

                var blob = _nativeContainer.GetBlobReference(key);
                var result = await blob.DeleteIfExistsAsync(token);
            }
        }

    }

    public class AzureDirectory : IEntry
    {
        public AzureDirectory(AzureBlobStorageContainer azureBlobStorageContainer, string name)
        {
            Bucket = azureBlobStorageContainer;
            Key = name;
        }

        public IStorageBucket Bucket { get; }
        public string Key { get; }
        public bool IsDirectory => true;
    }

    public class AzureBlobStorageBlob : IBlob
    {
        public IStorageBucket Bucket { get; }

        private CloudBlockBlob _nativeBlob;

        public string Key { get; private set; }
        public long Length { get; }
        public DateTimeOffset? LastModified { get; }
        public string ContentType { get; }
        public string Tier { get; }

        public AzureBlobStorageBlob(AzureBlobStorageContainer container, CloudBlockBlob nativeBlob)
        {
            Bucket = container;
            _nativeBlob = nativeBlob;
            Key = _nativeBlob.Name;
            Length = nativeBlob.Properties.Length;
            LastModified = nativeBlob.Properties.LastModified;
            ContentType = nativeBlob.Properties.ContentType;

            if (nativeBlob.Properties.StandardBlobTier != null)
                Tier = Enum.GetName(typeof(StandardBlobTier), nativeBlob.Properties.StandardBlobTier);
        }

        public bool IsDirectory => false;

        public bool ContentTypeIsInferred => false;

        public async Task DownloadToStreamAsync(Stream target, IProgress<long> progress, CancellationToken token)
        {
            var storageProgress = new Progress<StorageProgress>();
            storageProgress.ProgressChanged += (s, args) =>
            {
                progress.Report(args.BytesTransferred);
            };

            await _nativeBlob.DownloadToStreamAsync(
                target,
                AccessCondition.GenerateEmptyCondition(),
                new BlobRequestOptions(),
                new OperationContext(),
                storageProgress,
                token);
        }

        public Uri GetLink(TimeSpan lifeTime)
        {
            var sas = _nativeBlob.GetSharedAccessSignature(new SharedAccessBlobPolicy
            {
                Permissions = SharedAccessBlobPermissions.Read,
                SharedAccessExpiryTime = DateTimeOffset.UtcNow.Add(lifeTime),
                SharedAccessStartTime = DateTimeOffset.UtcNow,
            });

            return new Uri(_nativeBlob.Uri.AbsoluteUri + sas);
        }
    }
}
