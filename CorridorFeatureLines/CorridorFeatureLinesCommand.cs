using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;
using Autodesk.Civil.ApplicationServices;
using Autodesk.Civil.DatabaseServices;
using DFWBIUG_C3D_Data_Shortcuts_2020.CorridorFeatureLines;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Entity = Autodesk.AutoCAD.DatabaseServices.Entity;

[assembly: CommandClass(typeof(DFWBIUG_C3D_Data_Shortcuts_2020.CorridorFeatureLinesCommand))]

namespace DFWBIUG_C3D_Data_Shortcuts_2020
{
    public class CorridorFeatureLinesCommand
    {
        [CommandMethod("CorrSurfPointCodes")]
        public void CorrSurfPointCodesCommand() // This method can have any name
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
                var saveFileName = GetSaveFileLocation("StyleColorExport.csv");

                if (string.IsNullOrEmpty(saveFileName))
                {
                    return;
                }

                var lines = new List<string>();

                lines.Add("DrawingName," + doc.Name);

                var civDoc = CivilApplication.ActiveDocument;
                var surfInfos = new List<SurfaceInformation>();

                using (var tr = doc.Database.TransactionManager.StartTransaction())
                {
                    foreach (var corrId in civDoc.CorridorCollection)
                    {
                        var corr = corrId.GetObject(OpenMode.ForRead) as Corridor;

                        if (corr.CorridorSurfaces.Count == 0)
                        {
                            continue;
                        }

                        foreach (var corrSurf in corr.CorridorSurfaces)
                        {
                            // The surface isn't built, so skip it. 
                            if (!corrSurf.IsBuild)
                            {
                                continue;
                            }

                            var surfInfo = new SurfaceInformation(corrSurf.Name, corrSurf.SurfaceId);
                            var surfPtCodes = new List<string>();

                            surfInfo.LinkCodes.AddRange(corrSurf.LinkCodes());
                            foreach (var linkCode in surfInfo.LinkCodes)
                            {
                                foreach (var baseline in corr.Baselines)
                                {
                                    foreach (var region in baseline.BaselineRegions)
                                    {
                                        foreach (var assembly in region.AppliedAssemblies)
                                        {
                                            var links = assembly.get_LinksByCode(linkCode);
                                            var codes = links.SelectMany(l => l.CalculatedPoints.SelectMany(x => x.CorridorCodes)).Distinct();
                                            surfPtCodes.AddRange(codes);
                                        }
                                    }
                                }
                            }
                            surfPtCodes.AddRange(corrSurf.PointCodes());
                            surfInfo.PointCodes.AddRange(surfPtCodes.Distinct());

                            surfInfos.Add(surfInfo);
                        }
                    }

                    tr.Commit();
                }

                lines.AddRange(surfInfos.Select(x => x.GetString()));

                File.WriteAllLines(saveFileName, lines);
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage("Error: " + ex.Message);
            }
        }

        public static string GetSaveFileLocation(string fileName)
        {
            var doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            string settingsFileLocation = Path.Combine(Path.GetDirectoryName(doc.Name), fileName);

            using (var saveDialog = new System.Windows.Forms.SaveFileDialog())
            {
                saveDialog.Title = "Save Style Information File";
                saveDialog.Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*";

                saveDialog.InitialDirectory = settingsFileLocation;

                if (saveDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    return saveDialog.FileName;
                }
                else
                {
                    return "";
                }
            }
        }
    }
}
