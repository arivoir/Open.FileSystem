using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Open.FileSystem
{
    public class WeakDictionary<TKey, TValue> : IDictionary<TKey, TValue>
        where TValue : class
    {
        private Dictionary<TKey, WeakReference<TValue>> _dictionary = new Dictionary<TKey, WeakReference<TValue>>();
        public TValue this[TKey key]
        {
            get
            {
                TValue value;
                if (TryGetValue(key, out value))
                    return value;
                else
                    throw new KeyNotFoundException();
            }

            set
            {
                _dictionary[key] = new WeakReference<TValue>(value);
            }
        }

        public int Count
        {
            get
            {
                Clean();
                return _dictionary.Count;
            }
        }

        public bool IsReadOnly
        {
            get
            {
                return (_dictionary as ICollection<KeyValuePair<TKey, WeakReference<TValue>>>).IsReadOnly;
            }
        }

        public ICollection<TKey> Keys
        {
            get
            {
                Clean();
                return _dictionary.Keys;
            }
        }

        public ICollection<TValue> Values
        {
            get
            {
                Clean();
                return _dictionary.Values.Select(reference =>
                {
                    TValue target;
                    if (reference.TryGetTarget(out target))
                    {
                        return target;
                    }
                    else
                    {
                        return default(TValue);
                    }
                }).ToList();
            }
        }

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            _dictionary.Add(item.Key, new WeakReference<TValue>(item.Value));
        }

        public void Add(TKey key, TValue value)
        {
            _dictionary.Add(key, new WeakReference<TValue>(value));
        }

        public void Clear()
        {
            _dictionary.Clear();
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            return _dictionary.ContainsKey(item.Key);
        }

        public bool ContainsKey(TKey key)
        {
            return _dictionary.ContainsKey(key);
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            foreach (var item in GetItems())
            {
                if (arrayIndex >= array.Length)
                    break;
                array[arrayIndex] = item;
                arrayIndex++;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return GetItems().GetEnumerator();
        }

        private IEnumerable<KeyValuePair<TKey, TValue>> GetItems()
        {
            foreach (var pair in _dictionary)
            {
                TValue target;
                if (pair.Value.TryGetTarget(out target))
                    yield return new KeyValuePair<TKey, TValue>(pair.Key, target);
            }
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            return _dictionary.Remove(item.Key);
        }

        public bool Remove(TKey key)
        {
            return _dictionary.Remove(key);
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            WeakReference<TValue> reference;
            if (_dictionary.TryGetValue(key, out reference))
            {
                TValue target;
                if (reference.TryGetTarget(out target))
                {
                    value = target;
                    return true;
                }
            }
            value = default(TValue);
            return false;
        }

        private void Clean()
        {
            foreach (var item in _dictionary.ToArray())
            {
                TValue target;
                if (!item.Value.TryGetTarget(out target))
                {
                    _dictionary.Remove(item.Key);
                }
            }
        }
    }
}