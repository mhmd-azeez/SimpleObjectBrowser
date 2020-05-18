using SimpleObjectBrowser.ViewModels;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SimpleObjectBrowser.Services
{
    public interface IStorageAccount
    {
        AccountType Type { get; }
        string Name { get; }

        Task<IEnumerable<IStorageBucket>> ListBucketsAsync();
    }
}