using System.Text;

namespace SevenZip
{
    using System;
    using System.Runtime.InteropServices;

#if UNMANAGED
    internal static class NativeMethods
    {
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int CreateObjectDelegate(
            [In] ref Guid classID,
            [In] ref Guid interfaceID,
            out IntPtr outObject);

        static IntPtr AllocateForBSTR(int size)
        {
            return Marshal.AllocHGlobal(size);
        }
        static IntPtr SysAllocStringLen(string s, int len)
        {
            const uint k_BstrSize_Max = 0xFFFFFFFF;
            const int CBstrSize = 4;
            if (len >= (k_BstrSize_Max - sizeof(char) - CBstrSize) / sizeof(char))
                return IntPtr.Zero;

            var size = len * sizeof(char);
            var p = AllocateForBSTR(size + CBstrSize + sizeof(char));
            if (p == IntPtr.Zero)
                return IntPtr.Zero;
            Marshal.WriteInt32(p, size);
            var bstr = p + CBstrSize;
            Marshal.Copy(s.ToCharArray(), 0, bstr, s.Length);
            Marshal.WriteInt16(bstr + size, 0);
            return bstr;
        }
        static IntPtr SysAllocStringLen32(string s, int len)
        {
            const uint k_BstrSize_Max = 0xFFFFFFFF;
            const int CBstrSize = 4;
            if (len >= (k_BstrSize_Max - sizeof(int) - CBstrSize) / sizeof(int))
                return IntPtr.Zero;

            var size = len * sizeof(int);
            var p = AllocateForBSTR(size + CBstrSize + sizeof(int));
            if (p == IntPtr.Zero)
                return IntPtr.Zero;
            var bstr = p + CBstrSize;
            var bytes = Encoding.UTF32.GetBytes(s);
            
            Marshal.WriteInt32(p, size);
            Marshal.Copy(bytes, 0, bstr, bytes.Length);
            Marshal.WriteInt32(bstr + size, 0);
            return bstr;
        }
        public static IntPtr MarshalBStrNew(string val)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return SysAllocStringLen(val, (val?.Length ?? 0));
            }

            return SysAllocStringLen32(val, (val?.Length ?? 0));
            var bstr = Marshal.AllocHGlobal(val.Length * 2 + sizeof(int));
            Marshal.WriteInt32(bstr, val.Length * 2);
            var byteArray = Encoding.Unicode.GetBytes(val); // TODO: use unsafe
            Marshal.Copy(byteArray, 0, bstr + 4, byteArray.Length);
            return bstr + 4;
        }

        public static string MarshalPtrToBStrNew(IntPtr val)
        {
            if (val == IntPtr.Zero)
                return null;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return Marshal.PtrToStringBSTR(val);
            }
            var bytes = new byte[Marshal.ReadInt32(val - 4)];
            Marshal.Copy(val, bytes, 0, bytes.Length);

            return Encoding.UTF32.GetString(bytes);
        }

        public static T SafeCast<T>(PropVariant var, T def)
        {
            object obj;
            
            try
            {
                obj = var.Object;
            }
            catch (Exception)
            {
                return def;
            }

            if (obj is T expected)
            {
                return expected;
            }
            
            return def;
        }
    }
#endif
}