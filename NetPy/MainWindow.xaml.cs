using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Python.Runtime;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;


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

            AppWindow.Closing += WindowClosing;

            ExtendsContentIntoTitleBar = true;
            SetTitleBar(AppTitleBar);

            PythonPrep.errorMessage = ErrorMessage;
            PythonPrep pythonPrep = PythonPrep.PythonInit();
            pySource = pythonPrep.pySource;
            poetryLibPath = pythonPrep.poetryLibPath;

            string currentDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string assetsPath = Path.Combine(currentDirectory, "Assets");
            string editorPage = Path.Join(assetsPath, "Editor.html");
            if (File.Exists(editorPage))
                Code.Source = new Uri(editorPage);

            PyTest();
        }

        void WindowClosing(AppWindow sender, AppWindowClosingEventArgs args)
        {
            PythonEngine.Shutdown();
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

                AppTitleTextBlock.Text = "作図アプリ";

                dynamic testlib = Py.Import("testlib");
                Debug("Execution Python Done!");
            }

        }

        string Base64Decode(string base64)
        {
            string base64String = base64;
            // 4の倍数になるようにパディングを追加
            int mod4 = base64String.Length % 4;
            if (mod4 > 0)
                base64String += new string('=', 4 - mod4);
            // Base64文字列をデコード
            byte[] bytes = Convert.FromBase64String(base64String);
            string decodedString = Encoding.UTF8.GetString(bytes);
            return decodedString;
        }

        async Task<string> GetEditorValue()
        {
            string base64 = await Code.ExecuteScriptAsync("btoa(unescape(encodeURIComponent(editor.getValue())))");
            base64 = base64.Substring(1, base64.Length - 2);
            return Base64Decode(base64);
        }

        async void Preview(object sender, RoutedEventArgs e)
        {
            string code = await GetEditorValue();

            string tmp = Path.GetTempFileName();

            string tmpPath = Path.GetTempPath();
            string pyGraphPath = Path.Combine(tmpPath, "pyGraph");
            if (!Directory.Exists(pyGraphPath))
                Directory.CreateDirectory(pyGraphPath);
            string basename = Path.GetFileNameWithoutExtension(tmp);
            string pythonCodePath = Path.Combine(pyGraphPath, basename + ".py");
            string svgPath = Path.Combine(pyGraphPath, $"{basename}.svg");

            File.AppendAllText(tmp, "import matplotlib.pyplot as plt\n");
            File.AppendAllText(tmp, "import numpy as np\n");
            File.AppendAllText(tmp, code);
            File.AppendAllText(tmp, $"\nplt.savefig(r\"{svgPath}\")\n");

            if (File.Exists(tmp))
                File.Move(tmp, pythonCodePath);
            else
                return;

            using (Py.GIL())
            {
                dynamic sys = Py.Import("sys");

                sys.path.append(poetryLibPath);
                sys.path.append(pySource);
                sys.path.append(pyGraphPath);

                try
                {
                    dynamic graph = Py.Import(basename);
                }
                catch (Exception ex)
                {
                    Debug(ex.Message);
                }
            }

            if (File.Exists(svgPath))
                Graph.Source = new Uri(svgPath);
        }
    }
}
