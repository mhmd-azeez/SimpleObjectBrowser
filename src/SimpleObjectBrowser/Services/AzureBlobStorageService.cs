using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using SimpleObjectBrowser.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
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

        public async Task<IEnumerable<IEntry>> ListEntriesAsync(string prefix, bool heirarchical)
        {
            var segmentedResult = await _nativeContainer.ListBlobsSegmentedAsync(
                prefix, heirarchical == false, BlobListingDetails.Metadata, null, null, null, null);

            var entries = new List<IEntry>();

            foreach (var entry in segmentedResult.Results)
            {
                if (entry is CloudBlockBlob blob)
                {
                    entries.Add(new AzureBlobStorageBlob(this, blob));
                }
                else if (entry is CloudBlobDirectory directory)
                {
                    entries.Add(new AzureDirectory(this, directory.Prefix));
                }
            }

            return segmentedResult.Results.OfType<CloudBlockBlob>().Select(b => new AzureBlobStorageBlob(this, b));
        }
    }

    public class AzureDirectory : IEntry
    {
        public AzureDirectory(AzureBlobStorageContainer azureBlobStorageContainer, string prefix)
        {
            Bucket = azureBlobStorageContainer;
            Name = prefix;
        }

        public IStorageBucket Bucket { get; }
        public string Name { get; }
        public bool IsDirectory => true;
    }

    public class AzureBlobStorageBlob : IBlob
    {
        public IStorageBucket Bucket { get; }

        private IListBlobItem _nativeBlob;

        public string Name { get; private set; }
        public long Length { get; }
        public DateTimeOffset? LastModified { get; }
        public string ContentType { get; }

        public AzureBlobStorageBlob(AzureBlobStorageContainer container, CloudBlockBlob nativeBlob)
        {
            Bucket = container;
            _nativeBlob = nativeBlob;
            Name = _nativeBlob.Uri.AbsoluteUri;
            Length = nativeBlob.Properties.Length;
            LastModified = nativeBlob.Properties.LastModified;
            ContentType = nativeBlob.Properties.ContentType;
        }

        public bool IsDirectory => false;
    }
}
