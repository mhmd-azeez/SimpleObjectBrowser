using SimpleObjectBrowser.ViewModels;

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace SimpleObjectBrowser.Services
{
    public interface IPagedResult<T>
    {
        T Result { get; }
        Task<IPagedResult<T>> GetNextPage();
        bool HasNextPage();
        bool HasPreviousPage();
        Task<IPagedResult<T>> Refresh();

        IPagedResult<T> Previous { get; }
    }

    public class PagedResult<T> : IPagedResult<T>
    {
        private readonly Func<Task<IPagedResult<T>>> _resultFactory;
        private readonly Func<IPagedResult<T>, Task<IPagedResult<T>>> _next;

        public PagedResult(
            T result, 
            Func<Task<IPagedResult<T>>> resultFactory, 
            IPagedResult<T> previous = null,
            Func<IPagedResult<T>, Task<IPagedResult<T>>> next = null)
        {
            Result = result;
            _resultFactory = resultFactory;
            Previous = previous;
            _next = next;
        }

        public T Result { get; private set; }
        public IPagedResult<T> Previous { get; }

        public Task<IPagedResult<T>> GetNextPage()
        {
            if (_next is null)
            {
                throw new NotSupportedException();
            }

            return _next(this);
        }

        public Task<IPagedResult<T>> Refresh()
        {
            return _resultFactory();
        }

        public bool HasNextPage()
        {
            return _next != null;
        }

        public bool HasPreviousPage()
        {
            return Previous != null;
        }
    }

    public interface IStorageAccount
    {
        AccountType Type { get; }
        string Name { get; }

        Task<IEnumerable<IStorageBucket>> ListBucketsAsync();
    }
}