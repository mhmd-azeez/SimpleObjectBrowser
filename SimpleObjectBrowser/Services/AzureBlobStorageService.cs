using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using SimpleObjectBrowser.ViewModels;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SimpleObjectBrowser.Services
{
    public interface ICredential
    {
        public IStorageAccount Connect();
    }

    public class AzureBlobStorageConnectionStringCredential : ICredential
    {
        public string ConnectionString { get; set; }
        public IStorageAccount Connect()
        {
            return new AzureBlobStorageAccount(CloudStorageAccount.Parse(ConnectionString));
        }
    }

    public class AzureBlobStorageAccount : IStorageAccount
    {
        private readonly CloudStorageAccount _nativeAccount;
        private readonly CloudBlobClient _blobClient;

        public AzureBlobStorageAccount(CloudStorageAccount nativeAccount)
        {
            _nativeAccount = nativeAccount;
            _blobClient = _nativeAccount.CreateCloudBlobClient();
        }

        public string Name => _nativeAccount.Credentials.AccountName;

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

        public async Task<IEnumerable<IBlob>> ListBlobsAsync()
        {
            var segmentedResult = await _nativeContainer.ListBlobsSegmentedAsync(
                string.Empty, true, BlobListingDetails.Metadata, null, null, null, null);

            return segmentedResult.Results.Select(b => new AzureBlobStorageBlob(this, b));
        }
    }

    public class AzureBlobStorageBlob : IBlob
    {
        public IStorageBucket Bucket { get; }

        private IListBlobItem _nativeBlob;

        public string Name { get; private set; }

        public AzureBlobStorageBlob(AzureBlobStorageContainer container, IListBlobItem nativeBlob)
        {
            Bucket = container;
            _nativeBlob = nativeBlob;
            Name = _nativeBlob.Uri.AbsoluteUri;
        }
    }
}
