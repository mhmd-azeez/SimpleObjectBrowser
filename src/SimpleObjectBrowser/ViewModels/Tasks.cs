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
                    path.Append($"{path}/{parts[i + 1]}");
                else
                    path.Append(parts[i + 1]);
            }

            return path.ToString();
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

        public event EventHandler Failed;
        public event EventHandler Succeeded;

        private bool _hasFailed;
        public bool HasFailed
        {
            get { return _hasFailed; }
            set { Set(ref _hasFailed, value); }
        }

        private bool _hasSucceeded;
        public bool HasSucceeded
        {
            get { return _hasSucceeded; }
            set { Set(ref _hasSucceeded, value); }
        }

        private void OnFailed()
        {
            HasFailed = true;
            HasSucceeded = false;
            Failed?.Invoke(this, EventArgs.Empty);
        }
        private void OnSucceeded()
        {
            HasFailed = false;
            HasSucceeded = true;
            Succeeded?.Invoke(this, EventArgs.Empty);
        }

        public void Cancel()
        {
            _tokenSource.Cancel();
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

            Text = $"Deleting {prefixes.Count()} files...";
        }

        public async override Task StartAsync()
        {
            try
            {
                var keys = new List<string>();

                foreach (var prefix in _prefixes)
                {
                    var expanded = await _bucket.ListEntriesAsync(prefix, false);
                    keys.AddRange(expanded.Select(i => i.Name));
                }

                await _bucket.DeleteBlobs(keys, _tokenSource.Token);
            }
            catch (Exception ex)
            {
                throw;
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
            }
            catch (Exception ex)
            {
                throw;
            }
        }
    }
}
