using Microsoft.UI.Xaml;
using Python.Runtime;
using System;
using System.Collections.Generic;
using System.IO;


namespace NetPy
{
    public sealed partial class MainWindow : Window
    {
        static string logFileName = Path.GetTempFileName();
        string pySource = "";
        string poetryLibPath = "";

        public MainWindow()
        {
            InitializeComponent();

            ExtendsContentIntoTitleBar = true;
            SetTitleBar(AppTitleBar);

            PythonPrep.errorMessage = ErrorMessage;
            PythonPrep pythonPrep = PythonPrep.PythonInit();
            pySource = pythonPrep.pySource;
            poetryLibPath = pythonPrep.poetryLibPath;

            PyTest();
        }

        public static void Debug(string text)
        {
            File.AppendAllText(logFileName, text + "\n");
        }

        void ClickExit(object sender, RoutedEventArgs e)
        {
            Close();
        }

        public static void AddEnvPath(params string[] paths)
        {
            string[] envPathsArray = Environment.GetEnvironmentVariable("PATH").Split(Path.PathSeparator);
            List<string> envPaths = new(envPathsArray);
            foreach (var path in paths)
            {
                if (path.Length > 0 && !envPaths.Contains(path))
                {
                    envPaths.Insert(0, path);
                }
            }
            Environment.SetEnvironmentVariable("PATH", string.Join(Path.PathSeparator.ToString(), envPaths), EnvironmentVariableTarget.Process);
        }

        void PyTest()
        {
            if (poetryLibPath == "" || pySource == "")
                return;
            if (!PythonEngine.IsInitialized)
                PythonEngine.Initialize();

            Debug("Starting python...");
            using (Py.GIL())
            {
                dynamic sys = Py.Import("sys");

                sys.path.append(poetryLibPath);
                sys.path.append(pySource);

                PyString str = new("Hello, world");
                AppTitleTextBlock.Text = str.ToString();

                dynamic np = Py.Import("numpy");
                dynamic array = np.array(new int[] { 1, 2, 3 });

                dynamic testlib = Py.Import("testlib");
                Debug("Execution Python Done!");
            }

            PythonEngine.Shutdown();
        }
    }
}
