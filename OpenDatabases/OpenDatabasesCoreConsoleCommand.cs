using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;
using Autodesk.Civil.ApplicationServices;
using Autodesk.Civil.DatabaseServices;
using DFWBIUG_C3D_Data_Shortcuts_2020.OpenDatabases.BatchDwgProcessing;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Surface = Autodesk.Civil.DatabaseServices.Surface;

[assembly: CommandClass(typeof(DFWBIUG_C3D_Data_Shortcuts_2020.OpenDatabasesCoreConsoleCommand))]

namespace DFWBIUG_C3D_Data_Shortcuts_2020
{
    class OpenDatabasesCoreConsoleCommand
    {
        private ConcurrentQueue<string> DwgPathsToProcess { get; set; }


        [CommandMethod("OpenDbsByCoreConsole")]
        public void OpenDbsByCoreConsoleCommand() // This method can have any name
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

                try
                {
                    #region CoreConsole

                    IsGettingDrawingsDone = false;

                    StartC3DRCoreConsoleWorker("Test1");

                    #endregion

                    DwgPathsToProcess = new ConcurrentQueue<string>();
                    // Add the drawings to process to the queue
                    foreach (var dwgToOpen in dwgsToOpen)
                    {
                        // It might take a little while for the 
                        // core console to be created. So let's
                        // wait until that occurs. 
                        while (!IsCoreConsoleCreated)
                        {
                            Thread.Sleep(500);
                        }
                        C3DRCoreConsoleWorker.DwgPaths.Enqueue(dwgToOpen);
                    }

                    C3DRCoreConsoleWorker.IsDoneAddingToQueue = true;
                    
                    while (!IsGettingDrawingsDone)
                    {
                        System.Threading.Thread.Sleep(1000);
                        System.Windows.Forms.Application.DoEvents();                        
                    }

                    foreach (var errorWorker in C3DRCoreConsoleWorker.Errors)
                    {
                        lines.Add(errorWorker.GetString());
                    }

                    try
                    {
                        foreach (var tempPath in TempFilePaths)
                        {
                            try
                            {
                                var linesTemp = File.ReadAllText(tempPath);
                                lines.Add(linesTemp);
                            }
                            catch (System.Exception ex)
                            {
                                ed.WriteMessage("Error: " + ex.Message);
                            }
                        }
                    }
                    catch (System.Exception ex)
                    {
                        ed.WriteMessage("Error: " + ex.Message);
                    }
                }
                catch (System.Exception ex)
                {
                    ed.WriteMessage("Error: " + ex.Message);
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

        private static void ProcessDatabase(List<string> lines, string dwgPath)
        {
            try
            {
                lines.Add("Drawing: " + dwgPath);
                
            }
            catch (System.Exception ex)
            {
                lines.Add("Error: " + ex.Message);
            }
        }

        public C3DRCoreConsoleWorker C3DRCoreConsoleWorker { get; set; }

        public bool IsGettingDrawingsDone { get; set; }
        public bool IsCoreConsoleCreated { get; set; }

        public List<string> TempFilePaths { get; set; }

        private async void StartC3DRCoreConsoleWorker(string ProgramRunId)
        {
            IsCoreConsoleCreated = false;
            await Task.Run(() =>
            {
                var timeout = 20 * 60000; // 20 Minuites

                var tempPath = Path.Combine(Path.GetTempPath(), "GetDwgInformation_Test.scr");

                if (!File.Exists(tempPath))
                {
                    var lines = File.ReadAllText(@"C:\Civil 3D Projects\GetDwgInformation.scr")
                                    .Replace("{Year}", "2020")
                                    .Replace("{ProgramRunId}", ProgramRunId); // {Year}
                    File.WriteAllText(tempPath, lines);
                }

                C3DRCoreConsoleWorker = new C3DRCoreConsoleWorker(GetActiveProfileName(),
                                                                  tempPath,
                                                                  timeout, timeout, 5, 0)
                {
                    WorkerReportsProgress = true,
                    WorkerSupportsCancellation = true
                };

                C3DRCoreConsoleWorker.RunWorkerCompleted += new System.ComponentModel.RunWorkerCompletedEventHandler(OnRunWorkerCompleted);
                C3DRCoreConsoleWorker.ProgressChanged += new ProgressChangedEventHandler(OnProgressChanged);

                C3DRCoreConsoleWorker.RunWorkerAsync();

                IsCoreConsoleCreated = true;
            });
        }

        private void OnRunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            var commandLineOutputs = string.Join("\n", C3DRCoreConsoleWorker.CommandLineOutputs);
            TempFilePaths = C3DRCoreConsoleWorker.CommandLineOutputs.Where(x => !string.IsNullOrEmpty(x))
                                                                    .Where(x => x.StartsWith("C3DRDwg File:"))
                                                                    .Select(x => x.Replace("C3DRDwg File:", "").Trim()).ToList();
            var logFilePath = Path.Combine(Path.GetTempPath(), "C3DRProj_Log_" + Guid.NewGuid().ToString().Replace("{", "").Replace("}", "") + ".txt");
            File.WriteAllLines(logFilePath, C3DRCoreConsoleWorker.CommandLineOutputs);
            IsGettingDrawingsDone = true;
        }

        private void OnProgressChanged(object sender, ProgressChangedEventArgs e)
        {

        }

        public static string GetActiveProfileName()
        {
            var acadApp = (dynamic)Autodesk.AutoCAD.ApplicationServices.Application.AcadApplication;
            var preferences = acadApp.Preferences;
            var profiles = (dynamic)preferences.Profiles;
            var currentProfile = profiles.ActiveProfile.ToString();
            return currentProfile;
        }

        // A Lisp Function to collect all of the required information.
        [LispFunction("GetDwgInformation")]
        public void GetDwgInformation(ResultBuffer rbArgs)
        {
            // (GetDwgInformation "ProgramRunId")

            var doc = Application.DocumentManager.MdiActiveDocument;
            var ed = doc.Editor;

            try
            {
                var civDoc = CivilApplication.ActiveDocument;
                

                var programRunId = rbArgs.AsArray().Select(x => x.Value?.ToString()).ToList().DefaultIfEmpty("").FirstOrDefault();

                using (var tr = HostApplicationServices.WorkingDatabase.TransactionManager.StartTransaction())
                {
                    var lines = new List<string>();
                    OpenDatabasesCommand.GetSampleDwgInfo(lines, civDoc);
                    WriteToTemporaryFile(programRunId, lines);
                    tr.Commit();
                }
            }
            catch (System.Exception ex)
            {
                Application.DocumentManager.MdiActiveDocument.Editor.WriteMessage("\nError: " + ex.Message + "\n");
            }
        }

        private void WriteToTemporaryFile(string programRunId, List<string> lines)
        {
            var tempName = Guid.NewGuid().ToString().Replace("{", "").Replace("}", "").Replace("-", "") + ".txt";
            var tempFolderPath = Path.GetTempPath();
            var tempFullPath = Path.Combine(tempFolderPath, "DwgInfo_" + programRunId + "_" + tempName);

            File.WriteAllLines(tempFullPath, lines);

            Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.Editor.WriteMessage("C3DRDwg File:" + tempFullPath);
        }
    }
}
