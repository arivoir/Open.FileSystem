using C1.DataCollection;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Open.FileSystemAsync
{
    public class TransformList<S, T> : C1WrapDataCollection<S, T>
        where S : class
        where T : class
    {
        #region fields

        private Func<S, T> _convert;
        private Func<T, S> _convertBack;

        #endregion

        #region initialization

        public TransformList(IEnumerable<S> source) :
            base(source)
        {
        }

        public TransformList(IEnumerable<S> source, Func<S, T> convert, Func<T, S> convertBack, bool cacheTransformedItems = false) :
            this(source)
        {
            _convert = convert;
            _convertBack = convertBack;
        }

        #endregion

        #region object model

        protected override IReadOnlyList<T> CreateInternalList(IReadOnlyList<S> source)
        {
            return Enumerable.Range(0, source.Count()).Select(i => default(T)).ToList();
        }

        protected internal new List<T> InternalList
        {
            get
            {
                return base.InternalList as List<T>;
            }
        }

        public override T this[int index]
        {
            get
            {
                var item = InternalList[index];
                if (item == null)
                {
                    item = Transform(index, Source[index]);
                    if (item != null)
                    {
                        InternalList[index] = item;
                        OnItemSet(index, item);
                    }
                }
                return item;
            }
        }

        #endregion

        #region transform

        protected virtual T Transform(int index, S item)
        {
            if (_convert == null)
                throw new NotSupportedException();

            return _convert(item);
        }

        protected virtual S TransformBack(T item)
        {
            if (_convertBack == null)
                throw new NotSupportedException();

            return _convertBack(item);
        }

        #endregion

        protected virtual void OnItemSet(int index, T item)
        {
        }

        protected virtual void OnItemRemoved(int index, T item)
        {
        }

        protected override async void OnSourceCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    {
                        var deferral = e.GetDeferral();
                        try
                        {
                            {
                                var transformedAddedItems = new List<T>();
                                for (int i = 0; i < e.NewItems.Count; i++)
                                {
                                    var newItem = e.NewItems[i] as S;
                                    var newIndex = e.NewStartingIndex + i;
                                    var transformedItem = Transform(newIndex, newItem);
                                    InternalList.Insert(newIndex, transformedItem);
                                    OnItemSet(newIndex, transformedItem);
                                    transformedAddedItems.Add(transformedItem);
                                }
                                var awaiter = NotifyCollectionChangedAsyncEventArgs.Create(NotifyCollectionChangedAction.Add, transformedAddedItems, e.NewStartingIndex, CancellationToken.None);
                                OnCollectionChanged(this, awaiter.EventArgs);
                                await awaiter.WaitDeferralsAsync();
                            }
                        }
                        finally
                        {
                            deferral.Complete();
                        }
                    }
                    break;
                case NotifyCollectionChangedAction.Move:
                    throw new NotImplementedException();
                case NotifyCollectionChangedAction.Remove:
                    {
                        var deferral = e.GetDeferral();
                        try
                        {
                            var transformedRemovedItems = new List<T>();
                            for (int i = e.OldItems.Count - 1; i >= 0; i--)
                            {
                                var oldIndex = e.OldStartingIndex + i;
                                var oldItem = InternalList[oldIndex];
                                if (oldItem != null)
                                    OnItemRemoved(oldIndex, oldItem);
                                InternalList.RemoveAt(oldIndex);
                                transformedRemovedItems.Add(oldItem);
                            }
                            var awaiter = NotifyCollectionChangedAsyncEventArgs.Create(NotifyCollectionChangedAction.Remove, transformedRemovedItems, e.OldStartingIndex, CancellationToken.None);
                            OnCollectionChanged(this, awaiter.EventArgs);
                            await awaiter.WaitDeferralsAsync();
                        }
                        finally
                        {
                            deferral.Complete();
                        }
                    }
                    break;
                case NotifyCollectionChangedAction.Replace:
                    {
                        var deferral = e.GetDeferral();
                        try
                        {
                            var transformedReplacedItems = new List<T>();
                            var oldItems = new List<T>();
                            for (int i = 0; i < e.NewItems.Count; i++)
                            {
                                var originalItem = e.NewItems[i] as S;
                                var index = e.NewStartingIndex + i;
                                var oldItem = InternalList[index];
                                if (oldItem != null)
                                    OnItemRemoved(index, oldItem);
                                var transformedItem = Transform(index, originalItem);
                                InternalList[index] = transformedItem;
                                OnItemSet(index, transformedItem);
                                oldItems.Add(oldItem);
                                transformedReplacedItems.Add(transformedItem);
                            }
                            var awaiter = NotifyCollectionChangedAsyncEventArgs.Create(NotifyCollectionChangedAction.Replace, transformedReplacedItems, oldItems, e.NewStartingIndex, CancellationToken.None);
                            OnCollectionChanged(this, awaiter.EventArgs);
                            await awaiter.WaitDeferralsAsync();
                        }
                        finally
                        {
                            deferral.Complete();
                        }
                    }
                    break;
                default:
                case NotifyCollectionChangedAction.Reset:
                    {
                        var deferral = e.GetDeferral();
                        try
                        {
                            base.InternalList = CreateInternalList(Source);
                            var awaiter = NotifyCollectionChangedAsyncEventArgs.Create(NotifyCollectionChangedAction.Reset, CancellationToken.None);
                            OnCollectionChanged(this, awaiter.EventArgs);
                            await awaiter.WaitDeferralsAsync();
                        }
                        finally
                        {
                            deferral.Complete();
                        }
                    }
                    break;
            }
        }
    }
}
