using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace BIMDemo.SQLiteDatabase
{
    public static class SQLiteLoader
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr LoadLibrary(string path);

        public static void LoadSQLite(string pathToSqliteDll)
        {
            IntPtr handle = LoadLibrary(pathToSqliteDll);
            if (handle == IntPtr.Zero)
            {
                throw new Exception("Could not load SQLite runtime from specified path.");
            }
        }
    }
}
