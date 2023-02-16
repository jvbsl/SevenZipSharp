using System;
using System.Configuration;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace SevenZip
{
    internal class LibraryManagerWindows : SevenZipLibraryManagerBase
    {
        #region Native
        
        [DllImport("kernel32.dll", BestFitMapping = false, ThrowOnUnmappableChar = true)]
        private static extern IntPtr LoadLibrary([MarshalAs(UnmanagedType.LPStr)] string fileName);

        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool FreeLibrary(IntPtr hModule);

        [DllImport("kernel32.dll", BestFitMapping = false, ThrowOnUnmappableChar = true)]
        private static extern IntPtr GetProcAddress(IntPtr hModule, [MarshalAs(UnmanagedType.LPStr)] string procName);
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

            return Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), Environment.Is64BitProcess ? "7z64.dll" : "7z.dll");
        }

        public override string GetLastError()
        {
            return $"Error code: {Marshal.GetLastWin32Error()}";
        }

        public override IntPtr NativeLoadLibrary(string fileName)
        {
            return LoadLibrary(fileName);
        }

        public override bool NativeFreeLibrary(IntPtr hModule)
        {
            return FreeLibrary(hModule);
        }

        public override IntPtr NativeGetProcAddress(IntPtr hModule, string procName)
        {
            return GetProcAddress(hModule, procName);
        }
    }
}