using C1.DataCollection;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading;
using System.Threading.Tasks;

namespace Open.FileSystem
{
    public class CursorList<T> : C1CursorDataCollection<T>, ISupportEditing<T>
        where T : class
    {
        private Func<int, string, IReadOnlyList<SortDescription>, FilterExpression, CancellationToken, Task<Tuple<string, IReadOnlyList<T>>>> _getNextPageAction;
        private Func<IReadOnlyList<SortDescription>, bool> _canSort;

        public int PageSize { get; set; } = 200;

        public CursorList(Func<int, string, IReadOnlyList<SortDescription>, FilterExpression, CancellationToken, Task<Tuple<string, IReadOnlyList<T>>>> getNextPageAction, Func<IReadOnlyList<SortDescription>, bool> canSort = null)
        {
            this._getNextPageAction = getNextPageAction;
            this._canSort = canSort ?? ((arg) => false);
        }

        protected override Task<Tuple<string, IReadOnlyList<T>>> GetPageAsync(int startingIndex, string pageToken, int? count = default(int?), IReadOnlyList<SortDescription> sortDescriptions = null, FilterExpression filterExpresssion = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return _getNextPageAction(PageSize, pageToken, sortDescriptions, filterExpresssion, cancellationToken);
        }

        public override bool CanSort(params SortDescription[] sortDescriptions)
        {
            return _canSort(sortDescriptions);
        }

        public bool CanInsert(int index)
        {
            return true;
        }

        public bool CanRemove(int index)
        {
            return true;
        }

        public bool CanReplace(int index)
        {
            return true;
        }

        public bool CanMove(int fromIndex, int toIndex)
        {
            return false;
        }

        public async Task<int> InsertAsync(int index, T item)
        {
            InternalList.Insert(0, item as T);
            var awaiter = NotifyCollectionChangedAsyncEventArgs.Create(NotifyCollectionChangedAction.Add, item, 0, CancellationToken.None);
            OnCollectionChanged(this, awaiter.EventArgs);
            await awaiter.WaitDeferralsAsync();
            return 0;
        }

        public async Task RemoveAsync(int index)
        {
            if (index < InternalList.Count)
            {
                var item = InternalList[index];
                InternalList.RemoveAt(index);
                var awaiter = NotifyCollectionChangedAsyncEventArgs.Create(NotifyCollectionChangedAction.Remove, item, index, CancellationToken.None);
                OnCollectionChanged(this, awaiter.EventArgs);
                await awaiter.WaitDeferralsAsync();
            }
        }

        public async Task ReplaceAsync(int index, T item)
        {
            if (index < InternalList.Count)
            {
                var oldItem = InternalList[index];
                InternalList[index] = item as T;
                var awaiter = NotifyCollectionChangedAsyncEventArgs.Create(NotifyCollectionChangedAction.Replace, item, oldItem, index, CancellationToken.None);
                OnCollectionChanged(this, awaiter.EventArgs);
                await awaiter.WaitDeferralsAsync();
            }
        }

        public Task MoveAsync(int fromIndex, int toIndex)
        {
            throw new NotImplementedException();
        }
    }
}
