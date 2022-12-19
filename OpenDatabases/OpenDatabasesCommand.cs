using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;
using Autodesk.Civil.ApplicationServices;
using Autodesk.Civil.DatabaseServices;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Surface = Autodesk.Civil.DatabaseServices.Surface;

[assembly: CommandClass(typeof(DFWBIUG_C3D_Data_Shortcuts_2020.OpenDatabasesCommand))]

namespace DFWBIUG_C3D_Data_Shortcuts_2020
{
    public class OpenDatabasesCommand
    {
        [CommandMethod("OpenDbsBySideLoading")]
        public void OpenDbsBySideLoadingCommand() // This method can have any name
        {
            // Put your command code here
            var doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null)
            {
                return;
            }

            var ed = doc.Editor;

            try
            {
                var dwgsToOpen = GetDrawings();

                if (!dwgsToOpen.Any())
                {
                    return;
                }

                var saveFileName = StyleColorsCommand.GetSaveFileLocation("");

                var lines = new List<string>();

                var openDbs = new List<Database>();

                try
                {
                    foreach (var dwgPath in dwgsToOpen)
                    {
                        ProcessDatabase(lines, openDbs, dwgPath);
                    }
                }
                catch (System.Exception ex)
                {
                    ed.WriteMessage("Error: " + ex.Message);
                }
                finally
                {
                    // Dispose of the open databases that are not already disposed.
                    foreach (var openDb in openDbs.Where(x => x != null && !x.IsDisposed))
                    {
                        openDb.Dispose();
                    }
                }
                
                File.WriteAllLines(saveFileName, lines);
                System.Diagnostics.Process.Start(saveFileName);
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage("Error: " + ex.Message);
            }
        }

        public static List<string> GetDrawings()
        {
            using (var openDialog = new System.Windows.Forms.OpenFileDialog())
            {
                openDialog.Title = "Open Drawings";
                openDialog.Filter = "DWG files (*.dwg)|*.dwg|All files (*.*)|*.*";
                openDialog.Multiselect = true;

                if (openDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    return openDialog.FileNames.ToList();
                }
                else
                {
                    return new List<string>();
                }
            }
        }

        private static void ProcessDatabase(List<string> lines, List<Database> openDbs, string dwgPath)
        {
            try
            {
                lines.Add("Drawing: " + dwgPath);
                var dwgDb = new Database(false, true);
                dwgDb.ReadDwgFile(dwgPath, FileOpenMode.OpenForReadAndAllShare, true, "");

                var civDoc = CivilDocument.GetCivilDocument(dwgDb);

                using (Transaction acTrans = dwgDb.TransactionManager.StartTransaction())
                {
                    GetSampleDwgInfo(lines, civDoc);
                }
            }
            catch (System.Exception ex)
            {
                lines.Add("Error: " + ex.Message);
            }
        }

        public static void GetSampleDwgInfo(List<string> lines, CivilDocument civDoc)
        {
            lines.Add("Surfaces");
            foreach (var surf in civDoc.GetSurfaceIds().Cast<ObjectId>().Select(x => x.GetObject(OpenMode.ForRead) as Surface))
            {
                lines.Add("\t" + surf.Name);
            }

            lines.Add("Corridors");
            foreach (var corr in civDoc.CorridorCollection.Select(x => x.GetObject(OpenMode.ForRead) as Corridor))
            {
                lines.Add("\t" + corr.Name);
            }

            lines.Add("Sites");
            foreach (var site in civDoc.GetSiteIds().Cast<ObjectId>().Select(x => x.GetObject(OpenMode.ForRead) as Site))
            {
                lines.Add("\t" + site.Name);
            }
        }
    }
}
