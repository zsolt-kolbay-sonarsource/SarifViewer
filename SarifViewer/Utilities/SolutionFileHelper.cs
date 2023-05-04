using System.IO;
using System;
using System.Runtime.InteropServices;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.OLE.Interop;
using System.Linq;
using System.Threading.Tasks;

namespace SarifViewer.Utilities;

public static class SolutionFileHelper
{
    public static async Task OpenSourceCodeInVisualStudio(string sourceFilePath, int lineNumber)
    {
        if (string.IsNullOrEmpty(sourceFilePath))
        {
            throw new ArgumentException("The source file path must not be null or empty.", nameof(sourceFilePath));
        }

        if (lineNumber < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(lineNumber), "The line number must be greater than or equal to 1.");
        }

        if (!File.Exists(sourceFilePath))
        {
            throw new FileNotFoundException("The source file was not found.", sourceFilePath);
        }

        string solutionPath = FindSolutionFileOfSourceCode(sourceFilePath);
        if (solutionPath == null)
        {
            throw new IOException($"Failed to find solution file for source file '{sourceFilePath}'.");
        }

        DTE2 dte = GetRunningVisualStudioInstanceWithSolution(solutionPath);

        if (dte == null)
        {
            Type visualStudioType = Type.GetTypeFromProgID("VisualStudio.DTE", true);
            dte = (DTE2)Activator.CreateInstance(visualStudioType);
            dte.MainWindow.Visible = true;
            dte.UserControl = true;
            dte.Solution.Open(solutionPath);

            while (!dte.Solution.IsOpen)
            {
                await Task.Delay(100);
            }
        }

        var window = dte.ItemOperations.OpenFile(sourceFilePath);

        bool sourceFileLoaded = false;
        string sourceFileFullPath = Path.GetFullPath(sourceFilePath);

        while (!sourceFileLoaded)
        {
            foreach (Window wnd in dte.Windows)
            {
                if (wnd.Document != null && string.Equals(Path.GetFullPath(wnd.Document.FullName), sourceFileFullPath, StringComparison.OrdinalIgnoreCase))
                {
                    sourceFileLoaded = true;
                    break;
                }
            }
            await Task.Delay(100);
        }

        window.Activate();
        TextSelection selection = (TextSelection)dte.ActiveDocument.Selection;
        selection.GotoLine(lineNumber, true);

        dte.MainWindow.Activate();
    }

    private static string FindSolutionFileOfSourceCode(string sourceCodeFilePath)
    {
        if (string.IsNullOrEmpty(sourceCodeFilePath))
        {
            throw new ArgumentException("The file path must not be null or empty.", nameof(sourceCodeFilePath));
        }

        DirectoryInfo currentDirectory = new DirectoryInfo(Path.GetDirectoryName(sourceCodeFilePath));

        while (currentDirectory != null)
        {
            FileInfo[] solutionFiles = currentDirectory.GetFiles("*.sln");

            if (solutionFiles.Any())
            {
                return solutionFiles.First().FullName;
            }

            currentDirectory = currentDirectory.Parent;
        }

        return null;
    }

    private static DTE2 GetRunningVisualStudioInstanceWithSolution(string solutionPath)
    {
        string targetSolutionFullPath = Path.GetFullPath(solutionPath);

        // Get all running Visual Studio processes
        var visualStudioProcesses = System.Diagnostics.Process.GetProcessesByName("devenv");

        foreach (var process in visualStudioProcesses)
        {
            try
            {
                // Get the DTE object from the running Visual Studio process
                object dteObject = GetDTE(process.Id);

                if (dteObject is DTE2 dte && dte.Solution != null && dte.Solution.IsOpen)
                {
                    string openSolutionPath = Path.GetFullPath(dte.Solution.FullName);

                    if (string.Equals(openSolutionPath, targetSolutionFullPath, StringComparison.OrdinalIgnoreCase))
                    {
                        return dte;
                    }
                }
            }
            catch (COMException)
            {
                // Ignore exceptions and continue checking the remaining processes
            }
        }

        return null;
    }

    private static _DTE GetDTE(int processId)
    {
        IRunningObjectTable runningObjectTable;
        IEnumMoniker monikerEnumerator;
        IMoniker[] monikers = new IMoniker[1];

        GetRunningObjectTable(0, out runningObjectTable);
        runningObjectTable.EnumRunning(out monikerEnumerator);
        monikerEnumerator.Reset();

        while (monikerEnumerator.Next(1, monikers, out uint numFetched) == 0)
        {
            IBindCtx ctx;
            CreateBindCtx(0, out ctx);

            string runningObjectName;
            monikers[0].GetDisplayName(ctx, null, out runningObjectName);

            object runningObjectVal;
            runningObjectTable.GetObject(monikers[0], out runningObjectVal);

            if (runningObjectVal is _DTE && runningObjectName.StartsWith("!VisualStudio"))
            {
                int currentProcessId = int.Parse(runningObjectName.Split(':')[1]);

                if (currentProcessId == processId)
                {
                    return (_DTE)runningObjectVal;
                }
            }
        }

        return null;
    }

    [DllImport("ole32.dll")]
    private static extern int CreateBindCtx(int reserved, out IBindCtx ppbc);

    [DllImport("ole32.dll")]
    private static extern int GetRunningObjectTable(int reserved, out IRunningObjectTable prot);
}
