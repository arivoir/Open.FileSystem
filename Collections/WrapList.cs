using C1.DataCollection;
using System.Collections;
using System.Collections.Generic;

namespace Open.FileSystemAsync
{
    //public class WrapList<T> : WrapCollectionView<T>
    //    where T : class
    //{
    //    public WrapList(IEnumerable<T> innerList)
    //        : base(innerList)
    //    {
    //    }
    //}

    public class EmptyCollectionView<T> : C1WrapDataCollection<T>
        where T : class
    {
        public EmptyCollectionView()
        {
            base.Source = new List<T>();
        }
    }
}
