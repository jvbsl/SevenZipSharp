using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace SevenZip
{
    internal static class SevenZipLibraryManager
    {
        private static Lazy<SevenZipLibraryManagerBase> instance = new Lazy<SevenZipLibraryManagerBase>(() =>
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return new LibraryManagerWindows();
            return new LibraryManagerUnix();
        }, LazyThreadSafetyMode.ExecutionAndPublication);

        public static SevenZipLibraryManagerBase Instance => instance.Value;
    }
}