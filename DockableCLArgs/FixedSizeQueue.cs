using System.Collections;
using System.Collections.Generic;

namespace MattC.DockableCLArgs
{
    public class FixedSizeQueue<T>
    {
        Queue<T> _queue;

        public uint Size { get; private set; }

        public FixedSizeQueue(uint size)
        {
            _queue = new Queue<T>();
            Size = size;
        }

        public virtual void Enqueue(T obj)
        {
            _queue.Enqueue(obj);
            while (_queue.Count > Size)
            {
                _queue.Dequeue();
            }
        }

        public void Resize(uint newSize)
        {
            Size = newSize;
            while (_queue.Count > Size)
            {
                _queue.Dequeue();
            }
        }

        public bool Contains(T obj)
        {
            return _queue.Contains(obj);
        }

        public T[] ToArray()
        {
            return _queue.ToArray();
        }

        public int Count
        {
            get { return _queue.Count; }
        }

        public IEnumerator GetEnumerator()
        {
            return _queue.GetEnumerator();
        }
    }
}
