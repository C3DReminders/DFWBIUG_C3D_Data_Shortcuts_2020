using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;
using Autodesk.Civil.ApplicationServices;
using Autodesk.Civil.DatabaseServices.Styles;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

[assembly: CommandClass(typeof(DFWBIUG_C3D_Data_Shortcuts_2020.StyleColorsCommand))]

namespace DFWBIUG_C3D_Data_Shortcuts_2020
{
    public class StyleColorsCommand
    {

        [CommandMethod("StyleColorsExport")]
        public void StyleColorsExportCommand() // This method can have any name
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

                using (var tr = doc.Database.TransactionManager.StartTransaction())
                {
                    ProcessObjects(lines, civDoc.Styles);
                    tr.Commit();
                }

                File.WriteAllLines(saveFileName, lines);
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage("Error: " + ex.Message);
            }
        }

        private void ProcessObjects(List<string> lines, object parent)
        {
            PropertyInfo[] properties = getPropertyInformation(parent);

            foreach (var property in properties)
            {
                lines.Add(property.Name);
                var obj = CreateInstance(parent, property);

                var isColl = IsCollection(property);
                if (isColl == true)
                {
                    lines.Add("Collection: " + property.Name);
                    ProcessStyleCollection(lines, obj);
                }
                else
                {
                    ProcessObjects(lines, obj);
                }
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

        // We define this utility class that will be used by derived classes to get property information from an
        // object. This is a nice utility, and in a perfect world, we would implement it in a utility class not here.
        //
        protected static PropertyInfo[] getPropertyInformation(object fromObject)
        {
            // We are interested in public, declared, or instance properties.
            Type objectType = fromObject.GetType();
            PropertyInfo[] properties = objectType.GetProperties(BindingFlags.Public | BindingFlags.DeclaredOnly | BindingFlags.Instance);
            return properties;
        }

        public static object CreateInstance(object parent, System.Reflection.PropertyInfo info)
        {
            // This method gets call from our parent, which is a style root. We want to instantiate the collection
            // and get called Collect() with it.
            //
            Type objectType = parent.GetType();
            return objectType.InvokeMember(info.Name, BindingFlags.GetProperty, null, parent, new object[0]);
        }

        public static bool? IsCollection(PropertyInfo objectType)
        {
            // This utility method helps us to figure out what type of collector we need from the property information
            // of a property in the object we want to collect. The idea is that style roots can contain other style
            // roots or style collections, and we will not know which one we need unless we evaluate the name of the
            // property type. If the name contains 'Collection' is a style collection. If the name contains 'Root', it
            // means is a style root. If none of this are contained in the property type name, we return 'Style'. It
            // doesn't really make sense to return 'Style' as a default. In reality, we should throw an exception if
            // it is not a collection or a root, but fortunately, we prevent hitting the default in the client code.
            //
            string propertyType = objectType.PropertyType.ToString();
            if (propertyType.Contains("Collection"))
            {
                return true;
            }
            else if (propertyType.Contains("Root"))
            {
                return null;
            }
            return false;
        }

        public static void ProcessStyleCollection(List<string> lines, object obj)
        {
            var styleColl = obj as StyleCollectionBase;
            
            foreach (var id in styleColl)
            {
                var style = id.GetObject(OpenMode.ForRead) as StyleBase;
                lines.Add("StyleName," + style.Name);

                PropertyInfo[] properties = getPropertyInformation(style);

                ProcessProperties(lines, ",", style, properties);
            }
        }

        private static void ProcessProperties(List<string> lines, string prefix, object parentObj, PropertyInfo[] properties)
        {
            if (parentObj is ObjectId || parentObj is string)
            {
                return;
            }

            foreach (PropertyInfo property in properties)
            {
                try
                {
                    lines.Add(prefix + property.Name + "," + property.GetValue(parentObj));
                    var childObj = CreateInstance(parentObj, property);
                    if (childObj is null)
                    {
                        continue;
                    }
                    var props = getPropertyInformation(childObj);
                    ProcessProperties(lines, (prefix + ","), childObj, props);
                }
                catch (System.Exception ex)
                {

                }
            }
        }
    }
}
