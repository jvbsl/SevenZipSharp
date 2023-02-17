using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using SevenZip;

namespace SevenZip
{
    interface INativeGarbageCollectable : IDisposable
    {
        IntPtr ThisPointer { get; }
    }

    internal class NativeGC
    {
        private static Dictionary<IntPtr, INativeGarbageCollectable> _objects =
            new Dictionary<IntPtr, INativeGarbageCollectable>();

        public static void Add(INativeGarbageCollectable collectable)
        {
            _objects.Add(collectable.ThisPointer, collectable);
        }
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate IntPtr CastDelegate(IntPtr outArchive);

    internal class SetPropertiesWrapper : INativeGarbageCollectable, ISetProperties
    {
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int SetPropertiesDelegate(IntPtr thisPointer, IntPtr names, IntPtr values, int numProperties);

        private static SetPropertiesDelegate SetPropertiesImplementation;

        private static CastDelegate CastIOutArchive;
        private static CastDelegate CastIInArchive;

        public SetPropertiesWrapper(IntPtr thisPointer)
        {
            CacheMethods();
            ThisPointer = thisPointer;
        }

        private static IntPtr GetIntPtr(IOutArchive archive)
        {
            CacheMethods();
            if (!(archive is OutArchiveWrapper wrapper))
                throw new InvalidCastException();
            return CastIOutArchive(wrapper.ThisPointer);
        }

        private static IntPtr GetIntPtr(IInArchive archive)
        {
            CacheMethods();
            if (!(archive is InArchiveWrapper wrapper))
                throw new InvalidCastException();
            return CastIInArchive(wrapper.ThisPointer);
        }

        public SetPropertiesWrapper(IOutArchive archive)
            : this(GetIntPtr(archive))
        {
        }

        public SetPropertiesWrapper(IInArchive archive)
            : this(GetIntPtr(archive))
        {
        }


        static void CacheMethods()
        {
            SetPropertiesImplementation =
                Marshal.GetDelegateForFunctionPointer<SetPropertiesDelegate>(
                    SevenZipLibraryManager.Instance.GetProcAddress("ISetProperties_SetProperties"));

            CastIOutArchive = Marshal.GetDelegateForFunctionPointer<CastDelegate>(
                SevenZipLibraryManager.Instance.GetProcAddress("Cast_IOutArchive"));
            CastIInArchive = Marshal.GetDelegateForFunctionPointer<CastDelegate>(
                SevenZipLibraryManager.Instance.GetProcAddress("Cast_IInArchive"));
        }

        public int SetProperties(IntPtr names, IntPtr values, int numProperties)
        {
            return SetPropertiesImplementation(ThisPointer, names, values, numProperties);
        }

        public void Dispose()
        {
        }

        public IntPtr ThisPointer { get; }
    }

    internal class OutArchiveWrapper : IOutArchive
    {
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int UpdateItemsDelegate(IntPtr thisPointer, IntPtr outStream, uint numItems,
            IntPtr updateCallback);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void GetFileTimeTypeDelegate(IntPtr thisPointer, IntPtr type);

        private static UpdateItemsDelegate UpdateItemsImplementation;
        private static GetFileTimeTypeDelegate GetFileTimeTypeImplementation;

        private static CastDelegate CastIOutStream;
        private static CastDelegate CastIOutArchive;

        static void CacheMethods()
        {
            UpdateItemsImplementation =
                Marshal.GetDelegateForFunctionPointer<UpdateItemsDelegate>(
                    SevenZipLibraryManager.Instance.GetProcAddress("IOutArchive_UpdateItems"));
            CastIOutStream =
                Marshal.GetDelegateForFunctionPointer<CastDelegate>(
                    SevenZipLibraryManager.Instance.GetProcAddress("Cast_IOutStream"));
            CastIOutArchive =
                Marshal.GetDelegateForFunctionPointer<CastDelegate>(
                    SevenZipLibraryManager.Instance.GetProcAddress("Cast_IInArchive_IOutArchive"));

            GetFileTimeTypeImplementation =
                Marshal.GetDelegateForFunctionPointer<GetFileTimeTypeDelegate>(
                    SevenZipLibraryManager.Instance.GetProcAddress("IOutArchive_GetFileTimeType"));
        }

        public IntPtr ThisPointer { get; }

        private static IntPtr GetCasted(InArchiveWrapper wrapper)
        {
            if (wrapper is null)
                return IntPtr.Zero;
            CacheMethods();
            return CastIOutArchive(wrapper.ThisPointer);
        }

        public OutArchiveWrapper(InArchiveWrapper wrapper)
            : this(GetCasted(wrapper))
        {
        }

        public OutArchiveWrapper(IntPtr thisPointer)
        {
            CacheMethods();
            ThisPointer = thisPointer;
        }

        public int UpdateItems(ISequentialOutStream outStream, uint numItems, IArchiveUpdateCallback updateCallback)
        {
            var wrappedStream = (outStream is IOutStream outStreamFull)
                ? CastIOutStream(new COutStream(outStreamFull).ThisPointer)
                : new CSequentialOutStream(outStream).ThisPointer;

            var updateCallbackPointer = new CArchiveUpdateCallback(updateCallback).ThisPointer;

            return UpdateItemsImplementation(ThisPointer, wrappedStream, numItems, updateCallbackPointer);
        }

        public void GetFileTimeType(IntPtr type)
        {
            GetFileTimeTypeImplementation(ThisPointer, type);
        }
    }

    internal class InArchiveWrapper : IInArchive
    {
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int OpenDelegate(IntPtr thisPointer, IntPtr stream, ref ulong maxCheckStartPosition,
            IntPtr openArchiveCallback);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void CloseDelegate(IntPtr thisPointer);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate uint GetNumberOfDelegate(IntPtr thisPointer, out uint value);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void GetPropertyDelegate(IntPtr thisPointer, uint index, ItemPropId propId,
            ref PropVariant value);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int ExtractDelegate(IntPtr thisPointer, IntPtr indexes, uint numItems, int testMode,
            IntPtr extractCallback);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void GetArchivePropertyDelegate(IntPtr thisPointer, ItemPropId propId, ref PropVariant value);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void GetPropertyInfoDelegate(IntPtr thisPointer, uint index, out IntPtr name,
            out ItemPropId propId, out ushort varType);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void GetArchivePropertyInfoDelegate(IntPtr thisPointer, uint index, out IntPtr name,
            out ItemPropId propId, out ushort varType);

        private static OpenDelegate OpenImplementation;
        private static CloseDelegate CloseImplementation;
        private static GetNumberOfDelegate GetNumberOfItemsImplementation;
        private static GetPropertyDelegate GetPropertyImplementation;
        private static ExtractDelegate ExtractImplementation;
        private static GetArchivePropertyDelegate GetArchivePropertyImplementation;
        private static GetNumberOfDelegate GetNumberOfPropertiesImplementation;
        private static GetPropertyInfoDelegate GetPropertyInfoImplementation;
        private static GetNumberOfDelegate GetNumberOfArchivePropertiesImplementation;
        private static GetArchivePropertyInfoDelegate GetArchivePropertyInfoImplementation;


        static void CacheMethods()
        {
            OpenImplementation =
                Marshal.GetDelegateForFunctionPointer<OpenDelegate>(
                    SevenZipLibraryManager.Instance.GetProcAddress("IInArchive_Open"));
            CloseImplementation =
                Marshal.GetDelegateForFunctionPointer<CloseDelegate>(
                    SevenZipLibraryManager.Instance.GetProcAddress("IInArchive_Close"));
            GetNumberOfItemsImplementation =
                Marshal.GetDelegateForFunctionPointer<GetNumberOfDelegate>(
                    SevenZipLibraryManager.Instance.GetProcAddress("IInArchive_GetNumberOfItems"));
            GetPropertyImplementation =
                Marshal.GetDelegateForFunctionPointer<GetPropertyDelegate>(
                    SevenZipLibraryManager.Instance.GetProcAddress("IInArchive_GetProperty"));
            ExtractImplementation =
                Marshal.GetDelegateForFunctionPointer<ExtractDelegate>(
                    SevenZipLibraryManager.Instance.GetProcAddress("IInArchive_Extract"));
            GetArchivePropertyImplementation =
                Marshal.GetDelegateForFunctionPointer<GetArchivePropertyDelegate>(
                    SevenZipLibraryManager.Instance.GetProcAddress("IInArchive_GetArchiveProperty"));
            GetNumberOfPropertiesImplementation =
                Marshal.GetDelegateForFunctionPointer<GetNumberOfDelegate>(
                    SevenZipLibraryManager.Instance.GetProcAddress("IInArchive_GetNumberOfProperties"));
            GetPropertyInfoImplementation =
                Marshal.GetDelegateForFunctionPointer<GetPropertyInfoDelegate>(
                    SevenZipLibraryManager.Instance.GetProcAddress("IInArchive_GetPropertyInfo"));
            GetNumberOfArchivePropertiesImplementation =
                Marshal.GetDelegateForFunctionPointer<GetNumberOfDelegate>(
                    SevenZipLibraryManager.Instance.GetProcAddress("IInArchive_GetNumberOfArchiveProperties"));
            GetArchivePropertyInfoImplementation =
                Marshal.GetDelegateForFunctionPointer<GetArchivePropertyInfoDelegate>(
                    SevenZipLibraryManager.Instance.GetProcAddress("IInArchive_GetArchivePropertyInfo"));
        }

        public IntPtr ThisPointer { get; }

        public InArchiveWrapper(IntPtr thisPointer)
        {
            CacheMethods();
            Console.WriteLine($"InArchiveWrapper: {thisPointer.ToInt64():x8}");
            ThisPointer = thisPointer;
        }

        public int Open(IInStream stream, ref ulong maxCheckStartPosition, IArchiveOpenCallback openArchiveCallback)
        {
            var streamWrapped = new CInStream(stream);
            var openArchiveCallbackWrapped = new CArchiveOpenCallback(openArchiveCallback);
            return OpenImplementation(ThisPointer, streamWrapped.ThisPointer, ref maxCheckStartPosition,
                openArchiveCallbackWrapped.ThisPointer);
        }

        public void Close()
        {
            // TODO free native stream
            CloseImplementation(ThisPointer);
        }

        public uint GetNumberOfItems()
        {
            GetNumberOfItemsImplementation(ThisPointer, out var res);
            return res;
        }

        public void GetProperty(uint index, ItemPropId propId, ref PropVariant value)
        {
            GetPropertyImplementation(ThisPointer, index, propId, ref value);
        }

        public int Extract(uint[] indexes, uint numItems, int testMode, IArchiveExtractCallback extractCallback)
        {
            var gcHandle = GCHandle.Alloc(indexes, GCHandleType.Pinned);
            IntPtr extractCallbackObject = new CArchiveExtractCallback(extractCallback).ThisPointer;
            try
            {
                return ExtractImplementation(ThisPointer, gcHandle.AddrOfPinnedObject(), numItems, testMode,
                    extractCallbackObject);
            }
            finally
            {
                gcHandle.Free();
            }
        }

        public void GetArchiveProperty(ItemPropId propId, ref PropVariant value)
        {
            GetArchivePropertyImplementation(ThisPointer, propId, ref value);
        }

        public uint GetNumberOfProperties()
        {
            GetNumberOfPropertiesImplementation(ThisPointer, out var res);
            return res;
        }

        public void GetPropertyInfo(uint index, out string name, out ItemPropId propId, out ushort varType)
        {
            GetPropertyInfoImplementation(ThisPointer, index, out var namePtr, out propId, out varType);
            name = NativeMethods.MarshalPtrToBStrNew(namePtr);
        }

        public uint GetNumberOfArchiveProperties()
        {
            GetNumberOfArchivePropertiesImplementation(ThisPointer, out var res);
            return res;
        }

        public void GetArchivePropertyInfo(uint index, out string name, out ItemPropId propId, out ushort varType)
        {
            GetArchivePropertyInfoImplementation(ThisPointer, index, out var namePtr, out propId, out varType);
            name = NativeMethods.MarshalPtrToBStrNew(namePtr);
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
            IntPtr getStream,
            IntPtr getPassword);

        private static CreateIArchiveExtractCallbackCallback CreateIArchiveExtractCallback;
        private static ReleaseWrapperCallback ReleaseIArchiveExtractCallback;

        static void CacheMethods()
        {
            CreateIArchiveExtractCallback =
                Marshal.GetDelegateForFunctionPointer<CreateIArchiveExtractCallbackCallback>(
                    SevenZipLibraryManager.Instance.GetProcAddress("CreateIArchiveExtractCallback"));
            ReleaseIArchiveExtractCallback =
                Marshal.GetDelegateForFunctionPointer<ReleaseWrapperCallback>(
                    SevenZipLibraryManager.Instance.GetProcAddress("ReleaseIArchiveExtractCallback"));
        }

        private readonly CProgress.SetTotalDelegate _setTotal;
        private readonly CProgress.SetCompletedDelegate _setCompleted;
        private readonly SetOperationResultDelegate _setOperationResult;
        private readonly PrepareOperationDelegate _prepareOperation;
        private readonly GetStreamDelegate _getStream;
        private readonly GetTextPassword _getPassword;

        public IntPtr ThisPointer { get; }


        public CArchiveExtractCallback(IArchiveExtractCallback callback)
        {
            CacheMethods();
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
                             var getStreamPtr = actualStream is null
                                 ? IntPtr.Zero
                                 : new CSequentialOutStream(actualStream).ThisPointer;
                             Console.WriteLine($"GetStreamPtr: {getStreamPtr.ToInt64():x8}");
                             Marshal.WriteIntPtr(stream, getStreamPtr); // TODO
                             return res;
                         };

            if (callback is ICryptoGetTextPassword crypto)
            {
                _getPassword = (IntPtr pointer, out IntPtr password) =>
                               {
                                   var res = crypto.CryptoGetTextPassword(out var passwordText);
                                   password = NativeMethods.MarshalBStrNew(passwordText);
                                   return res;
                               };
            }

            var pointers = new[]
                           {
                               Marshal.GetFunctionPointerForDelegate(_setTotal),
                               Marshal.GetFunctionPointerForDelegate(_setCompleted),
                               Marshal.GetFunctionPointerForDelegate(_setOperationResult),
                               Marshal.GetFunctionPointerForDelegate(_prepareOperation),
                               Marshal.GetFunctionPointerForDelegate(_getStream),
                               Marshal.GetFunctionPointerForDelegate(_getPassword)
                           };

            Console.WriteLine(
                $"SetTotal:{pointers[0].ToInt64():x8}\nSetCompleted:{pointers[1].ToInt64():x8}\nSetOperationResult:{pointers[2].ToInt64():x8}\nPrepareOperation:{pointers[3].ToInt64():x8}\nGetStream:{pointers[4].ToInt64():x8}\n");

            ThisPointer =
                CreateIArchiveExtractCallback(pointers[0], pointers[1], pointers[2], pointers[3], pointers[4], pointers[5]);
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

        private static CreateISequentialOutStreamDelegate CreateISequentialOutStream;
        private static ReleaseWrapperCallback ReleaseISequentialOutStream;

        static void CacheMethods()
        {
            CreateISequentialOutStream =
                Marshal.GetDelegateForFunctionPointer<CreateISequentialOutStreamDelegate>(
                    SevenZipLibraryManager.Instance.GetProcAddress("CreateISequentialOutStream"));
            ReleaseISequentialOutStream =
                Marshal.GetDelegateForFunctionPointer<ReleaseWrapperCallback>(
                    SevenZipLibraryManager.Instance.GetProcAddress("ReleaseISequentialOutStream"));
        }

        private readonly WriteDelegate _write;

        public IntPtr ThisPointer { get; }

        public CSequentialOutStream(ISequentialOutStream outStream)
        {
            if (outStream is null)
                throw new ArgumentNullException();
            CacheMethods();
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

        static void CacheMethods()
        {
            CreateISequentialInStream =
                Marshal.GetDelegateForFunctionPointer<CreateISequentialInStreamDelegate>(
                    SevenZipLibraryManager.Instance.GetProcAddress("CreateISequentialInStream"));
            ReleaseISequentialInStream =
                Marshal.GetDelegateForFunctionPointer<ReleaseWrapperCallback>(
                    SevenZipLibraryManager.Instance.GetProcAddress("ReleaseISequentialInStream"));
        }

        private readonly ReadDelegate _read;

        public IntPtr ThisPointer { get; }

        public CSequentialInStream(ISequentialInStream inStream)
        {
            CacheMethods();
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

    internal class COutStream : INativeGarbageCollectable
    {
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int SeekDelegate(IntPtr thisPointer, long offset, uint seekOrigin, IntPtr newPosition);

        public delegate int SetSizeDelegate(IntPtr thisPointer, ulong newSize);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr CreateIOutStreamDelegate(IntPtr write, IntPtr seek, IntPtr setSize);

        private static CreateIOutStreamDelegate CreateIOutStream;
        private static ReleaseWrapperCallback ReleaseIOutStream;

        static void CacheMethods()
        {
            CreateIOutStream =
                Marshal.GetDelegateForFunctionPointer<CreateIOutStreamDelegate>(
                    SevenZipLibraryManager.Instance.GetProcAddress("CreateIOutStream"));
            ReleaseIOutStream =
                Marshal.GetDelegateForFunctionPointer<ReleaseWrapperCallback>(
                    SevenZipLibraryManager.Instance.GetProcAddress("ReleaseIOutStream"));
        }

        private readonly CSequentialOutStream.WriteDelegate _write;
        private readonly SeekDelegate _seek;
        private readonly SetSizeDelegate _setSize;

        public IntPtr ThisPointer { get; }

        public COutStream(IOutStream outStream)
        {
            CacheMethods();
            _write = (pointer, data, size, processedSize) =>
                     {
                         var dataArray = new byte[size];
                         Marshal.Copy(data, dataArray, 0, (int)size);
                         return outStream.Write(dataArray, size, processedSize);
                     };
            _seek = (pointer, offset, origin, position) =>
                    {
                        outStream.Seek(offset, (SeekOrigin)origin, position);
                        return 0;
                    };
            _setSize = (pointer, size) => outStream.SetSize((long)size);
            var pointers = new[]
                           {
                               Marshal.GetFunctionPointerForDelegate(_write),
                               Marshal.GetFunctionPointerForDelegate(_seek),
                               Marshal.GetFunctionPointerForDelegate(_setSize)
                           };
            Console.WriteLine($"Write:{pointers[0].ToInt64():x8}\nSeek:{pointers[1].ToInt64():x8}");

            ThisPointer = CreateIOutStream(pointers[0], pointers[1], pointers[2]);
            Console.WriteLine($"COutStream this: {ThisPointer.ToInt64():x8}");

            NativeGC.Add(this);
        }

        public void Dispose()
        {
            ReleaseIOutStream(ThisPointer);
        }
    }

    internal class CInStream : INativeGarbageCollectable
    {
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int SeekDelegate(IntPtr thisPointer, long offset, uint seekOrigin, IntPtr newPosition);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr CreateIInStreamDelegate(IntPtr read, IntPtr seek);

        private static CreateIInStreamDelegate CreateIInStream;
        private static ReleaseWrapperCallback ReleaseIInStream;

        static void CacheMethods()
        {
            CreateIInStream =
                Marshal.GetDelegateForFunctionPointer<CreateIInStreamDelegate>(
                    SevenZipLibraryManager.Instance.GetProcAddress("CreateIInStream"));
            ReleaseIInStream =
                Marshal.GetDelegateForFunctionPointer<ReleaseWrapperCallback>(
                    SevenZipLibraryManager.Instance.GetProcAddress("ReleaseIInStream"));
        }

        private readonly CSequentialInStream.ReadDelegate _read;
        private readonly SeekDelegate _seek;

        public IntPtr ThisPointer { get; }

        public CInStream(IInStream inStream)
        {
            CacheMethods();
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

    public delegate int GetTextPassword(IntPtr thisPointer, out IntPtr password);

    class CArchiveOpenCallback : INativeGarbageCollectable
    {
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int GetStreamDelegate(IntPtr thisPointer, IntPtr name, out IntPtr inStream);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int GetPropertyDelegate(IntPtr thisPointer, ItemPropId propId,
            ref PropVariant value);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int SetDelegate(IntPtr thisPointer, IntPtr files, IntPtr bytes);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr CreateIArchiveOpenCallbackDelegate(IntPtr setTotal, IntPtr setCompleted,
            IntPtr getPassword, IntPtr getStream, IntPtr getProperty);

        private static CreateIArchiveOpenCallbackDelegate CreateIArchiveOpenCallback;
        private static ReleaseWrapperCallback ReleaseIArchiveOpenCallback;

        static void CacheMethods()
        {
            CreateIArchiveOpenCallback =
                Marshal.GetDelegateForFunctionPointer<CreateIArchiveOpenCallbackDelegate>(
                    SevenZipLibraryManager.Instance.GetProcAddress("CreateIArchiveOpenCallback"));
            ReleaseIArchiveOpenCallback =
                Marshal.GetDelegateForFunctionPointer<ReleaseWrapperCallback>(
                    SevenZipLibraryManager.Instance.GetProcAddress("ReleaseIArchiveOpenCallback"));
        }

        private readonly SetDelegate _setTotal;
        private readonly SetDelegate _setCompleted;
        private readonly GetTextPassword _getPassword;
        private readonly GetStreamDelegate _getStream;
        private readonly GetPropertyDelegate _getProperty;

        public IntPtr ThisPointer { get; }

        public CArchiveOpenCallback(IArchiveOpenCallback openCallback)
        {
            CacheMethods();
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
            if (openCallback is ICryptoGetTextPassword crypto)
            {
                _getPassword = (IntPtr pointer, out IntPtr password) =>
                               {
                                   var res = crypto.CryptoGetTextPassword(out var passwordText);
                                   password = NativeMethods.MarshalBStrNew(passwordText);
                                   return res;
                               };
            }

            if (openCallback is IArchiveOpenVolumeCallback volume)
            {
                _getStream = (IntPtr pointer, IntPtr name, out IntPtr stream) =>
                             {
                                 var res = volume.GetStream(NativeMethods.MarshalPtrToBStrNew(name),
                                     out var inStream);
                                 stream = new CInStream(inStream).ThisPointer;
                                 return res;
                             };
                _getProperty = (IntPtr pointer, ItemPropId id, ref PropVariant value) =>
                               {
                                   var res = volume.GetProperty(id, ref value);
                                   return res;
                               };
            }

            var pointers = new[]
                           {
                               Marshal.GetFunctionPointerForDelegate(_setTotal),
                               Marshal.GetFunctionPointerForDelegate(_setCompleted),
                               Marshal.GetFunctionPointerForDelegate(_getPassword),
                               Marshal.GetFunctionPointerForDelegate(_getStream),
                               Marshal.GetFunctionPointerForDelegate(_getProperty)
                           };
            Console.WriteLine($"SetTotal:{pointers[0].ToInt64():x8}\nSetCompleted:{pointers[1].ToInt64():x8}");

            ThisPointer = CreateIArchiveOpenCallback(pointers[0], pointers[1], pointers[2], pointers[3], pointers[4]);

            Console.WriteLine($"CArchiveOpenCallback this: {ThisPointer.ToInt64():x8}");

            NativeGC.Add(this);
        }

        public void Dispose()
        {
            ReleaseIArchiveOpenCallback(ThisPointer);
        }
    }

    internal class CArchiveUpdateCallback : INativeGarbageCollectable
    {
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int GetPassword2Delegate(IntPtr thisPointer, ref int passwordIsDefined, out IntPtr password);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int SetOperationResultDelegate(IntPtr thisPointer, int opRes);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int GetStreamDelegate(IntPtr thisPointer, uint index, out IntPtr outStream);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int GetUpdateItemInfoDelegate(IntPtr thisPointer, uint index, ref int newData, ref int newProps,
            ref uint indexInArchive);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int GetPropertyDelegate(IntPtr thisPointer, uint index, ItemPropId propID,
            ref PropVariant value);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr CreateIArchiveUpdateCallbackDelegate(IntPtr setTotal,
            IntPtr setCompleted,
            IntPtr setOperationResult,
            IntPtr getStream,
            IntPtr getUpdateItemInfo,
            IntPtr getProperty,
            IntPtr getPassword2);

        private static CreateIArchiveUpdateCallbackDelegate CreateIArchiveUpdateCallback;
        private static ReleaseWrapperCallback ReleaseIArchiveUpdateCallback;

        static void CacheMethods()
        {
            CreateIArchiveUpdateCallback =
                Marshal.GetDelegateForFunctionPointer<CreateIArchiveUpdateCallbackDelegate>(
                    SevenZipLibraryManager.Instance.GetProcAddress("CreateIArchiveUpdateCallback"));
            ReleaseIArchiveUpdateCallback =
                Marshal.GetDelegateForFunctionPointer<ReleaseWrapperCallback>(
                    SevenZipLibraryManager.Instance.GetProcAddress("ReleaseIArchiveUpdateCallback"));
        }

        private readonly CProgress.SetTotalDelegate _setTotal;
        private readonly CProgress.SetCompletedDelegate _setCompleted;
        private readonly SetOperationResultDelegate _setOperationResult;
        private readonly GetStreamDelegate _getStream;
        private readonly GetUpdateItemInfoDelegate _getUpdateItemInfo;
        private readonly GetPropertyDelegate _getProperty;
        private readonly GetPassword2Delegate _getPassword2;


        public CArchiveUpdateCallback(IArchiveUpdateCallback callback)
        {
            CacheMethods();

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
            _getStream = (IntPtr pointer, uint index, out IntPtr stream) =>
                         {
                             var res = callback.GetStream(index, out var actualStream);
                             var getStreamWrapper = actualStream is IInStream actualStreamFull
                                 ? (INativeGarbageCollectable)new CInStream(actualStreamFull)
                                 : new CSequentialInStream(actualStream);
                             var getStreamPtr = getStreamWrapper.ThisPointer;
                             Console.WriteLine($"GetStreamPtr: {getStreamPtr.ToInt64():x8}");
                             stream = getStreamPtr; // TODO
                             return res;
                         };
            _getUpdateItemInfo = (IntPtr pointer, uint index, ref int data, ref int props, ref uint archive) =>
                                     callback.GetUpdateItemInfo(index, ref data, ref props, ref archive);
            _getProperty = (IntPtr pointer, uint index, ItemPropId id, ref PropVariant value) =>
                           {
                               var res = callback.GetProperty(index, id, ref value);
                               Console.WriteLine($"Prop Ptr: {value.Value.ToInt64():x8}");
                               return res;
                           };
            if (callback is ICryptoGetTextPassword2 crypto2)
            {
                _getPassword2 = (IntPtr pointer, ref int defined, out IntPtr password) =>
                                {
                                    var res = crypto2.CryptoGetTextPassword2(ref defined, out var passwordString);
                                    password = NativeMethods.MarshalBStrNew(passwordString);
                                    return res;
                                };
            }

            ThisPointer = CreateIArchiveUpdateCallback(Marshal.GetFunctionPointerForDelegate(_setTotal),
                Marshal.GetFunctionPointerForDelegate(_setCompleted),
                Marshal.GetFunctionPointerForDelegate(_setOperationResult),
                Marshal.GetFunctionPointerForDelegate(_getStream),
                Marshal.GetFunctionPointerForDelegate(_getUpdateItemInfo),
                Marshal.GetFunctionPointerForDelegate(_getProperty),
                Marshal.GetFunctionPointerForDelegate(_getPassword2));
        }

        public IntPtr ThisPointer { get; }

        public void Dispose()
        {
            ReleaseIArchiveUpdateCallback(ThisPointer);
        }
    }
}