﻿#region COPYRIGHT

/*
 *     Copyright 2009-2012 Yuri Astrakhan  (<Firstname><Lastname>@gmail.com)
 *
 *     This file is part of TimeSeriesDb library
 * 
 *  TimeSeriesDb is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 * 
 *  TimeSeriesDb is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 * 
 *  You should have received a copy of the GNU General Public License
 *  along with TimeSeriesDb.  If not, see <http://www.gnu.org/licenses/>.
 *
 */

#endregion

using System;
using System.Collections.Generic;
using NYurik.TimeSeriesDb.Common;

namespace NYurik.TimeSeriesDb
{
    public static class EnumerableFeedExtensions
    {
        public static IEnumerable<TVal> Stream<TInd, TVal>(
            this IEnumerableFeed<TInd, TVal> feed,
            TInd fromInd = default(TInd), TInd untilInd = default(TInd), bool inReverse = false,
            IEnumerable<Buffer<TVal>> bufferProvider = null, long maxItemCount = long.MaxValue)
            where TInd : IComparable<TInd>
        {
            return feed.StreamSegments(fromInd, untilInd, inReverse, bufferProvider, maxItemCount).Stream();
        }

        public static IEnumerable<TVal> Stream<TInd, TVal>(
            Func<IEnumerableFeed<TInd, TVal>> feedFactory,
            TInd fromInd = default(TInd), TInd untilInd = default(TInd), bool inReverse = false,
            IEnumerable<Buffer<TVal>> bufferProvider = null, long maxItemCount = long.MaxValue,
            Action<IEnumerableFeed<TInd, TVal>> onDispose = null)
            where TInd : IComparable<TInd>
        {
            if (feedFactory == null)
                throw new ArgumentNullException("feedFactory");

            IEnumerableFeed<TInd, TVal> feed = feedFactory();
            try
            {
                foreach (var segm in feed.StreamSegments(fromInd, untilInd, inReverse, bufferProvider, maxItemCount))
                {
                    for (int i = segm.Offset; i < segm.Offset + segm.Count; i++)
                        yield return segm.Array[i];
                }
            }
            finally
            {
                if (onDispose != null)
                    onDispose(feed);
            }
        }

        public static IEnumerable<ArraySegment<TVal>> StreamSegments<TInd, TVal>(
            this IEnumerableFeed<TInd, TVal> feed,
            TInd fromInd = default(TInd), TInd untilInd = default(TInd), bool inReverse = false,
            IEnumerable<Buffer<TVal>> bufferProvider = null, long maxItemCount = long.MaxValue)
            where TInd : IComparable<TInd>
        {
            if (feed == null)
                throw new ArgumentNullException("feed");

            return Utils.IsDefault(untilInd)
                       ? feed.StreamSegments(fromInd, inReverse, bufferProvider, maxItemCount)
                       : StreamSegmentsUntil(feed, fromInd, untilInd, inReverse, bufferProvider, maxItemCount);
        }

        private static IEnumerable<ArraySegment<TVal>> StreamSegmentsUntil<TInd, TVal>(
            IEnumerableFeed<TInd, TVal> feed,
            TInd fromInd, TInd untilInd, bool inReverse,
            IEnumerable<Buffer<TVal>> bufferProvider, long maxItemCount)
            where TInd : IComparable<TInd>
        {
            Func<TVal, TInd> tsa = feed.IndexAccessor;

            foreach (var segm in feed.StreamSegments(fromInd, inReverse, bufferProvider, maxItemCount))
            {
                if (segm.Count == 0)
                    continue;

                int comp = tsa(segm.Array[segm.Offset + segm.Count - 1]).CompareTo(untilInd);
                if (inReverse ? comp >= 0 : comp < 0)
                {
                    yield return segm;
                    continue;
                }

                var pos = (int)
                          Utils.BinarySearch(
                              untilInd, segm.Offset, segm.Count, false, inReverse, i => tsa(segm.Array[i]));

                if (pos < 0)
                    pos = ~pos;
                else if (inReverse)
                    pos++;

                if (pos > segm.Offset)
                    yield return new ArraySegment<TVal>(segm.Array, segm.Offset, pos - segm.Offset);

                yield break;
            }
        }

        public static IEnumerable<T> Stream<T>(this ArraySegment<T> arraySegment)
        {
            if (arraySegment.Count > 0)
            {
                int max = arraySegment.Offset + arraySegment.Count;
                for (int i = arraySegment.Offset; i < max; i++)
                    yield return arraySegment.Array[i];
            }
        }

        public static IEnumerable<T> Stream<T>(this IEnumerable<ArraySegment<T>> stream)
        {
            if (stream == null) throw new ArgumentNullException("stream");

            foreach (var v in stream)
                if (v.Count > 0)
                {
                    int max = v.Offset + v.Count;
                    for (int i = v.Offset; i < max; i++)
                        yield return v.Array[i];
                }
        }
    }
}