using C1.DataCollection;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Open.FileSystem
{
    public class ReadOnlyCollectionView<T> : C1WrapDataCollection<T>
        where T : class
    {

        public ReadOnlyCollectionView(IEnumerable<T> source)
            : base(source)
        {
        }

        public override bool CanInsert(int index, T item)
        {
            return false;
        }

        public override bool CanRemove(int index)
        {
            return false;
        }

        public override bool CanReplace(int index, T item)
        {
            return false;
        }

        public override bool CanMove(int fromIndex, int toIndex)
        {
            return false;
        }

        public override Task<int> InsertAsync(int index, T item, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public override Task RemoveAsync(int index, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public override Task<int> ReplaceAsync(int index, T item, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public override Task MoveAsync(int fromIndex, int toIndex, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
