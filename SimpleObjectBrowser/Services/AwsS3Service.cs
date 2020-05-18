using Amazon.S3;
using Amazon.S3.Model;
using SimpleObjectBrowser.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

        public async Task<IEnumerable<IEntry>> ListEntriesAsync(string prefix, bool heirarchical)
        {
            var request = new ListObjectsV2Request
            {
                BucketName = _nativeBucket.BucketName,
                MaxKeys = 25,
                Prefix = prefix
            };

            if (heirarchical)
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

                blobs.Add(new S3Blob(this, blob, metadata.Metadata));
            }

            foreach (var p in response.CommonPrefixes)
            {
                blobs.Add(new S3Directory(p, this));
            }

            return blobs;
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

        public S3Blob(S3Bucket s3Bucket, S3Object nativeBlob, MetadataCollection metadata)
        {
            Bucket = s3Bucket;
            _nativeBlob = nativeBlob;
            Name = _nativeBlob.Key;
            Length = _nativeBlob.Size;
            LastModified = _nativeBlob.LastModified;
            ContentType = metadata["Content-Type"];
            // TODO: ContentType
        }

        public IStorageBucket Bucket { get; }
        public string Name { get; }
        public string ContentType { get; }
        public DateTimeOffset? LastModified { get; }
        public long Length { get; }
        public bool IsDirectory => false;
    }
}
