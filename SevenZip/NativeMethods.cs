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