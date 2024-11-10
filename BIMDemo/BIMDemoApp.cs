using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Runtime;
using BIMDemo.SQLiteDatabase;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Exception = System.Exception;

namespace BIMDemo
{
    public class BIMDemoApp : IExtensionApplication
    {
        public static void WriteMessage(string message)
        {
            Application.DocumentManager.MdiActiveDocument.Editor.WriteMessage("\n" + message + "\n");
        }

        public static void WriteErrorMessage(string message, Exception ex)
        {
            Application.DocumentManager.MdiActiveDocument.Editor.WriteMessage("\n" + message + "\n");
            Application.DocumentManager.MdiActiveDocument.Editor.WriteMessage("\n" + ex.StackTrace + "\n");
        }

        public void Initialize()
        {
            try
            {
#pragma warning disable SYSLIB0012 // Type or member is obsolete
                var dllFolder = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().CodeBase);
#pragma warning restore SYSLIB0012 // Type or member is obsolete
                dllFolder = dllFolder.Substring(6);
                var dllFullPath = Path.Combine(dllFolder, @"e_sqlite3.dll");
                SQLiteLoader.LoadSQLite(dllFullPath); // Specify your SQLite DLL path here
            }
            catch (Exception ex)
            {
                WriteMessage(ex.Message);
            }
        }

        public void Terminate()
        {

        }

    }
}
