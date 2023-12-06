using Microsoft.UI.Xaml.Controls;
using Python.Runtime;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;

namespace NetPy
{
    public class NoopFormatter : IFormatter
    {
        public object Deserialize(Stream s) => throw new NotImplementedException();
        public void Serialize(Stream s, object o) { }
        public SerializationBinder Binder { get; set; }
        public StreamingContext Context { get; set; }
        public ISurrogateSelector SurrogateSelector { get; set; }
    }

    internal class PythonPrep
    {
        public string pySource { get; set; }
        public string poetryLibPath { get; set; }

        public string PYTHON_DLL { get; set; } = "";
        public string PYTHON_HOME { get; set; } = "";
        public Type? FORMATTER_TYPE { get; set; } = null;
        public static TextBlock errorMessage { get; set; }

        public PythonPrep() { }
        static void Debug(string text) => MainWindow.Debug(text);

        static void ErrorMessage(string text)
        {
            errorMessage.Text = text;
        }

        /// <summary>
        /// Poetry がインストールされているかをチェックする
        /// </summary>
        static bool CheckPoetryInstalled(string execPython)
        {
            if (!File.Exists(execPython)) return false;
            Process process = new();
            process.StartInfo.FileName = execPython;
            process.StartInfo.Arguments = "-m poetry -V";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.CreateNoWindow = true;
            process.Start();
            string poetryVersion = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            Debug($"Checking poetry... {poetryVersion}");
            Match match = Regex.Match(poetryVersion, @"[pP]oetry \([vV]ersion (\d+\.\d+\.\d+)\)");
            return match.Success;
        }

        static string? GetPythonEngine()
        {
            string? PYTHON_ENGINE_PARENT = GetPythonEngineParent();
            if (PYTHON_ENGINE_PARENT == null) return null;

            string? PYTHON_ENGINE = "";
            foreach (string pythonEngine in Directory.GetDirectories(PYTHON_ENGINE_PARENT, "Python*"))
            {
                string execPython = Path.Combine(pythonEngine, "python.exe");
                Debug($"Looking for the Python executable file: {pythonEngine}");
                if (CheckPoetryInstalled(execPython))
                {
                    PYTHON_ENGINE = pythonEngine;
                    break;
                }
                else
                {
                    Debug("Poetry is not installed");
                }
            }
            if (PYTHON_ENGINE == "")
            {
                Debug($"Python is not exist!");
                ErrorMessage("Python is not installed!");
                return null;
            }
            Debug($"Found Python directory, {PYTHON_ENGINE}");
            return PYTHON_ENGINE;
        }

        static string? GetPythonEngineParent()
        {
            string PYTHON_ENGINE_PARENT = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                @"Programs\Python"
            );
            if (!Directory.Exists(PYTHON_ENGINE_PARENT))
            {
                Debug($"Python is not exist!, {PYTHON_ENGINE_PARENT}");
                ErrorMessage("Python is not installed!");
                return null;
            }
            return PYTHON_ENGINE_PARENT;
        }

        static string? GetPythonDLL(string PYTHON_ENGINE)
        {
            string? PYTHON_DLL = null;
            Regex reg = new(@"^[pP]ython\d{3}\.dll$");
            foreach (string pythonDll in Directory.GetFiles(PYTHON_ENGINE).Where(path => reg.IsMatch(Path.GetFileName(path))))
            {
                PYTHON_DLL = pythonDll;
                break;
            }
            if (PYTHON_DLL == null)
            {
                Debug($"Python shared library is not found.");
                ErrorMessage($"Python shared library is not found.");
                return null;
            }
            Debug($"Found Python shared library, {PYTHON_DLL}");

            return PYTHON_DLL;
        }

        public bool PythonInitCore()
        {
            string? PYTHON_ENGINE = GetPythonEngine();
            if (PYTHON_ENGINE == null) return false;
            string? _PYTHON_DLL = GetPythonDLL(PYTHON_ENGINE);
            if (_PYTHON_DLL == null) return false;
            //Runtime.PythonDLL = PYTHON_DLL;
            PYTHON_DLL = _PYTHON_DLL;
            Debug("Setting a python home path...");
            try
            {
                PYTHON_HOME = PYTHON_ENGINE;
            }
            catch (Exception ex)
            {
                Debug(ex.Message);
                ErrorMessage($"Python isn't installed.\nPlease install and set the path.");
                return false;
            }
            Debug("Setting a formatter type...");
            FORMATTER_TYPE = typeof(NoopFormatter);
            Debug("Settings python...");
            return true;
        }

        public static string GetPythonSourcePath()
        {
            string currentDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string assetsPath = Path.Combine(currentDirectory, "Assets");
            string pySource = Path.Combine(assetsPath, @"PySource\src");
            return pySource;
        }

        public static PythonPrep? PythonInit()
        {
            PythonPrep pythonPrep = new();
            try
            {
                Debug("Initializing python...");
                bool pythonInit = pythonPrep.PythonInitCore();
                if (pythonInit)
                {
                    Runtime.PythonDLL = pythonPrep.PYTHON_DLL;
                    PythonEngine.PythonHome = pythonPrep.PYTHON_HOME;
                    RuntimeData.FormatterType = pythonPrep.FORMATTER_TYPE;
                }
            }
            catch (Exception ex)
            {
                Debug(ex.Message);
                ErrorMessage("Initialization Error\n" + ex.Message);
            }

            pythonPrep.pySource = GetPythonSourcePath();
            pythonPrep.poetryLibPath = pythonPrep.GetPoetryLibPath();

            return pythonPrep;
        }

        string ExecPoetry(string args, string works)
        {
            string currentDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string assetsPath = Path.Combine(currentDirectory, "Assets");
            string localAppData = Environment.GetEnvironmentVariable("LocalAppData");
            string poetryPath = Path.Combine(localAppData, $"{PYTHON_HOME}\\Scripts\\poetry.exe");
            if (!File.Exists(poetryPath))
            {
                ErrorMessage("Poetry is not installed!");
                Debug(errorMessage.Text);
                return "NoExistPoetry";
            }
            Process process = new();
            process.StartInfo.EnvironmentVariables.Add("PYTHONIOENCODING", "utf-8");
            process.StartInfo.FileName = poetryPath;
            process.StartInfo.Arguments = "" + args;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.WorkingDirectory = Path.Combine(assetsPath, works);
            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            Debug("poetry " + process.StartInfo.Arguments + ", dir: " + process.StartInfo.WorkingDirectory);
            Debug(output + "\n");

            return output;
        }

        string GetPythonPath()
        {
            string output = ExecPoetry("env info -p", "PySource");
            if (output == "NoExistPoetry")
                return "";
            ExecPoetry("install", "PySource");
            if (output == "")
            {
                output = ExecPoetry("env info -p", "PySource");
            }
            string pythonEnvPath = output.Replace("\r", "").Replace("\n", "");
            return pythonEnvPath;
        }

        public string GetPoetryLibPath()
        {
            string path = "";
            try
            {
                string pySourcePath = GetPythonPath();
                if (pySourcePath != "")
                {
                    path = Path.Combine(pySourcePath, @"Lib\site-packages");
                }
            }
            catch (Exception ex)
            {
                ErrorMessage(ex.Message);
                Debug(errorMessage.Text);
            }
            return path;
        }
    }
}
