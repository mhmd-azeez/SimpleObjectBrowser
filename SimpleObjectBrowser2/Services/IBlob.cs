using System;

namespace SimpleObjectBrowser.Services
{
    public interface IBlob
    {
        IStorageBucket Bucket { get; }
        string Name { get; }
        string ContentType { get; }
        DateTimeOffset? LastModified { get; }
        long Length { get; }
    }
}