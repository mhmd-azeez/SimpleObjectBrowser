using SimpleObjectBrowser.Mvvm;
using SimpleObjectBrowser.Services;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleObjectBrowser.ViewModels
{
    public static class PathHelper
    {
        public static string Combine(params string[] parts)
        {
            if (parts.Length == 0)
                return string.Empty;

            var path = new StringBuilder(parts[0]);

            for (int i = 1; i < parts.Length; i++)
            {
                if (path.Length > 0)
                    path.Append($"{path}/{parts[i]}");
                else
                    path.Append(parts[i]);
            }

            return path.ToString();
        }

        public static string GetPrefix(string key, char delimiter = '/')
        {
            var parts = key.Split(delimiter);

            var builder = new StringBuilder();

            for (int i = 0; i < parts.Length - 1; i++)
            {
                builder.Append(parts[i]);

                if (i != parts.Length - 1)
                    builder.Append(delimiter);
            }

            return builder.ToString();
        }
    }

    public abstract class TaskViewModel : BindableBase
    {
        protected CancellationTokenSource _tokenSource = new CancellationTokenSource();

        private string _text;
        public string Text
        {
            get { return _text; }
            protected set { Set(ref _text, value); }
        }

        private double _progress;
        public double Progress
        {
            get { return _progress; }
            protected set { Set(ref _progress, value); }
        }

        private bool _isIndeterminate;
        public bool IsIndeterminate
        {
            get { return _isIndeterminate; }
            set { Set(ref _isIndeterminate, value); }
        }

        public event EventHandler<string> Failed;
        public event EventHandler Succeeded;
        public event EventHandler Completed;

        protected void OnFailed(string message)
        {
            Failed?.Invoke(this, message);
            Completed?.Invoke(this, EventArgs.Empty);
        }
        protected void OnSucceeded()
        {
            Succeeded?.Invoke(this, EventArgs.Empty);
            Completed?.Invoke(this, EventArgs.Empty);
        }

        public void Cancel()
        {
            _tokenSource.Cancel();
            OnSucceeded();
        }

        public abstract Task StartAsync();
    }

    public class FileInfo
    {
        public string Name { get; set; }
        public Func<Stream> OpenStream { get; set; }
        public long Length { get; set; }
        public string ContentType { get; set; }
    }

    public class DeleteBlobsTaskViewModel : TaskViewModel
    {
        private readonly IEnumerable<string> _prefixes;
        private readonly IStorageBucket _bucket;

        public DeleteBlobsTaskViewModel(string prefix, IStorageBucket bucket) : this(new[] { prefix }, bucket)
        {

        }

        public DeleteBlobsTaskViewModel(IEnumerable<string> prefixes, IStorageBucket bucket)
        {
            _prefixes = prefixes;
            _bucket = bucket;

            Text = $"Deleting {prefixes.Count()} blobs...";
            IsIndeterminate = true;
        }

        public async override Task StartAsync()
        {
            try
            {
                var keys = new List<string>();

                foreach (var prefix in _prefixes)
                {
                    var expanded = await _bucket.ListAllEntries(prefix, false);
                    keys.AddRange(expanded.Select(i => i.Key));
                }

                await _bucket.DeleteBlobs(keys, _tokenSource.Token);
                OnSucceeded();
            }
            catch (Exception ex)
            {
                OnFailed(ex.Message);
            }
        }
    }

    public class UploadBlobsTaskViewModel : TaskViewModel
    {
        private readonly IEnumerable<FileInfo> _files;
        private readonly string _prefix;
        private readonly IStorageBucket _bucket;

        public UploadBlobsTaskViewModel(FileInfo file, string prefix, IStorageBucket bucket) : this(new[] { file }, prefix, bucket)
        {

        }

        public UploadBlobsTaskViewModel(IEnumerable<FileInfo> files, string prefix, IStorageBucket bucket)
        {
            _files = files;
            _prefix = prefix;
            _bucket = bucket;

            Text = $"Uploading {files.Count()} files...";
        }

        public override async Task StartAsync()
        {
            try
            {
                var total = _files.Sum(f => f.Length);
                double done = 0;

                int count = _files.Count();
                int processed = 1;

                foreach (var file in _files)
                {
                    _tokenSource.Token.ThrowIfCancellationRequested();

                    Text = $"Uploading {count} files ({processed}: '{file.Name}')...";

                    var progress = new Progress<long>();
                    progress.ProgressChanged += (s, transferred) =>
                    {
                        Progress = (done + transferred) / total;
                    };

                    var fullName = PathHelper.Combine(_prefix, file.Name);

                    using (var stream = file.OpenStream())
                    {
                        await _bucket.UploadBlob(fullName, stream, file.ContentType, _tokenSource.Token, progress);
                    }

                    done += file.Length;
                    processed++;
                    Progress = done / total;
                }

                OnSucceeded();
            }
            catch (Exception ex)
            {
                OnFailed(ex.Message);
            }
        }
    }

    public class DownloadBlobsTaskViewModel : TaskViewModel
    {
        private readonly string _localFolderPath;
        private readonly IList<IEntry> _entries;

        public DownloadBlobsTaskViewModel(string localFolderPath, IList<IEntry> entries)
        {
            _localFolderPath = localFolderPath;
            _entries = entries;

            Text = $"Downloading {entries.Count} entries...";
        }

        public override async Task StartAsync()
        {
            try
            {
                foreach (var directory in _entries.Where(e => e.IsDirectory).ToArray())
                {
                    _entries.Remove(directory);
                    var children = await directory.ListAllBlobsAsync();

                    foreach (var child in children)
                    {
                        _entries.Add(child);
                    }
                }

                var blobs = _entries.OfType<IBlob>().ToArray();

                var total = blobs.Sum(f => f.Length);
                double done = 0;

                int count = blobs.Length;
                int processed = 1;

                foreach (var blob in blobs)
                {
                    _tokenSource.Token.ThrowIfCancellationRequested();

                    Text = $"Downloading {count} blobs ({processed}: '{blob.Key}')...";

                    var progress = new Progress<long>();
                    progress.ProgressChanged += (s, transferred) =>
                    {
                        Progress = (done + transferred) / total;
                    };

                    var path = Path.Combine(_localFolderPath, PathHelper.GetPrefix(blob.Key));
                    if (Directory.Exists(path) == false)
                    {
                        Directory.CreateDirectory(path);
                    }

                    var fullPath = Path.Combine(_localFolderPath, blob.Key);

                    using (var output = File.OpenWrite(fullPath))
                    {
                        await blob.DownloadToStreamAsync(output, progress, _tokenSource.Token);
                    }

                    done += blob.Length;
                    processed++;
                    Progress = done / total;
                }

                OnSucceeded();
            }
            catch (Exception ex)
            {
                OnFailed(ex.Message);
            }
        }
    }
}
