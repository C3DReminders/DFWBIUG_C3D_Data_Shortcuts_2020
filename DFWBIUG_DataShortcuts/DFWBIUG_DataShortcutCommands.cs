// (C) Copyright 2021 by  
//
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Autodesk.Civil.DataShortcuts;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

// This line is not mandatory, but improves loading performances
[assembly: CommandClass(typeof(DFWBIUG_C3D_Data_Shortcuts_2022.DFWBIUG_DataShortcutCommands))]

namespace DFWBIUG_C3D_Data_Shortcuts_2022
{
    // This class is instantiated by AutoCAD for each document when
    // a command is called by the user the first time in the context
    // of a given document. In other words, non static data in this class
    // is implicitly per-document!
    public class DFWBIUG_DataShortcutCommands
    {
        // The CommandMethod attribute can be applied to any public  member 
        // function of any public class.
        // The function should take no arguments and return nothing.
        // If the method is an intance member then the enclosing class is 
        // intantiated for each document. If the member is a static member then
        // the enclosing class is NOT intantiated.
        //
        // NOTE: CommandMethod has overloads where you can provide helpid and
        // context menu.

        // Modal Command with localized name
        [CommandMethod("DFWBIUG", "DSToggle", "DSToggle", CommandFlags.Modal)]
        public void DSToggleCommand() // This method can have any name
        {
            // Put your command code here
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null)
            {
                return;
            }

            var ed = doc.Editor;

            try
            {
                // Find the _Shortcuts folder.
                var _shortcutDirectoryInfos = FindShortcutsFolder(doc);

                if (!_shortcutDirectoryInfos.Any())
                {
                    // We didn't find any "_Shortcuts" folders. 
                    ed.WriteMessage("\nDidn't find any _Shortcuts folders, the Working Folder and Data Shortcuts were not set.");
                    return; // Exits the method.
                }

                // Get the current Working Folder. 
                var currentWorkingFolder = DataShortcuts.GetWorkingFolder();

                // Trim out any trailing \\
                currentWorkingFolder = currentWorkingFolder.Trim('\\');

                if (_shortcutDirectoryInfos.Count == 1)
                {
                    var _shortcutDirectoryInfo = _shortcutDirectoryInfos.First();

                    var workingFldr = GetWorkingFolderFromDirectoryInfo(_shortcutDirectoryInfo);

                    // Check to see if the working folder is set. If it is already set, it can 
                    // be a time killer if there are lots of folders.
                    if (!currentWorkingFolder.Equals(workingFldr))
                    {
                        DataShortcuts.SetWorkingFolder(workingFldr);
                    }

                    // The project name is the name of the folder. 
                    var projName = _shortcutDirectoryInfo.Parent.Name;

                    // Get the current project's name
                    var currentProjectName = DataShortcuts.GetCurrentProjectFolder();

                    // Check to see if the project is set. If it is already set it can
                    // be a time killer for Civil 3D to go through and verify all of the 
                    // Data shortcuts in a project.
                    if (!currentProjectName.Equals(projName, StringComparison.OrdinalIgnoreCase))
                    {
                        DataShortcuts.SetCurrentProjectFolder(projName);
                    }

                    // Update the shortcut node.
                    Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.SendStringToExecute("REFRESHSHORTCUTNODE ", false, false, false);

                    return;
                }

                // We have more than one _Shortcuts folder, so lets toggle between them.
                var currentIndex = -1;
                var workingFolderInfo = new List<Tuple<DirectoryInfo, string>>();
                for (int i = 0; i < _shortcutDirectoryInfos.Count; i++)
                {
                    var workingFldr = GetWorkingFolderFromDirectoryInfo(_shortcutDirectoryInfos[i]);
                    if (workingFldr.Equals(currentWorkingFolder))
                    {
                        currentIndex = i + 1;
                    }

                    workingFolderInfo.Add(Tuple.Create(_shortcutDirectoryInfos[i], workingFldr));
                }

                currentIndex = (currentIndex == -1 || currentIndex > _shortcutDirectoryInfos.Count - 1) ? 0 : currentIndex;

                var workingFolder = workingFolderInfo[currentIndex].Item2;
                var projectName = workingFolderInfo[currentIndex].Item1.Parent.Name;

                DataShortcuts.SetWorkingFolder(workingFolder);
                DataShortcuts.SetCurrentProjectFolder(projectName);

                // Update the shortcut node.
                Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.SendStringToExecute("REFRESHSHORTCUTNODE ", false, false, false);
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage("Error: " + ex.Message);
            }            
        }

        private static string GetWorkingFolderFromDirectoryInfo(DirectoryInfo _shortcutDirectoryInfo)
        {
            // Get the parent folder
            var workingFolder = _shortcutDirectoryInfo.Parent.Parent.FullName;

            // Remove any trailing \ from the path. 
            workingFolder = workingFolder.Trim('\\');
            return workingFolder;
        }

        private static List<DirectoryInfo> FindShortcutsFolder(Document doc)
        {
            var dwgLocation = doc.Name;

            // Get the path of the document name, the drawing location.
            var dwgDirectory = Path.GetDirectoryName(dwgLocation);
            var directoryInfos = new List<DirectoryInfo>();

            // Convert the path into DirectoryInfo, this lets you find out what is in a folder.
            var directoryInfo = new DirectoryInfo(dwgDirectory);

            // Process the folders until we get to the root directory, ie "C:\".
            while (directoryInfo.Name != directoryInfo.Root.Name)
            {
                // Get the folders in the current path.
                var found_ShortcutFolders = directoryInfo.GetDirectories("_Shortcuts");
                if (found_ShortcutFolders.Any())
                {
                    // We found a _Shortcuts folder, so lets add it to the list.
                    directoryInfos.Add(found_ShortcutFolders.First());
                }

                directoryInfo = directoryInfo.Parent;
            }

            return directoryInfos;
        }


        [CommandMethod("DFWBIUG", "SetDSAndAssociateToProject", "SetDSAndAssociateToProject", CommandFlags.Modal)]
        public void SetDSAndAssociateToProjectCommand() // This method can have any name
        {
            // Put your command code here
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null)
            {
                return;
            }

            var ed = doc.Editor;

            try
            {
                // Find the _Shortcuts folder.
                var _shortcutDirectoryInfos = FindShortcutsFolder(doc);

                if (!_shortcutDirectoryInfos.Any())
                {
                    // We didn't find any "_Shortcuts" folders. 
                    ed.WriteMessage("\nDidn't find any _Shortcuts folders, the Working Folder and Data Shortcuts were not set.");
                    return; // Exits the method.
                }

                // Get the current Working Folder. 
                var currentWorkingFolder = DataShortcuts.GetWorkingFolder();

                // Trim out any trailing \\
                currentWorkingFolder = currentWorkingFolder.Trim('\\');

                // There is more than one project, so exit.
                if (_shortcutDirectoryInfos.Count > 1)
                {
                    ed.WriteMessage("\nFound too many _Shortcuts folders, the Working Folder and Data Shortcuts were not set.");
                    return; // Exits the method.
                }

                var _shortcutDirectoryInfo = _shortcutDirectoryInfos.First();

                var workingFldr = GetWorkingFolderFromDirectoryInfo(_shortcutDirectoryInfo);

                // Check to see if the working folder is set. If it is already set, it can 
                // be a time killer if there are lots of folders.
                if (!currentWorkingFolder.Equals(workingFldr))
                {
                    DataShortcuts.SetWorkingFolder(workingFldr);
                }

                // The project name is the name of the folder. 
                var projName = _shortcutDirectoryInfo.Parent.Name;

                // Get the current project's name
                var currentProjectName = DataShortcuts.GetCurrentProjectFolder();

                // Check to see if the project is set. If it is already set it can
                // be a time killer for Civil 3D to go through and verify all of the 
                // Data shortcuts in a project.
                if (!currentProjectName.Equals(projName, StringComparison.OrdinalIgnoreCase))
                {
                    DataShortcuts.SetCurrentProjectFolder(projName);
                }

                // Get the DS Project ID
                var dsProjId = DataShortcuts.GetDSProjectId(_shortcutDirectoryInfo.Parent.FullName);

                // Associate the drawing to the project. 
                DataShortcuts.AssociateDSProject(dsProjId);
                ed.WriteMessage("Associated the drawing to project " + dsProjId);

                // Update the shortcut node.
                Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.SendStringToExecute("REFRESHSHORTCUTNODE ", false, false, false);

            }
            catch (System.Exception ex)
            {
                ed.WriteMessage("Error: " + ex.Message);
            }
        }

        [CommandMethod("DFWBIUG", "CreateDataReferences", "CreateDataReferences", CommandFlags.Modal)]
        public void CreateDataReferencesCommand() // This method can have any name
        {
            // Put your command code here
            Document doc = Application.DocumentManager.MdiActiveDocument;
            var db = doc.Database;
            if (doc == null)
            {
                return;
            }

            var ed = doc.Editor;

            try
            {
                var isValidCreation = false;
                var dsMgr = DataShortcuts.CreateDataShortcutManager(ref isValidCreation);

                if (!isValidCreation)
                {
                    ed.WriteMessage("Unable to create data shortcut manager, verify the working folder and project is set.");
                    return;
                }

                for (int i = 0; i < dsMgr.GetPublishedItemsCount(); i++)
                {
                    var dsInfo = dsMgr.GetPublishedItemAt(i);

                    ed.WriteMessage("\nDS Name: " + dsInfo.Name);
                    ed.WriteMessage("\nDS Description: " + dsInfo.Description);
                    ed.WriteMessage("\nDS Type: " + dsInfo.DSEntityType);
                    ed.WriteMessage("\nDS Source File Name: " + dsInfo.SourceFileName);
                    ed.WriteMessage("\nDS Source Location: " + dsInfo.SourceLocation);
                    
                    dsMgr.CreateReference(i, db);
                }

            }
            catch (System.Exception ex)
            {
                ed.WriteMessage("Error: " + ex.Message);
            }
        }

        [CommandMethod("DFWBIUG", "CreateDSs", "CreateDSs", CommandFlags.Modal)]
        public void CreateDSsCommand() // This method can have any name
        {
            // Put your command code here
            Document doc = Application.DocumentManager.MdiActiveDocument;
            var db = doc.Database;
            if (doc == null)
            {
                return;
            }

            var ed = doc.Editor;

            try
            {
                var isValidCreation = false;
                var dsMgr = DataShortcuts.CreateDataShortcutManager(ref isValidCreation);

                if (!isValidCreation)
                {
                    ed.WriteMessage("Unable to create data shortcut manager, verify the working folder and project is set.");
                    return;
                }

                for (int i = 0; i < dsMgr.GetExportableItemsCount(); i++)
                {
                    var dsInfo = dsMgr.GetExportableItemAt(i);

                    ed.WriteMessage("\nDS Name: " + dsInfo.Name);
                    ed.WriteMessage("\nDS Description: " + dsInfo.Description);
                    ed.WriteMessage("\nDS Type: " + dsInfo.DSEntityType);

                    dsMgr.SetSelectItemAtIndex(i, true);
                }

                DataShortcuts.SaveDataShortcutManager(ref dsMgr);

                // Update the shortcut node.
                Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.SendStringToExecute("REFRESHSHORTCUTNODE ", false, false, false);
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage("Error: " + ex.Message);
            }
        }
    }

}
