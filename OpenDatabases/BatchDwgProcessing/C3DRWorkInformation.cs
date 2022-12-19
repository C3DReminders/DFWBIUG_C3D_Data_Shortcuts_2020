using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DFWBIUG_C3D_Data_Shortcuts_2020.OpenDatabases.BatchDwgProcessing
{
    public class C3DRWorkInformation : IDisposable
    {
        public C3DRWorkerResult Result { get; set; }
        public List<string> CommandOutput { get; set; }
        public Process Civil3DProcess { get; set; }
        public int Index { get; set; }
        public int NoResponseTime { get; set; }
        public int ProcessTime { get; set; }
        public FileAttributes DwgFileAttributes { get; set; }
        public string DwgPath { get; set; }
        public DateTime DwgModifiedTime { get; set; }

        public C3DRWorkInformation(string dwgPath, int index)
        {
            DwgPath = dwgPath;
            Index = index;

            Result = C3DRWorkerResult.None;
            ProcessTime = 0;
            NoResponseTime = 0;

            CommandOutput = new List<string>();

            try
            {
                DwgModifiedTime = File.GetLastWriteTime(dwgPath);
            }
            catch (Exception ex)
            {
                CommandOutput.Add("Error getting DwgModifiedTime: " + ex.Message);
            }

            try
            {
                DwgFileAttributes = File.GetAttributes(dwgPath);
            }
            catch (Exception ex)
            {
                CommandOutput.Add("Error getting DwgFileAttributes: " + ex.Message);
            }
        }

        public void ProcessCommandLineData(object sender, DataReceivedEventArgs e)
        {
            CommandOutput.Add(e.Data);
        }

        public bool HasProcessEnded(C3DRCoreConsoleWorker parent)
        {
            if (Civil3DProcess.HasExited)
            {
                Result = C3DRWorkerResult.Succeed;
                return true;
            }

            if (NoResponseTime >= parent.NoResponseTimeLimit)
            {
                Result = C3DRWorkerResult.NoResponse;
                return true;
            }

            if (ProcessTime >= parent.TimeOut)
            {
                Result = C3DRWorkerResult.Timeout;
                return true;
            }

            return false;
        }

        public void UpdateWaitTimes()
        {
            if (Civil3DProcess == null)
            {
                return;
            }
            if (Civil3DProcess.HasExited)
            {
                return;
            }

            ProcessTime += 500;
            NoResponseTime = Civil3DProcess.Responding ? 0 : NoResponseTime + 500;
        }

        public void Dispose()
        {
            if (string.IsNullOrEmpty(DwgPath) || (DwgFileAttributes & FileAttributes.ReadOnly) == 0)
            {
                return;
            }

            File.SetAttributes(DwgPath, DwgFileAttributes);
            DwgPath = null;
        }
    }

    public enum C3DRWorkerResult
    {
        None = 0,
        Succeed = 1,
        NoResponse = 2,
        Timeout = 3
    }
}