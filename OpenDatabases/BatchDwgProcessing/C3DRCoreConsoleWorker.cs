// This code comes from a variety of sources, including the 
// Autodesk Batch Save Utility (Standalone) and ScriptPro
// https://github.com/ADN-DevTech/ScriptPro

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DFWBIUG_C3D_Data_Shortcuts_2020.OpenDatabases.BatchDwgProcessing
{
    public class C3DRCoreConsoleWorker : BackgroundWorker
    {
        public ConcurrentQueue<string> DwgPaths { get; set; }
        public string C3DRProfile { get; set; }
        public string ScriptFullPath { get; set; }
        public int DwgPathsCount { get; set; }
        public bool IsDoneAddingToQueue { get; set; }

        public int TimeOut { get; set; }
        public int NoResponseTimeLimit { get; set; }
        public int NumberOfProcesses { get; set; }
        public int ReadLength { get; set; }
        public C3DRLockedSettings LockSettings { get; set; }
        public int NumberProcessed { get; set; }
        public int NumberFailed { get; set; }
        public int NumberSkipped { get; set; }
        public List<C3DRWorkInformation> WorkInformations { get; set; }
        public List<string> CommandLineOutputs { get; set; }

        public List<C3DRDwgError> Errors { get; set; }

        public C3DRCoreConsoleWorker(string profile, string scriptPath, int timeOut, int noResponseTimeLimit, int numberOfProcesses, int readLength)
        {
            Errors = new List<C3DRDwgError>();
            DwgPaths = new ConcurrentQueue<string>();
            IsDoneAddingToQueue = false;
            DwgPathsCount = 0;
            LockSettings = new C3DRLockedSettings();
            C3DRProfile = profile;
            ScriptFullPath = scriptPath;
            TimeOut = timeOut;
            NoResponseTimeLimit = noResponseTimeLimit;
            NumberOfProcesses = numberOfProcesses;
            ReadLength = readLength;
            CommandLineOutputs = new List<string>();
            WorkInformations = new List<C3DRWorkInformation>();
            for (int i = 0; i < numberOfProcesses; i++)
            {
                WorkInformations.Add(null);
            }
        }

        public void SuspendWork()
        {
            SetSuspendWork(true);
        }

        public void SetSuspendWork(bool shouldSuspend)
        {
            lock (LockSettings)
            {
                LockSettings.IsSuspended = shouldSuspend;
            }
        }

        public void ResumeWork()
        {
            RunWorkerAsync();
        }

        public bool IsSuspended
        {
            get
            {
                lock (LockSettings)
                {
                    return LockSettings.IsSuspended;
                }
            }
            set
            {
                SetSuspendWork(value);
            }
        }

        public int GetNumberOfUnhandled()
        {
            return Math.Max(0, DwgPaths.Count() - NumberProcessed - NumberFailed - NumberSkipped);
        }

        protected override void OnDoWork(DoWorkEventArgs e)
        {
            if (DwgPathsCount == 0 && IsSuspended)
            {
                SetSuspendWork(false);
                return;
            }

            if (IsSuspended)
            {
                SetSuspendWork(false);
            }

            while (!IsDoneAddingToQueue || DwgPaths.Any())
            {
                if (GetShouldSkipDwg())
                {
                    // Should skip the drawing,
                    // so go to the next one. 
                    continue;
                }

                // Something was wrong with the drawing, try to repair it. 
                if (!DwgPaths.TryDequeue(out string dwgPath))
                {
                    // We failed to get something, lets wait to see if it gets populated.
                    Thread.Sleep(500);
                    continue;
                }

                DwgPathsCount += 1;

                ProcessDrawing(dwgPath);

                // Clean up some space to allow the program to run. 
                if (DwgPathsCount >= 2000 && (DwgPathsCount % 1000) == 0)
                {
                    GC.Collect();
                }

                if (WorkerSupportsCancellation && CancellationPending)
                {
                    CancelProcesses();
                    break;
                }
                if (IsSuspended)
                {
                    DwgPathsCount += 1;
                }
            }

            if (IsSuspended)
            {
                return;
            }

            while (WorkInformations.Where(x => x != null).Any())
            {
                Thread.Sleep(500);
                for (int i = 0; i < WorkInformations.Count; i++)
                {
                    var workInfo = WorkInformations[i];
                    if (workInfo == null || workInfo.Civil3DProcess == null)
                    {
                        // This work info isn't do anything, so lets skip it.
                        continue;
                    }

                    if (workInfo.HasProcessEnded(this))
                    {
                        ProcessDwgResult(workInfo, i);
                        continue;
                    }

                    workInfo.UpdateWaitTimes();
                }
            }
        }

        private bool GetShouldSkipDwg()
        {
            // This is here for a future feature to skip drawings
            // if a certain criteria are met. ie... in archive folder.
            return false;
        }

        // There needs to be code to get the current year running to get the accoreconsole path as well as the 
        // install path, as it might not be the C:\ drive. 
        public static string CoreConsoleEXEPath = @"C:\Program Files\Autodesk\AutoCAD 2020\accoreconsole.exe";

        private void ProcessDrawing(string dwgPath)
        {
            var workInfoLocation = GetAvailableWorkInformationLocation();
            var workInfo = WorkInformations[workInfoLocation];
            ProcessDwgResult(workInfo, workInfoLocation);

            WorkInformations[workInfoLocation] = new C3DRWorkInformation(dwgPath, DwgPathsCount);
            workInfo = WorkInformations[workInfoLocation];

            var processStartInfo = new ProcessStartInfo(CoreConsoleEXEPath);
            processStartInfo.CreateNoWindow = true;
            processStartInfo.UseShellExecute = false;
            processStartInfo.RedirectStandardOutput = true;
            processStartInfo.StandardOutputEncoding = Encoding.Unicode;

            var arguments = "/i \"" + dwgPath + "\" /s \"" + ScriptFullPath + "\" /p \"" + C3DRProfile + "\" /product \"" + "C3D" + "\" /l \"" + "en-US" + "\"";
            processStartInfo.Arguments = arguments;
            workInfo.Civil3DProcess = Process.Start(processStartInfo);
            workInfo.Civil3DProcess.BeginOutputReadLine();
            workInfo.Civil3DProcess.OutputDataReceived += new DataReceivedEventHandler(workInfo.ProcessCommandLineData);
            ReportProgress(DwgPathsCount, null);
        }

        private int GetAvailableWorkInformationLocation()
        {
            while (true)
            {
                for (int index = 0; index < WorkInformations.Count; index++)
                {
                    var workInfo = WorkInformations[index];
                    if (workInfo == null || workInfo.Civil3DProcess == null)
                    {
                        return index;
                    }

                    if (workInfo.HasProcessEnded(this))
                    {
                        return index;
                    }
                }
                // Lets give the process some down time before updating the wait times.
                Thread.Sleep(500);
                foreach (var workInfo in WorkInformations)
                {
                    workInfo?.UpdateWaitTimes();
                }
            }
        }

        private void ProcessDwgResult(C3DRWorkInformation workInfo, int index)
        {
            if (workInfo == null)
            {
                return;
            }

            Errors.Add(new C3DRDwgError(workInfo));

            if (workInfo.Civil3DProcess.ExitCode != 0)
            {

            }

            switch (workInfo.Result)
            {
                case C3DRWorkerResult.Succeed:
                    NumberProcessed += 1;
                    break;
                case C3DRWorkerResult.NoResponse:
                case C3DRWorkerResult.Timeout:
                case C3DRWorkerResult.None:
                    NumberFailed += 1;
                    workInfo.Civil3DProcess.Kill();
                    break;
                default:
                    break;
            }



            CommandLineOutputs.AddRange(workInfo.CommandOutput);

            workInfo?.Dispose();
            WorkInformations[index] = null;
        }

        private void CancelProcesses()
        {
            for (int i = 0; i < WorkInformations.Count; i++)
            {
                var workInfo = WorkInformations[i];

                if (workInfo.Civil3DProcess == null)
                {
                    workInfo.Dispose();
                    continue;
                }

                if (workInfo.Civil3DProcess.HasExited)
                {
                    workInfo.Result = C3DRWorkerResult.Succeed;
                    ProcessDwgResult(workInfo, i);
                    continue;
                }
                else
                {
                    workInfo.Civil3DProcess.Kill();
                }

                workInfo.Dispose();
                WorkInformations[i] = null;
            }
        }
    }

    public enum DwgResponse
    {
        [Description("Succeed")]
        Succeed = 0,
        [Description("Save Failed")]
        SaveFailed = 1,
        [Description("Not Responding")]
        NotResponding = 2,
        [Description("User Cancelled")]
        UserCancelled = 3,
        [Description("Open Failed")]
        OpenFailed = 3
    }
}
