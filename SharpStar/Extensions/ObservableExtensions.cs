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
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpStar.Extensions
{
	public static class ObservableExtensions
	{
		public static IObservable<TResult> CombineWithPrevious<TSource, TResult>(
			this IObservable<TSource> source,
			Func<TSource, TSource, TResult> resultSelector)
		{
			return source.Scan(
				Tuple.Create(default(TSource), default(TSource)),
				(previous, current) => Tuple.Create(previous.Item2, current))
				.Select(t => resultSelector(t.Item1, t.Item2));
		}
	}
}
