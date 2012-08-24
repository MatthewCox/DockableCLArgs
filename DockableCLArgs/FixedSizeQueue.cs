using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MattC.DockableCLArgs
{
    public class FixedSizeQueue<T> : Queue<T>
    {
        public uint Size { get; private set; }

        public FixedSizeQueue(uint size)
        {
            Size = size;
        }

        public new void Enqueue(T obj)
        {
            base.Enqueue(obj);
            while (base.Count > Size)
            {
                base.Dequeue();
            }
        }

        public void Resize(uint newSize)
        {
            Size = newSize;
            while (base.Count > Size)
            {
                base.Dequeue();
            }
        }
    }
}
