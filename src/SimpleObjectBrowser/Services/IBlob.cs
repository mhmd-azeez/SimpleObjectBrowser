using System;

namespace SimpleObjectBrowser.Services
{
    public interface IEntry
    {
        IStorageBucket Bucket { get; }
        string Name { get; }
        bool IsDirectory { get; }
    }

    public interface IBlob : IEntry
    {
        bool ContentTypeIsInferred { get; }
        string ContentType { get; }
        DateTimeOffset? LastModified { get; }
        long Length { get; }
    }
}