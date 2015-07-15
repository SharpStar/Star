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
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace StarLib.Server
{
    public class BufferManager
    {
        private readonly object _bufferLocker = new object();

        private readonly List<byte[]> _buffers;

        private readonly int _bufferSize;

        private readonly Stack<int> _availableBuffers;

        public BufferManager(int bufferSize)
        {
            _bufferSize = bufferSize;
            _buffers = new List<byte[]>();
            _availableBuffers = new Stack<int>();
        }

        public void SetBuffer(SocketAsyncEventArgs args)
        {
            if (_availableBuffers.Count > 0)
            {
                int index = _availableBuffers.Pop();

                byte[] buffer;
                lock (_bufferLocker)
                {
                    buffer = _buffers[index];
                }

                args.SetBuffer(buffer, 0, buffer.Length);
            }
            else
            {
                byte[] buffer = new byte[_bufferSize];

                lock (_bufferLocker)
                {
                    _buffers.Add(buffer);
                }

                args.SetBuffer(buffer, 0, buffer.Length);
            }
        }

        public void ClearBuffer(SocketAsyncEventArgs args)
        {
            int index;
            lock (_bufferLocker)
            {
                index = _buffers.IndexOf(args.Buffer);
            }

            if (index >= 0)
                _availableBuffers.Push(index);

            args.SetBuffer(null, 0, 0);
        }
    }
}
