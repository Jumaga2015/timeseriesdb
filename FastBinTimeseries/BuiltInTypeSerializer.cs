using System;
using System.IO;
using System.Reflection.Emit;

namespace NYurik.FastBinTimeseries
{
    internal delegate void UnsafeActionDelegate<TStorage, TItem>(
        //BuiltInTypeSerializer<TItem> thisInst, 
    TStorage storage, TItem[] buffer, int offset, int count, bool isWriting);

    internal class BuiltInTypeSerializer<T> : IBinSerializer<T>
    {
        private static readonly unsafe bool is64bit = sizeof (void*) == sizeof (long);
        private readonly int _typeSize;

        public BuiltInTypeSerializer()
        {
            var info = DynamicCodeFactory.Instance.CreateSerializer<T>();

            if (info.ItemSize <= 0)
                throw new InvalidOperationException("Struct size must be > 0");

            _typeSize = info.ItemSize;
            processFileStream = (UnsafeActionDelegate<FileStream, T>)
                                info.FileStreamMethod.CreateDelegate(
                                    typeof (UnsafeActionDelegate<,>).MakeGenericType(typeof (FileStream), typeof (T)),
                                    this);
            processMemoryMap = (UnsafeActionDelegate<IntPtr, T>)
                               info.MemMapMethod.CreateDelegate(
                                   typeof (UnsafeActionDelegate<,>).MakeGenericType(typeof (IntPtr), typeof (T)),
                                   this);
        }

        #region IBinSerializer<T> Members

        public int TypeSize
        {
            get { return _typeSize; }
        }

        public bool SupportsMemoryMappedFiles
        {
            get { return true; }
        }

        public int PageSize
        {
            get { return 0; }
        }

        private readonly UnsafeActionDelegate<FileStream,T> processFileStream;
        private readonly UnsafeActionDelegate<IntPtr, T> processMemoryMap;

        public void ProcessFileStream(FileStream fileStream, T[] buffer, int offset, int count, bool isWriting)
        {
            processFileStream(fileStream, buffer, offset, count, isWriting);
        }

        public void ProcessMemoryMap(IntPtr memMapPtr, T[] buffer, int offset, int count, bool isWriting)
        {
            processMemoryMap(memMapPtr, buffer, offset, count, isWriting);
        }

        public void ReadCustomHeader(BinaryReader reader)
        {
        }

        public void WriteCustomHeader(BinaryWriter writer)
        {
        }

        #endregion

        protected unsafe void ProcessFileStreamPtr(FileStream fileStream, void* bufPtr, int offset, int count,
                                                   bool isWriting)
        {
            var byteBufPtr = (byte*) bufPtr + offset*_typeSize;
            var byteCount = count*_typeSize;

            var bytesProcessed = isWriting
                                     ? Win32Apis.WriteFile(fileStream, byteBufPtr, byteCount)
                                     : Win32Apis.ReadFile(fileStream, byteBufPtr, byteCount);

            if (bytesProcessed != byteCount)
                throw new IOException(
                    String.Format("Unable to {0} {1} bytes - only {2} bytes were available",
                                  isWriting ? "write" : "read", byteCount, bytesProcessed));
        }

        protected unsafe void ProcessMemoryMapPtr(IntPtr memMapPtr, void* bufPtr, int offset, int count, bool isWriting)
        {
            var byteBufPtr = (byte*) bufPtr + offset*_typeSize;
            var byteCount = count*_typeSize;

            var src = isWriting ? byteBufPtr : (byte*) memMapPtr;
            var dest = isWriting ? (byte*) memMapPtr : byteBufPtr;

            CopyMemory(dest, src, (uint) byteCount);
        }

        /// <summary>
        /// Fast memory copying - copies in blocks of 32 bytes, using either int or long (on 64bit machines)
        /// Calling the native RtlMemoryMove was slower
        /// </summary>
        public static unsafe void CopyMemory(byte* pDestination, byte* pSource, uint byteCount)
        {
            const int blockSize = 32;
            if (byteCount >= blockSize)
            {
                if (is64bit)
                {
                    do
                    {
                        ((long*) pDestination)[0] = ((long*) pSource)[0];
                        ((long*) pDestination)[1] = ((long*) pSource)[1];
                        ((long*) pDestination)[2] = ((long*) pSource)[2];
                        ((long*) pDestination)[3] = ((long*) pSource)[3];
                        pDestination += blockSize;
                        pSource += blockSize;
                        byteCount -= blockSize;
                    } while (byteCount >= blockSize);
                }
                else
                {
                    do
                    {
                        ((int*) pDestination)[0] = ((int*) pSource)[0];
                        ((int*) pDestination)[1] = ((int*) pSource)[1];
                        ((int*) pDestination)[2] = ((int*) pSource)[2];
                        ((int*) pDestination)[3] = ((int*) pSource)[3];
                        ((int*) pDestination)[4] = ((int*) pSource)[4];
                        ((int*) pDestination)[5] = ((int*) pSource)[5];
                        ((int*) pDestination)[6] = ((int*) pSource)[6];
                        ((int*) pDestination)[7] = ((int*) pSource)[7];
                        pDestination += blockSize;
                        pSource += blockSize;
                        byteCount -= blockSize;
                    } while (byteCount >= blockSize);
                }
            }

            while (byteCount > 0)
            {
                *(pDestination++) = *(pSource++);
                byteCount--;
            }
        }
    }
}