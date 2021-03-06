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
using System.IO;

namespace NYurik.TimeSeriesDb
{
    /// <summary>
    /// Methods to support binary data serialization into either a FileStream or into memory.
    /// Must not be implemented directly. Implement <see cref="IBinSerializer{T}"/> instead.
    /// </summary>
    public interface IBinSerializer
    {
        /// <summary> Serializer version </summary>
        Version Version { get; }

        /// <summary> The size of each data element </summary>
        int TypeSize { get; }

        /// <summary>
        /// Get the type of items this serializer will process
        /// </summary>
        Type ItemType { get; }

        /// <summary>
        /// Will be set to true when this provider supports reading and writing to native memory (e.g. memory mapped file)
        /// </summary>
        bool SupportsMemoryPtrOperations { get; }

        /// <summary>
        /// Save this serializer's parameters to a binary writer. Must match all actions by <see cref="InitExisting"/>
        /// </summary>
        void InitNew(BinaryWriter writer);

        /// <summary>
        /// When creating serializer from a stream, load internal values. Must match all actions by <see cref="InitNew"/>
        /// </summary>
        void InitExisting(BinaryReader reader, Func<string, Type> typeResolver);
    }

    /// <summary>
    /// Methods to support binary data serialization into either a FileStream or into memory.
    /// </summary>
    public interface IBinSerializer<T> : IBinSerializer
    {
        /// <summary>
        /// Read or write all items from/to <paramref name="fileStream"/> 
        /// into/from <paramref name="buffer"/>.
        /// </summary>
        /// <param name="fileStream">FileStream already positioned at the point in the file from which to begin</param>
        /// <param name="buffer">Buffer to fill with/read from values</param>
        /// <param name="isWriting">True when buffer should be written into a file, false - when reading into the buffer</param>
        int ProcessFileStream(FileStream fileStream, ArraySegment<T> buffer, bool isWriting);

        /// <summary>
        /// Read/Write items from/to the memory pointer <paramref name="memPointer"/> into/from <paramref name="buffer"/>
        /// </summary>
        /// <param name="memPointer">A pointer to unmanaged memory (e.g. mapped to the file)</param>
        /// <param name="buffer">Buffer to fill with/read from values</param>
        /// <param name="isWriting">True when buffer should be written into a file, false - when reading into the buffer</param>
        void ProcessMemoryPtr(IntPtr memPointer, ArraySegment<T> buffer, bool isWriting);

        /// <summary>
        /// Compare the elements of <paramref name="buffer1"/> with <paramref name="buffer2"/>, and returns true if they are the same
        /// </summary>
        bool BinaryArrayCompare(ArraySegment<T> buffer1, ArraySegment<T> buffer2);
    }
}