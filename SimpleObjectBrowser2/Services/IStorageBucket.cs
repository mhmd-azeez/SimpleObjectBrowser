using System.Collections.Generic;
using System.Threading.Tasks;

namespace SimpleObjectBrowser.Services
{
    public interface IStorageBucket
    {
        string Name { get; }

        Task<IEnumerable<IBlob>> ListBlobsAsync();
    }
}