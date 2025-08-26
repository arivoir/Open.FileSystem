using C1.DataCollection;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Open.FileSystemAsync
{
    public class PagedList<T> : C1DataCollectionBase<T>, ISupportAsyncLoading<T>, ISupportEditing<T>
        where T : class
    {
        #region fields

        private SemaphoreSlim _semaphore = new SemaphoreSlim(1);
        private int _nextPageIndex = 0;
        private bool _firstPageLoaded;
        private Func<int, int, CancellationToken, Task<Tuple<int, IList<T>>>> _getPageWithCountAction;

        #endregion

        public PagedList(Func<int, int, CancellationToken, Task<Tuple<int, IList<T>>>> getPageWithCountAction, int pageSize = 200)
        {
            PageSize = pageSize;
            _getPageWithCountAction = getPageWithCountAction;
        }

        public int PageSize { get; private set; }
        public bool AddToTheBeginning { get; set; }

        public override T this[int index]
        {
            get
            {
                var item = base[index];
                if (item == null)
                {
                    var task = LoadAsync(index, index);
                }
                return item;
            }
        }

        /// <summary>
        /// Gets or sets the list that hold the items to be returned in the public api.
        /// </summary>
        private new List<T> InternalList
        {
            get
            {
                return (List<T>)base.InternalList;
            }
            set
            {
                base.InternalList = value;
            }
        }

        #region async loading

        public async Task LoadAsync(int? fromIndex = default(int?), int? toIndex = default(int?), CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                await _semaphore.WaitAsync();
                IList<T> page;
                var changes = new Queue<Tuple<int, T>>();
                var saveChanges = _firstPageLoaded;
                while (!_firstPageLoaded ||
                    _nextPageIndex <= Math.Min(Count - 1, toIndex ?? (Count - 1)))
                {
                    var pageWithCount = await _getPageWithCountAction(_nextPageIndex, PageSize, cancellationToken);
                    page = pageWithCount.Item2;
                    if (!_firstPageLoaded)
                    {
                        _firstPageLoaded = true;
                        InternalList = Enumerable.Range(0, pageWithCount.Item1).Select(i => default(T)).ToList();
                    }
                    // save items in the cache
                    for (int j = 0; j < page.Count; j++)
                    {
                        InternalList[_nextPageIndex + j] = page[j];
                    }
                    //save the changes
                    if (saveChanges)
                    {
                        for (int j = 0; j < page.Count; j++)
                        {
                            var item = page[j];
                            var index = _nextPageIndex + j;
                            changes.Enqueue(new Tuple<int, T>(index, item));
                        }
                    }
                    _nextPageIndex += page.Count;
                }
                //notify the changes
                if (!saveChanges)
                {
                    OnCollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
                }
                else
                {
                    while (changes.Count > 0)
                    {
                        var change = changes.Dequeue();
                        OnCollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, change.Item2, null, change.Item1));
                    }
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }


        class LoadedItem<T> : ILoadedItem<T>
        {
            public LoadedItem(int i, T item)
            {
                Index = i;
                Item = item;
            }

            public int Index { get; private set; }

            public T Item { get; private set; }
        }

        public IEnumerable<ILoadedItem<T>> GetLoadedItems(int? fromIndex = null, int? toIndex = null)
        {
            for (int i = 0; i < InternalList.Count; i++)
            {
                var item = InternalList[i];
                if (item != null)
                    yield return new LoadedItem<T>(i, item);
            }
        }

        #endregion

        #region editing

        public bool CanInsert(int index, T item)
        {
            return true;
        }

        public bool CanRemove(int index)
        {
            return true;
        }

        public bool CanReplace(int index, T item)
        {
            return true;
        }

        public bool CanMove(int fromIndex, int toIndex)
        {
            return false;
        }

        public async Task<int> InsertAsync(int index, T item, CancellationToken cancellationToken)
        {
            index = AddToTheBeginning ? 0 : Count;
            InternalList.Insert(index, item as T);
            if (index <= _nextPageIndex)
                _nextPageIndex++;
            var awaiter = NotifyCollectionChangedAsyncEventArgs.Create(NotifyCollectionChangedAction.Add, item, 0, cancellationToken);
            OnCollectionChanged(this, awaiter.EventArgs);
            await awaiter.WaitDeferralsAsync();
            return index;
        }

        public async Task RemoveAsync(int index, CancellationToken cancellationToken)
        {
            if (index < InternalList.Count)
            {
                var item = InternalList[index];
                InternalList.RemoveAt(index);
                if (index <= _nextPageIndex)
                    _nextPageIndex--;
                var awaiter = NotifyCollectionChangedAsyncEventArgs.Create(NotifyCollectionChangedAction.Remove, item, 0, cancellationToken);
                OnCollectionChanged(this, awaiter.EventArgs);
                await awaiter.WaitDeferralsAsync();
            }
        }

        public async Task<int> ReplaceAsync(int index, T item, CancellationToken cancellationToken)
        {
            if (index < InternalList.Count)
            {
                var oldItem = InternalList[index];
                InternalList[index] = item as T;
                var awaiter = NotifyCollectionChangedAsyncEventArgs.Create(NotifyCollectionChangedAction.Replace, item, oldItem, index, cancellationToken);
                OnCollectionChanged(this, awaiter.EventArgs);
                await awaiter.WaitDeferralsAsync();
                return index;
            }
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        public Task MoveAsync(int fromIndex, int toIndex, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }


        #endregion

    }
}
