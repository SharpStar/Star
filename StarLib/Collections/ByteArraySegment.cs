// SharpStar. A Starbound wrapper.
// Copyright (C) 2015 Mitchell Kutchuk
// 
// This program is free software; you can redistribute it and/or
// modify it under the terms of the GNU General Public License
// as published by the Free Software Foundation; either version 2
// of the License, or (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software
// Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StarLib.Collections
{
    //http://stackoverflow.com/questions/23413068/fast-way-to-copy-an-array-into-a-list
    public class ByteArraySegment : ICollection<byte>
    {
        private readonly byte[] _array;
        private readonly int _start;
        private readonly int _count;

        public ByteArraySegment(byte[] array, int start, int count)
        {
            _array = array;
            _start = start;
            _count = count;
        }

        public void Add(byte item)
        {
            throw new NotImplementedException();
        }

        public void Clear()
        {
            throw new NotImplementedException();
        }

        public bool Contains(byte item)
        {
            return _array.Contains(item);
        }

        public void CopyTo(byte[] target, int index)
        {
            Buffer.BlockCopy(_array, _start, target, index, _count);
        }

        public bool Remove(byte item)
        {
            throw new NotImplementedException();
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
                return true;
            }
        }

        public byte this[int index]
        {
            get
            {
                return _array[index];
            }
            set
            {
                if (index > _array.Length)
                    throw new ArgumentOutOfRangeException("value");

                _array[index] = value;
            }
        }

        public IEnumerator<byte> GetEnumerator()
        {
            return new ByteArraySegmentEnumerator(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        class ByteArraySegmentEnumerator : IEnumerator<byte>
        {
            private byte _current;
            private int _pos;

            private readonly ByteArraySegment _segment;

            public ByteArraySegmentEnumerator(ByteArraySegment segment)
            {
                _segment = segment;
                _pos = segment._start;
            }

            public bool MoveNext()
            {
                if (_pos >= _segment.Count)
                    return false;

                _current = _segment._array[++_pos];

                return true;
            }

            public void Reset()
            {
                _pos = _segment._start;
            }

            public byte Current
            {
                get
                {
                    return _current;
                }
            }

            public void Dispose()
            {
            }

            object IEnumerator.Current
            {
                get { return Current; }
            }

        }
    }
}
