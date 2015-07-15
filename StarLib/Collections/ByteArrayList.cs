using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StarLib.Collections
{
    public class ByteArrayList : IList<byte>
    {
        private byte[] _buffer;

        private int _count;

        public ByteArrayList() : this(256)
        {
        }

        public ByteArrayList(int initialCapcity)
        {
            _buffer = new byte[initialCapcity];
            _count = 0;
        }

        public IEnumerator<byte> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(byte item)
        {
            EnsureCapcity(_count + 1);

            _buffer[_count] = item;

            _count++;
        }

        public void AddRange(byte[] buffer, int offset, int count)
        {
            EnsureCapcity(_count + count);

            Buffer.BlockCopy(buffer, offset, _buffer, _count, count);

            _count += count;
        }

        public void Clear()
        {
            Array.Clear(_buffer, 0, _buffer.Length);
        }

        public bool Contains(byte item)
        {
            return _buffer.Contains(item);
        }

        public void CopyTo(byte[] array, int arrayIndex)
        {
            Buffer.BlockCopy(_buffer, 0, array, arrayIndex, _buffer.Length);
        }

        public bool Remove(byte item)
        {
            throw new NotImplementedException();
        }

        public byte[] GetBuffer()
        {
            return _buffer;
        }

        protected void EnsureCapcity(int newLen)
        {
            if (newLen > _buffer.Length)
            {
                const int multiplier = 2;

                byte[] newBuf = new byte[newLen * multiplier];

                Buffer.BlockCopy(_buffer, 0, newBuf, 0, _buffer.Length);

                _buffer = newBuf;
            }
        }

        public int Count
        {
            get
            {
                return _count;
            }
        }

        public bool IsReadOnly
        {
            get
            {
                return false;
            }
        }

        public int Capacity
        {
            get
            {
                return _buffer.Length;
            }
        }

        public int IndexOf(byte item)
        {
            throw new NotImplementedException();
        }

        public void Insert(int index, byte item)
        {
            byte[] buf = { item };

            InsertRange(index, buf, 0, buf.Length);
        }

        public void InsertRange(int index, byte[] buffer, int offset, int count)
        {
            EnsureCapcity(_count + count);

            Buffer.BlockCopy(_buffer, index, _buffer, index + count, _buffer.Length - (index + count));
            Buffer.BlockCopy(buffer, offset, _buffer, index, count);

            _count += count;
        }

        public byte[] GetRange(int offset, int count)
        {
            byte[] buf = new byte[count];
            CopyRange(buf, offset, count);

            return buf;
        }

        public int CopyRange(byte[] buffer, int offset, int count)
        {
            int c = Math.Min(_count, count);

            Buffer.BlockCopy(_buffer, offset, buffer, 0, c);

            return c;
        }

        public byte[] AsArray()
        {
            byte[] buf = new byte[_count];

            Buffer.BlockCopy(_buffer, 0, buf, 0, buf.Length);

            return buf;
        }

        public void RemoveAt(int index)
        {
            if (index >= _count)
                throw new ArgumentOutOfRangeException();

            RemoveRange(index, 1);
        }

        public void RemoveRange(int offset, int count)
        {
            if (offset + count >= _count)
                throw new ArgumentOutOfRangeException();

            Buffer.BlockCopy(_buffer, offset + count, _buffer, offset, _count - count);
            Array.Clear(_buffer, _count - count, count);

            _count -= count;
        }

        public byte this[int index]
        {
            get { return _buffer[index]; }
            set { _buffer[index] = value; }
        }
    }
}
