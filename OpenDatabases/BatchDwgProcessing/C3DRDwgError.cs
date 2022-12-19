using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DFWBIUG_C3D_Data_Shortcuts_2020.OpenDatabases.BatchDwgProcessing
{
    public class C3DRDwgError
    {
        public string Name { get; set; }
        public string FullName { get; set; }
        public string Folder { get; set; }
        public C3DRWorkerResult ErrorType { get; set; }
        public string CmdOutput { get; set; }
        public int ProcessTime { get; set; }
        public FileAttributes DwgFileAttributes { get; set; }
        public DateTime DwgModifiedTime { get; set; }
        public bool HasErrors { get; set; }
        public string ErrorInformation { get; set; }
        public int ExitCode { get; set; }

        public C3DRDwgError(C3DRWorkInformation workInfo)
        {
            Name = Path.GetFileName(workInfo.DwgPath);
            Folder = Path.GetDirectoryName(workInfo.DwgPath);
            FullName = workInfo.DwgPath;
            ErrorType = workInfo.Result;
            CmdOutput = string.Join("\n", workInfo.CommandOutput);
            ProcessTime = workInfo.ProcessTime;
            DwgFileAttributes = workInfo.DwgFileAttributes;
            DwgModifiedTime = workInfo.DwgModifiedTime;

            try
            {
                ExitCode = workInfo.Civil3DProcess.ExitCode;
            }
            catch (Exception)
            {
                ExitCode = -1;
            }

            var errorInfos = new List<string>();

            var checkClose = workInfo.CommandOutput.Where(x => !string.IsNullOrEmpty(x) && x.Contains("_.close")).Any();

            if (!checkClose)
            {
                errorInfos.Add("The drawing did not finish processing and did not close properly.");
            }

            var checkWorkerIssues = workInfo.CommandOutput.Where(x => !string.IsNullOrEmpty(x) && x.Contains("Error getting"));
            if (checkWorkerIssues.Any())
            {
                errorInfos.AddRange(checkWorkerIssues);
            }

            // Exception in 
            var checkExceptionIn = workInfo.CommandOutput.Where(x => !string.IsNullOrEmpty(x) && x.Contains("Exception in"));

            if (checkExceptionIn.Any())
            {
                errorInfos.Add("The drawing may have data shortcuts that are broken.");
            }

            if (ExitCode != 0)
            {
                errorInfos.Add("The Civil 3D process did not exit properly.");
            }

            HasErrors = !checkClose ||
                        checkWorkerIssues.Any() ||
                        workInfo.Result != C3DRWorkerResult.Succeed ||
                        checkExceptionIn.Any() ||
                        ExitCode != 0;

            ErrorInformation = string.Join("\n", errorInfos);
        }

        public string GetString()
        {
            var lines = new List<string>();

            lines.Add(Name);
            lines.Add(FullName);
            lines.Add(Folder);
            lines.Add("Error Type: " + ErrorType);
            lines.Add("Command Output: ");
            lines.Add(CmdOutput);
            lines.Add("Has Errors: " + HasErrors.ToString());
            lines.Add("Error Info: " + ErrorInformation);
            lines.Add("Exit Code: " + ExitCode.ToString());
            return string.Join("\n", lines);
        }
    }
}
