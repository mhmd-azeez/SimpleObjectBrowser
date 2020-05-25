using Amazon.S3;
using Amazon.S3.Model;

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
    public class S3CompatibleCredential : ICredential
    {
        public string Name { get; set; }
        public string AccessKey { get; set; }
        public string Secret { get; set; }
        public string HostName { get; set; }

        public IStorageAccount Connect()
        {
            var client = new AmazonS3Client(AccessKey, Secret, new AmazonS3Config
            {
                ServiceURL = HostName
            });

            return new S3StorageAccount(client, Name);
        }
    }

    public class S3StorageAccount : IStorageAccount
    {
        private AmazonS3Client _client;

        public S3StorageAccount(AmazonS3Client client, string name)
        {
            _client = client;
            _client.Config.Validate();
            Name = name ?? _client.Config.ServiceURL;
        }

        public AccountType Type => AccountType.AwsS3;
        public string Name { get; }

        public async Task<IEnumerable<IStorageBucket>> ListBucketsAsync()
        {
            var buckets = await _client.ListBucketsAsync();

            return buckets.Buckets.Select(b => new S3Bucket(b, _client));
        }
    }

    public class S3Bucket : IStorageBucket
    {
        private Amazon.S3.Model.S3Bucket _nativeBucket;
        private readonly AmazonS3Client _client;

        public S3Bucket(Amazon.S3.Model.S3Bucket nativeBucket, AmazonS3Client _client)
        {
            _nativeBucket = nativeBucket;
            this._client = _client;
            Name = _nativeBucket.BucketName;
        }

        public string Name { get; }

        public Task<IPagedResult<IEnumerable<IEntry>>> ListEntriesAsync(ListQuery query)
        {
            return ListEntriesAsync(query, null, null);
        }

        private async Task<IPagedResult<IEnumerable<IEntry>>> ListEntriesAsync(ListQuery query, string token, IPagedResult<IEnumerable<IEntry>> previous)
        {
            var request = new ListObjectsV2Request
            {
                BucketName = _nativeBucket.BucketName,
                MaxKeys = query.PageSize,
                ContinuationToken = token,
                Prefix = query.Prefix
            };

            if (query.Heirarchical)
            {
                request.Delimiter = "/";
            }

            var response = await _client.ListObjectsV2Async(request);

            var blobs = new List<IEntry>();

            foreach (var blob in response.S3Objects)
            {
                var metadata = await _client.GetObjectMetadataAsync(new GetObjectMetadataRequest
                {
                    BucketName = blob.BucketName,
                    Key = blob.Key
                });

                var contentType = metadata.Headers.ContentType.Split(';').First();

                blobs.Add(new S3Blob(this, blob, contentType));
            }

            foreach (var p in response.CommonPrefixes)
            {
                blobs.Add(new S3Directory(p, this));
            }

            Func<IPagedResult<IEnumerable<IEntry>>, Task<IPagedResult<IEnumerable<IEntry>>>> next = null;
            if (response.IsTruncated)
                next = prev => ListEntriesAsync(query, response.NextContinuationToken, prev);

            return new PagedResult<IEnumerable<IEntry>>(
                blobs,
                () => ListEntriesAsync(query, token, previous),
                previous,
                next);
        }

        public async Task UploadBlob(string fullName, Stream stream, string contentType, CancellationToken token, IProgress<long> progress)
        {
            var request = new PutObjectRequest
            {
                BucketName = _nativeBucket.BucketName,
                ContentType = contentType,
                InputStream = stream,
                Key = fullName,
            };

            request.StreamTransferProgress += (s, args) =>
            {
                progress.Report(args.TransferredBytes);
            };

            await _client.PutObjectAsync(request, token);
        }

        public async Task DeleteBlobs(IEnumerable<string> keys, CancellationToken token)
        {
            // TODO: Batch into 1000 items...
            var kvs = keys.Select(k => new KeyVersion { Key = k }).ToList();

            var response = await _client.DeleteObjectsAsync(new DeleteObjectsRequest
            {
                BucketName = _nativeBucket.BucketName,
                Objects = kvs,
                Quiet = true
            }, token);
        }

    }

    public class S3Directory : IEntry
    {

        public S3Directory(string prefix, IStorageBucket s3Bucket)
        {
            Name = prefix;
            Bucket = s3Bucket;
        }

        public IStorageBucket Bucket { get; }
        public string Name { get; }
        public bool IsDirectory => true;
    }

    public class S3Blob : IBlob
    {
        private S3Object _nativeBlob;

        public S3Blob(S3Bucket s3Bucket, S3Object nativeBlob, string contentType)
        {
            Bucket = s3Bucket;
            _nativeBlob = nativeBlob;
            Name = _nativeBlob.Key;
            Length = _nativeBlob.Size;
            LastModified = _nativeBlob.LastModified;
            ContentType = contentType;
        }

        public IStorageBucket Bucket { get; }
        public string Name { get; }
        public string ContentType { get; }
        public DateTimeOffset? LastModified { get; }
        public long Length { get; }
        public bool IsDirectory => false;
    }
}
