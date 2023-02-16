using System;
using System.Configuration;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace SevenZip
{
    internal class LibraryManagerUnix : SevenZipLibraryManagerBase
    {
        #region Native

        private const int RTLD_NOW = 0x2;
        private const int RTLD_LAZY = 0x00001 ;
        
        [DllImport("dl", BestFitMapping = false, ThrowOnUnmappableChar = true)]
        private static extern IntPtr dlopen(string fileName, int flags);        
        [DllImport("dl", BestFitMapping = false, ThrowOnUnmappableChar = true)]
        private static extern IntPtr dlerror();

        [DllImport("dl")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool dlclose(IntPtr hModule);

        [DllImport("dl", BestFitMapping = false, ThrowOnUnmappableChar = true)]
        private static extern IntPtr dlsym(IntPtr hModule, string procName);
        #endregion
        protected override string DetermineLibraryFilePath()
        {
            if (!string.IsNullOrEmpty(ConfigurationManager.AppSettings["7zLocation"]))
            {
                return ConfigurationManager.AppSettings["7zLocation"];
            }
    
            if (string.IsNullOrEmpty(Assembly.GetExecutingAssembly().Location)) 
            {
                return null;
            }

            return "/home/julian/Projects/SevenZipCBindings/cmake-build-debug/libSevenZipCBinding.so";
            return Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "7z.so"); // TODO dylib
        }

        public override string GetLastError()
        {
            return Marshal.PtrToStringAnsi(dlerror());
        }

        public override IntPtr NativeLoadLibrary(string fileName)
        {
            return dlopen(fileName, RTLD_LAZY);
        }

        public override bool NativeFreeLibrary(IntPtr hModule)
        {
            return dlclose(hModule);
        }

        public override IntPtr NativeGetProcAddress(IntPtr hModule, string procName)
        {
            return dlsym(hModule, procName);
        }
    }
}