using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace SevenZip
{
    interface INativeGarbageCollectable : IDisposable
    {
        IntPtr ThisPointer { get; }
    }
    internal class NativeGC
    {
        private static Dictionary<IntPtr, INativeGarbageCollectable> _objects = new Dictionary<IntPtr, INativeGarbageCollectable>();

        public static void Add(INativeGarbageCollectable collectable)
        {
            _objects.Add(collectable.ThisPointer, collectable);
        }
    }
    internal class OutArchiveWrapper : IOutArchive
    {
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int UpdateItemsDelegate(IntPtr thisPointer, IntPtr outStream, uint numItems, IntPtr updateCallback);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void GetFileTimeTypeDelegate(IntPtr thisPointer, IntPtr type);
        private static readonly UpdateItemsDelegate UpdateItemsImplementation;
        private static readonly GetFileTimeTypeDelegate GetFileTimeTypeImplementation;
        

        static OutArchiveWrapper()
        {
            UpdateItemsImplementation =
                Marshal.GetDelegateForFunctionPointer<UpdateItemsDelegate>(
                    SevenZipLibraryManager.Instance.GetProcAddress("OutArchiveWrapper_UpdateItems"));

            GetFileTimeTypeImplementation =
                Marshal.GetDelegateForFunctionPointer<GetFileTimeTypeDelegate>(
                    SevenZipLibraryManager.Instance.GetProcAddress("OutArchiveWrapper_GetFileTimeType"));
        }

        private readonly IntPtr _thisPointer;

        public OutArchiveWrapper(IntPtr thisPointer)
        {
            _thisPointer = thisPointer;
        }

        public int UpdateItems(ISequentialOutStream outStream, uint numItems, IArchiveUpdateCallback updateCallback)
        {
            var outStreamPointer = IntPtr.Zero;
            var updateCallbackPointer = IntPtr.Zero;

            return UpdateItemsImplementation(_thisPointer, outStreamPointer, numItems, updateCallbackPointer);
        }

        public void GetFileTimeType(IntPtr type)
        {
            GetFileTimeTypeImplementation(_thisPointer, type);
        }
    }
    
    internal class InArchiveWrapper : IInArchive
    {
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int OpenDelegate(IntPtr thisPointer, IntPtr stream, ref ulong maxCheckStartPosition, IntPtr openArchiveCallback);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void CloseDelegate(IntPtr thisPointer);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate uint GetNumberOfDelegate(IntPtr thisPointer, out uint value);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void GetPropertyDelegate(IntPtr thisPointer, uint index, ItemPropId propId, ref PropVariant value);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int ExtractDelegate(IntPtr thisPointer, IntPtr indexes, uint numItems, int testMode, IntPtr extractCallback);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void GetArchivePropertyDelegate(IntPtr thisPointer, ItemPropId propId, ref PropVariant value);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void GetPropertyInfoDelegate(IntPtr thisPointer, uint index, out string name, out ItemPropId propId, out ushort varType);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void GetArchivePropertyInfoDelegate(IntPtr thisPointer, uint index, out string name, out ItemPropId propId, out ushort varType);

        private static readonly OpenDelegate OpenImplementation;
        private static readonly CloseDelegate CloseImplementation;
        private static readonly GetNumberOfDelegate GetNumberOfItemsImplementation;
        private static readonly GetPropertyDelegate GetPropertyImplementation;
        private static readonly ExtractDelegate ExtractImplementation;
        private static readonly GetArchivePropertyDelegate GetArchivePropertyImplementation;
        private static readonly GetNumberOfDelegate GetNumberOfPropertiesImplementation;
        private static readonly GetPropertyInfoDelegate GetPropertyInfoImplementation;
        private static readonly GetNumberOfDelegate GetNumberOfArchivePropertiesImplementation;
        private static readonly GetArchivePropertyInfoDelegate GetArchivePropertyInfoImplementation;
        
        

        static InArchiveWrapper()
        {
            OpenImplementation = Marshal.GetDelegateForFunctionPointer<OpenDelegate>(SevenZipLibraryManager.Instance.GetProcAddress("IInArchive_Open"));
            CloseImplementation = Marshal.GetDelegateForFunctionPointer<CloseDelegate>(SevenZipLibraryManager.Instance.GetProcAddress("IInArchive_Close"));
            GetNumberOfItemsImplementation = Marshal.GetDelegateForFunctionPointer<GetNumberOfDelegate>(SevenZipLibraryManager.Instance.GetProcAddress("IInArchive_GetNumberOfItems"));
            GetPropertyImplementation = Marshal.GetDelegateForFunctionPointer<GetPropertyDelegate>(SevenZipLibraryManager.Instance.GetProcAddress("IInArchive_GetProperty"));
            ExtractImplementation = Marshal.GetDelegateForFunctionPointer<ExtractDelegate>(SevenZipLibraryManager.Instance.GetProcAddress("IInArchive_Extract"));
            GetArchivePropertyImplementation = Marshal.GetDelegateForFunctionPointer<GetArchivePropertyDelegate>(SevenZipLibraryManager.Instance.GetProcAddress("IInArchive_GetArchiveProperty"));
            GetNumberOfPropertiesImplementation = Marshal.GetDelegateForFunctionPointer<GetNumberOfDelegate>(SevenZipLibraryManager.Instance.GetProcAddress("IInArchive_GetNumberOfProperties"));
            GetPropertyInfoImplementation = Marshal.GetDelegateForFunctionPointer<GetPropertyInfoDelegate>(SevenZipLibraryManager.Instance.GetProcAddress("IInArchive_GetPropertyInfo"));
            GetNumberOfArchivePropertiesImplementation = Marshal.GetDelegateForFunctionPointer<GetNumberOfDelegate>(SevenZipLibraryManager.Instance.GetProcAddress("IInArchive_GetNumberOfArchiveProperties"));
            GetArchivePropertyInfoImplementation = Marshal.GetDelegateForFunctionPointer<GetArchivePropertyInfoDelegate>(SevenZipLibraryManager.Instance.GetProcAddress("IInArchive_GetArchivePropertyInfo"));
        }

        private readonly IntPtr _thisPointer;

        public InArchiveWrapper(IntPtr thisPointer)
        {
            Console.WriteLine($"InArchiveWrapper: {thisPointer.ToInt64():x8}");
            _thisPointer = thisPointer;
        }

        public int Open(IInStream stream, ref ulong maxCheckStartPosition, IArchiveOpenCallback openArchiveCallback)
        {
            var streamWrapped = new CInStream(stream);
            var openArchiveCallbackWrapped = new CArchiveOpenCallback(openArchiveCallback);
            return OpenImplementation(_thisPointer, streamWrapped.ThisPointer, ref maxCheckStartPosition,
                openArchiveCallbackWrapped.ThisPointer);
        }

        public void Close()
        {
            // TODO free native stream
            CloseImplementation(_thisPointer);
        }

        public uint GetNumberOfItems()
        {
            GetNumberOfItemsImplementation(_thisPointer, out var res);
            return res;
        }

        public void GetProperty(uint index, ItemPropId propId, ref PropVariant value)
        {
            GetPropertyImplementation(_thisPointer, index, propId, ref value);
        }

        public int Extract(uint[] indexes, uint numItems, int testMode, IArchiveExtractCallback extractCallback)
        {
            var gcHandle = GCHandle.Alloc(indexes, GCHandleType.Pinned);
            IntPtr extractCallbackObject = new CArchiveExtractCallback(extractCallback).ThisPointer;
            try
            {
                return ExtractImplementation(_thisPointer, gcHandle.AddrOfPinnedObject(), numItems, testMode,
                    extractCallbackObject);
            }
            finally
            {
                gcHandle.Free();
            }
        }

        public void GetArchiveProperty(ItemPropId propId, ref PropVariant value)
        {
            GetArchivePropertyImplementation(_thisPointer, propId, ref value);
        }

        public uint GetNumberOfProperties()
        {
            GetNumberOfPropertiesImplementation(_thisPointer, out var res);
            return res;
        }

        public void GetPropertyInfo(uint index, out string name, out ItemPropId propId, out ushort varType)
        {
            GetPropertyInfoImplementation(_thisPointer, index, out name, out propId, out varType);
        }

        public uint GetNumberOfArchiveProperties()
        {
            GetNumberOfArchivePropertiesImplementation(_thisPointer, out var res);
            return res;
        }

        public void GetArchivePropertyInfo(uint index, out string name, out ItemPropId propId, out ushort varType)
        {
            GetArchivePropertyInfoImplementation(_thisPointer, index, out name, out propId, out varType);
        }
    }

    internal class CProgress : INativeGarbageCollectable
    {
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int SetCompletedDelegate(IntPtr thisPointer, ref ulong completeValue);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int SetTotalDelegate(IntPtr thisPointer, ulong total);
        
        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public IntPtr ThisPointer { get; }
    }
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void ReleaseWrapperCallback(IntPtr thisPointer);

    internal class CArchiveExtractCallback : INativeGarbageCollectable
    {
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int SetOperationResultDelegate(IntPtr thisPointer, int opRes);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int PrepareOperationDelegate(IntPtr thisPointer, int askExtractMode);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int GetStreamDelegate(IntPtr thisPointer, uint index, IntPtr outStream, int askExtractMode);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr CreateIArchiveExtractCallbackCallback(IntPtr setTotal,
            IntPtr setCompleted,
            IntPtr setOperationResult,
            IntPtr prepareOperation,
            IntPtr getStream);

        private static readonly CreateIArchiveExtractCallbackCallback CreateIArchiveExtractCallback;
        private static readonly ReleaseWrapperCallback ReleaseIArchiveExtractCallback;
        
        static CArchiveExtractCallback()
        {
            CreateIArchiveExtractCallback = Marshal.GetDelegateForFunctionPointer<CreateIArchiveExtractCallbackCallback>(SevenZipLibraryManager.Instance.GetProcAddress("CreateIArchiveExtractCallback"));
            ReleaseIArchiveExtractCallback = Marshal.GetDelegateForFunctionPointer<ReleaseWrapperCallback>(SevenZipLibraryManager.Instance.GetProcAddress("ReleaseIArchiveExtractCallback"));
        }

        private readonly CProgress.SetTotalDelegate _setTotal;
        private readonly CProgress.SetCompletedDelegate _setCompleted;
        private readonly SetOperationResultDelegate _setOperationResult;
        private readonly PrepareOperationDelegate _prepareOperation;
        private readonly GetStreamDelegate _getStream;

        public IntPtr ThisPointer { get; }
        
        
        public CArchiveExtractCallback(IArchiveExtractCallback callback)
        {
            _setTotal = (pointer, total) =>
                        {
                            callback.SetTotal(total);
                            return 0;
                        };
            _setCompleted = (IntPtr pointer, ref ulong value) =>
                            {
                                callback.SetCompleted(ref value);
                                return 0;
                            };
            _setOperationResult = (pointer, res) =>
                                  {
                                      callback.SetOperationResult((OperationResult)res);
                                      return 0;
                                  };
            _prepareOperation = (pointer, mode) =>
                                {
                                    callback.PrepareOperation((AskMode)mode);
                                    return 0;
                                };
            _getStream = (pointer, index, stream, mode) =>
                         {
                             var res = callback.GetStream(index, out var actualStream, (AskMode)mode);
                             var getStreamPtr = new CSequentialOutStream(actualStream).ThisPointer;
                             Console.WriteLine($"GetStreamPtr: {getStreamPtr.ToInt64():x8}");
                             Marshal.WriteIntPtr(stream, getStreamPtr); // TODO
                             return res;
                         };

            var pointers = new[]
                           {
                               Marshal.GetFunctionPointerForDelegate(_setTotal),
                               Marshal.GetFunctionPointerForDelegate(_setCompleted),
                               Marshal.GetFunctionPointerForDelegate(_setOperationResult),
                               Marshal.GetFunctionPointerForDelegate(_prepareOperation),
                               Marshal.GetFunctionPointerForDelegate(_getStream)
                           };

            Console.WriteLine($"SetTotal:{pointers[0].ToInt64():x8}\nSetCompleted:{pointers[1].ToInt64():x8}\nSetOperationResult:{pointers[2].ToInt64():x8}\nPrepareOperation:{pointers[3].ToInt64():x8}\nGetStream:{pointers[4].ToInt64():x8}\n");

            ThisPointer = CreateIArchiveExtractCallback(pointers[0], pointers[1], pointers[2], pointers[3], pointers[4]);
            Console.WriteLine($"CArchiveExtractCallback this: {ThisPointer.ToInt64():x8}");

            NativeGC.Add(this);
        }
        public void Dispose()
        {
            ReleaseIArchiveExtractCallback(ThisPointer);
        }
    }

    internal class CSequentialOutStream : INativeGarbageCollectable
    {
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int WriteDelegate(IntPtr thisPointer, IntPtr data, uint size, IntPtr processedSize);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr CreateISequentialOutStreamDelegate(IntPtr write);
        private static readonly CreateISequentialOutStreamDelegate CreateISequentialOutStream;
        private static readonly ReleaseWrapperCallback ReleaseISequentialOutStream;
        
        static CSequentialOutStream()
        {
            CreateISequentialOutStream = Marshal.GetDelegateForFunctionPointer<CreateISequentialOutStreamDelegate>(SevenZipLibraryManager.Instance.GetProcAddress("CreateISequentialOutStream"));
            ReleaseISequentialOutStream = Marshal.GetDelegateForFunctionPointer<ReleaseWrapperCallback>(SevenZipLibraryManager.Instance.GetProcAddress("ReleaseISequentialOutStream"));
        }

        private readonly WriteDelegate _write;
        
        public IntPtr ThisPointer { get; }
        public CSequentialOutStream(ISequentialOutStream outStream)
        {
            _write = (pointer, data, size, processedSize) =>
                     {
                         var dataArray = new byte[size];
                         Marshal.Copy(data, dataArray, 0, (int)size);
                         return outStream.Write(dataArray, size, processedSize);
                     };
            var writePtr = Marshal.GetFunctionPointerForDelegate(_write);
            Console.WriteLine($"CSequentialOutStream Write: {writePtr.ToInt64():x8}");
            ThisPointer = CreateISequentialOutStream(writePtr);
            
            Console.WriteLine($"CSequentialOutStream this: {ThisPointer.ToInt64():x8}");

            NativeGC.Add(this);
        }
        public void Dispose()
        {
            ReleaseISequentialOutStream(ThisPointer);
        }
    }
    internal class CSequentialInStream : INativeGarbageCollectable
    {
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int ReadDelegate(IntPtr thisPointer, IntPtr data, uint size, IntPtr processedSize);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr CreateISequentialInStreamDelegate(IntPtr read);
        private static CreateISequentialInStreamDelegate CreateISequentialInStream;
        private static ReleaseWrapperCallback ReleaseISequentialInStream;
        
        static CSequentialInStream()
        {
            CreateISequentialInStream = Marshal.GetDelegateForFunctionPointer<CreateISequentialInStreamDelegate>(SevenZipLibraryManager.Instance.GetProcAddress("CreateISequentialInStream"));
            ReleaseISequentialInStream = Marshal.GetDelegateForFunctionPointer<ReleaseWrapperCallback>(SevenZipLibraryManager.Instance.GetProcAddress("ReleaseISequentialInStream"));
        }

        private readonly ReadDelegate _read;
        
        public IntPtr ThisPointer { get; }
        public CSequentialInStream(ISequentialInStream inStream)
        {
            _read = (pointer, data, size, processedSize) =>
                     {
                         var dataArray = new byte[size];
                         var res = inStream.Read(dataArray, size);
                         Marshal.Copy(dataArray, 0, data, res);
                         return res;
                     };
            ThisPointer = CreateISequentialInStream(Marshal.GetFunctionPointerForDelegate(_read));

            NativeGC.Add(this);
        }
        public void Dispose()
        {
            ReleaseISequentialInStream(ThisPointer);
        }
    }

    internal class CInStream : INativeGarbageCollectable
    {
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int SeekDelegate(IntPtr thisPointer, long offset, uint seekOrigin, IntPtr newPosition);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr CreateIInStreamDelegate(IntPtr read, IntPtr seek);
        private static readonly CreateIInStreamDelegate CreateIInStream;
        private static readonly ReleaseWrapperCallback ReleaseIInStream;
        
        static CInStream()
        {
            CreateIInStream = Marshal.GetDelegateForFunctionPointer<CreateIInStreamDelegate>(SevenZipLibraryManager.Instance.GetProcAddress("CreateIInStream"));
            ReleaseIInStream = Marshal.GetDelegateForFunctionPointer<ReleaseWrapperCallback>(SevenZipLibraryManager.Instance.GetProcAddress("ReleaseIInStream"));
        }

        private readonly CSequentialInStream.ReadDelegate _read;
        private readonly SeekDelegate _seek;
        
        public IntPtr ThisPointer { get; }

        public CInStream(IInStream inStream)
        {
            _read = (pointer, data, size, processedSize) =>
                    {
                        var dataArray = new byte[size];
                        var res = inStream.Read(dataArray, size);
                        Marshal.Copy(dataArray, 0, data, res);
                        Marshal.WriteInt32(processedSize, res);
                        return 0;
                    };
            _seek = (pointer, offset, origin, position) =>
                    {
                        inStream.Seek(offset, (SeekOrigin)origin, position);
                        return 0;
                    };
            var pointers = new[]
                           {
                               Marshal.GetFunctionPointerForDelegate(_read),
                               Marshal.GetFunctionPointerForDelegate(_seek)
                           };
            Console.WriteLine($"Read:{pointers[0].ToInt64():x8}\nSeek:{pointers[1].ToInt64():x8}");

            ThisPointer = CreateIInStream(pointers[0], pointers[1]);
            Console.WriteLine($"CInStream this: {ThisPointer.ToInt64():x8}");

            NativeGC.Add(this);
        }
        public void Dispose()
        {
            
            ReleaseIInStream(ThisPointer);
        }
    }

    class CArchiveOpenCallback : INativeGarbageCollectable
    {
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int SetDelegate(IntPtr thisPointer, IntPtr files, IntPtr bytes);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr CreateIArchiveOpenCallbackDelegate(IntPtr setTotal, IntPtr setCompleted);
        private static readonly CreateIArchiveOpenCallbackDelegate CreateIArchiveOpenCallback;
        private static readonly ReleaseWrapperCallback ReleaseIArchiveOpenCallback;
        
        static CArchiveOpenCallback()
        {
            CreateIArchiveOpenCallback = Marshal.GetDelegateForFunctionPointer<CreateIArchiveOpenCallbackDelegate>(SevenZipLibraryManager.Instance.GetProcAddress("CreateIArchiveOpenCallback"));
            ReleaseIArchiveOpenCallback = Marshal.GetDelegateForFunctionPointer<ReleaseWrapperCallback>(SevenZipLibraryManager.Instance.GetProcAddress("ReleaseIArchiveOpenCallback"));
        }

        private readonly SetDelegate _setTotal;
        private readonly SetDelegate _setCompleted;
        
        public IntPtr ThisPointer { get; }

        public CArchiveOpenCallback(IArchiveOpenCallback openCallback)
        {
            _setTotal = (pointer, files, bytes) =>
                        {
                            openCallback.SetTotal(files, bytes);
                            return 0;
                        };
            _setCompleted = (pointer, files, bytes) =>
                        {
                            openCallback.SetCompleted(files, bytes);
                            return 0;
                        };
            var pointers = new[]
                           {
                               Marshal.GetFunctionPointerForDelegate(_setTotal),
                               Marshal.GetFunctionPointerForDelegate(_setCompleted)
                           };
            Console.WriteLine($"SetTotal:{pointers[0].ToInt64():x8}\nSetCompleted:{pointers[1].ToInt64():x8}");

            ThisPointer = CreateIArchiveOpenCallback(pointers[0], pointers[1]);

            Console.WriteLine($"CArchiveOpenCallback this: {ThisPointer.ToInt64():x8}");

            NativeGC.Add(this);
        }
        public void Dispose()
        {
            ReleaseIArchiveOpenCallback(ThisPointer);
        }
    }
}