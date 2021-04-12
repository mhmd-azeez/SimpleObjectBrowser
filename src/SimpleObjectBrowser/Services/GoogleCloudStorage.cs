using Google.Apis.Auth.OAuth2;
using Google.Apis.Download;
using Google.Apis.Storage.v1.Data;
using Google.Apis.Upload;
using Google.Cloud.Storage.V1;

using MimeTypes;

using SimpleObjectBrowser.ViewModels;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleObjectBrowser.Services
{
    public class GoogleCloudStorageCredential : ICredential
    {
        public string Name { get; set; }
        public string Credentials { get; set; }
        public string Bucket { get; set; }

        public IStorageAccount Connect()
        {
            var googleCreds = GoogleCredential.FromJson(Credentials);

            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(Credentials)))
            {
                var serviceAccountCreds = ServiceAccountCredential.FromServiceAccountData(stream);
                return new GoogleCloudStorageAccount(googleCreds, serviceAccountCreds, Bucket, Name);
            }

        }
    }

    public class GoogleCloudStorageAccount : IStorageAccount
    {

        private readonly GoogleCredential _credentials;
        private readonly ServiceAccountCredential _serviceAccount;
        private readonly StorageClient _client;
        private readonly string _bucketName;

        public GoogleCloudStorageAccount(
            GoogleCredential credentials,
            ServiceAccountCredential serviceAccount,
            string bucketName,
            string name)
        {
            _credentials = credentials;
            _serviceAccount = serviceAccount;
            _client = StorageClient.Create(credentials);
            _bucketName = bucketName;
            Name = name ?? serviceAccount.ProjectId ?? "Google Cloud Storage";
        }

        public AccountType Type => AccountType.GoogleCloudStorage;

        public string Name { get; }

        public async Task<IEnumerable<IStorageBucket>> ListBucketsAsync()
        {
            if (_bucketName != null)
            {
                return new List<IStorageBucket> { new GoogleStorageBucket(_bucketName, _client, _serviceAccount) };
            }

            var buckets = await _client.ListBucketsAsync(_serviceAccount.ProjectId).ReadPageAsync(1024);

            return buckets.Select(b => new GoogleStorageBucket(b.Name, _client, _serviceAccount));
        }
    }

    public class GoogleStorageBucket : IStorageBucket
    {
        private StorageClient _client;
        private readonly ServiceAccountCredential _serviceAccount;

        public GoogleStorageBucket(
            string name,
            StorageClient client,
            ServiceAccountCredential serviceAccount)
        {
            _client = client;
            _serviceAccount = serviceAccount;
            Name = name;
        }

        public string Name { get; }

        public Task<IPagedResult<IEnumerable<IEntry>>> ListEntriesAsync(ListQuery query)
        {
            return ListEntriesAsync(query, null, null);
        }

        private async Task<IPagedResult<IEnumerable<IEntry>>> ListEntriesAsync(ListQuery query, string token, IPagedResult<IEnumerable<IEntry>> previous)
        {
            var request = new ListObjectsOptions
            {
                PageSize = query.PageSize,
                PageToken = token,
                //BucketName = _nativeBucket.BucketName,
                //MaxKeys = query.PageSize,
                //ContinuationToken = token,
                //Prefix = query.Prefix
            };

            if (query.Heirarchical)
            {
                request.Delimiter = "/";
            }

            var response = await _client.ListObjectsAsync(Name, query.Prefix, request).ReadPageAsync(query.PageSize);


            var blobs = new List<IEntry>();

            foreach (var blob in response)
            {
                var extension = blob.Name.Split('.').Last();
                MimeTypeMap.TryGetMimeType(extension, out var contentType);

                blobs.Add(new GoogleStorageBlob(this, _client, _serviceAccount, blob, contentType));
            }

            //foreach (var p in response.)
            //{
            //    blobs.Add(new S3Directory(p, this));
            //}

            Func<IPagedResult<IEnumerable<IEntry>>, Task<IPagedResult<IEnumerable<IEntry>>>> next = null;
            if (response.NextPageToken != null)
                next = prev => ListEntriesAsync(query, response.NextPageToken, prev);

            var currentPageNumber = (previous?.PageNumber ?? 0) + 1;

            return new PagedResult<IEnumerable<IEntry>>(
                query.PageSize,
                currentPageNumber,
                blobs,
                () => ListEntriesAsync(query, token, previous),
                previous,
                next);
        }

        public async Task UploadBlob(string fullName, Stream stream, string contentType, CancellationToken token, IProgress<long> progress)
        {
            var request = new Google.Apis.Storage.v1.Data.Object
            {
                Bucket = Name,
                Name = fullName,
                ContentType = contentType,
            };

            var p = new Progress<IUploadProgress>();
            p.ProgressChanged += (s, args) =>
            {
                progress.Report(args.BytesSent);
            };

            await _client.UploadObjectAsync(request, stream, cancellationToken: token, progress: p);
        }

        public async Task DeleteBlobs(IEnumerable<string> keys, CancellationToken token)
        {
            foreach (var key in keys)
            {
                await _client.DeleteObjectAsync(Name, key, cancellationToken: token);
            }
        }
    }

    public class GoogleStorageDirectory : IEntry
    {
        public GoogleStorageDirectory(string prefix, IStorageBucket s3Bucket)
        {
            Key = prefix;
            Bucket = s3Bucket;
        }

        public IStorageBucket Bucket { get; }
        public string Key { get; }
        public bool IsDirectory => true;
    }

    public class GoogleStorageBlob : IBlob
    {
        private readonly ServiceAccountCredential _serviceAccountCredential;
        private readonly Google.Apis.Storage.v1.Data.Object _nativeBlob;

        public GoogleStorageBlob(
            GoogleStorageBucket nativeBucket,
            StorageClient client,
            ServiceAccountCredential serviceAccountCredential,
            Google.Apis.Storage.v1.Data.Object nativeBlob,
            string contentType)
        {
            Bucket = nativeBucket;
            _client = client;
            _serviceAccountCredential = serviceAccountCredential;
            _nativeBlob = nativeBlob;
            Key = _nativeBlob.Name;
            Length = (long)_nativeBlob.Size;
            LastModified = _nativeBlob.Updated;
            ContentType = contentType;

            if (contentType != null)
                ContentTypeIsInferred = true;

            Tier = nativeBlob.StorageClass;
        }

        public IStorageBucket Bucket { get; }

        private readonly StorageClient _client;

        public string Key { get; }
        public string ContentType { get; }
        public DateTimeOffset? LastModified { get; }
        public long Length { get; }
        public bool IsDirectory => false;

        public bool ContentTypeIsInferred { get; }

        public async Task DownloadToStreamAsync(Stream target, IProgress<long> progress, CancellationToken token)
        {
            var p = new Progress<IDownloadProgress>();
            p.ProgressChanged += (s, args) =>
            {
                progress.Report(args.BytesDownloaded);
            };

            await _client.DownloadObjectAsync(_nativeBlob.Name, Key, target, cancellationToken: token, progress: p);
        }

        public Uri GetLink(TimeSpan lifeTime)
        {
            var signer = UrlSigner.FromServiceAccountCredential(_serviceAccountCredential);

            return new Uri(signer.Sign(_nativeBlob.Name, Key, lifeTime));
        }

        public string Tier { get; }
    }
}
