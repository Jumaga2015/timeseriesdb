#region COPYRIGHT

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
using System.Linq;

namespace NYurik.TimeSeriesDb
{
    public static class ObsoleteExtensions
    {
        /// <summary>
        /// Read up to <paramref name="count"/> items beging at <paramref name="firstItemIdx"/>, and return an <see cref="Array"/> object. 
        /// </summary>
        /// <param name="file"/>
        /// <param name="firstItemIdx">Index of the item to start from.</param>
        /// <param name="count">The maximum number of items to read.</param>
        [Obsolete("Use streaming methods instead")]
        public static Array GenericReadData(this IBinaryFile file, long firstItemIdx, int count)
        {
            return file.RunGenericMethod(new Generic(), Tuple.Create(firstItemIdx, count));
        }

        /// <summary>
        /// Read data starting at <paramref name="firstItemIdx"/> to fill up the <paramref name="buffer"/>.
        /// </summary>
        [Obsolete("Use streaming methods instead")]
        public static void ReadData<TInd, TVal>(
            BinSeriesFile<TInd, TVal> file, long firstItemIdx, ArraySegment<TVal> buffer)
            where TInd : IComparable<TInd>
        {
            ReadData<TVal>(file, firstItemIdx, buffer);
        }

        /// <summary>
        /// Read enough items to fill the <paramref name="buffer"/>, starting at <paramref name="firstItemIndex"/>.
        /// </summary>
        /// <param name="file"></param>
        /// <param name="firstItemIndex">Index of the item to start from.</param>
        /// <param name="buffer">Array of values to be written into a file.</param>
        [Obsolete("Use streaming methods instead")]
        public static void ReadData<T>(this BinaryFile<T> file, long firstItemIndex, ArraySegment<T> buffer)
        {
            int done = buffer.Offset;
            foreach (var seg in file.PerformStreaming(firstItemIndex, false, maxItemCount: buffer.Count))
            {
                if (done - buffer.Offset + seg.Count > buffer.Count)
                    throw new InvalidOperationException(
                        "Internal logic error: more than maxItemCount elements received");
                Array.Copy(seg.Array, 0, buffer.Array, done, seg.Count);
                done += seg.Count;
            }
        }

        /// <summary>
        /// Write an array of items to the file.
        /// </summary>
        /// <param name="file"></param>
        /// <param name="firstItemIndex">The index of the first value in the <paramref name="buffer"/> array.</param>
        /// <param name="buffer">Array of values to be written into a file.</param>
        [Obsolete("Use streaming methods instead")]
        public static void WriteData<T>(this BinIndexedFile<T> file, long firstItemIndex, ArraySegment<T> buffer)
        {
            file.WriteStream(new[] {buffer}, firstItemIndex);
        }

        /// <summary>
        /// Add new items at the end of the existing file
        /// </summary>
        [Obsolete("Use overloaded method")]
        public static void AppendData<TInd, TVal>(this BinSeriesFile<TInd, TVal> file, ArraySegment<TVal> buffer)
            where TInd : struct, IComparable<TInd>
        {
            file.AppendData(new[] {buffer});
        }

        /// <summary>
        /// Read data starting at <paramref name="fromInclusive"/>, up to, 
        /// but not including <paramref name="toExclusive"/>.
        /// </summary>
        /// <returns>An array of items no bigger than <paramref name="maxItemCount"/></returns>
        [Obsolete("Use streaming methods instead")]
        public static TVal[] ReadData<TVal, TInd>(
            this BinSeriesFile<TInd, TVal> file, TInd fromInclusive,
            TInd toExclusive, int maxItemCount)
            where TInd : struct, IComparable, IComparable<TInd>
        {
            return file.Stream(fromInclusive, toExclusive, maxItemCount: maxItemCount).ToArray();
        }

        /// <summary>
        /// Read all available data begining at a given index
        /// </summary>
        [Obsolete("Use streaming methods instead")]
        public static TVal[] ReadDataToEnd<TVal, TInd>(this BinSeriesFile<TInd, TVal> binSeriesFile, TInd fromInclusive)
            where TInd : struct, IComparable, IComparable<TInd>
        {
            return binSeriesFile.Stream(fromInclusive).ToArray();
        }

        /// <summary>
        /// Read all available data begining at a given index
        /// </summary>
        [Obsolete("Use streaming methods instead")]
        public static TVal[] ReadDataToEnd<TVal, TInd>(this BinSeriesFile<TInd, TVal> binSeriesFile, long firstItemIdx)
            where TInd : struct, IComparable, IComparable<TInd>
        {
            return binSeriesFile.PerformStreaming(firstItemIdx, false).Stream().ToArray();
        }

        #region Nested type: Generic

        private class Generic : IGenericCallable<Array, Tuple<long, int>>
        {
            #region IGenericCallable<Array,Tuple<long,int>> Members

            [Obsolete]
            public Array Run<T>(IGenericInvoker source, Tuple<long, int> arg)
            {
                long firstItemIdx = arg.Item1;
                int count = arg.Item2;

                var file = ((BinaryFile<T>) source);

                long fileCount = file.Count;
                if (firstItemIdx < 0 || firstItemIdx > fileCount)
                    throw new ArgumentOutOfRangeException(
                        "firstItemIdx" + "", firstItemIdx, string.Format("Accepted range [0:{0}]", fileCount));
                if (count < 0)
                    throw new ArgumentOutOfRangeException("count" + "", count, "Must be non-negative");

                var result = new T[(int) Math.Min(fileCount - firstItemIdx, count)];

                file.PerformFileAccess(firstItemIdx, new ArraySegment<T>(result), false);

                return result;
            }

            #endregion
        }

        #endregion
    }
}