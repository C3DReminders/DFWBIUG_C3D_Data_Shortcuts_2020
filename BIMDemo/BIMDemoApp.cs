using Autodesk.AutoCAD.ApplicationServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIMDemo
{
    internal static class BIMDemoApp
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
    }
}
