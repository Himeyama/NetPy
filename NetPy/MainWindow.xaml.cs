using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Python.Runtime;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;


namespace NetPy
{
    public sealed partial class MainWindow : Window
    {
        static string logFileName = Path.GetTempFileName();
        public string pySource = "";
        public string poetryLibPath = "";
        public App app = null!;

        public MainWindow()
        {
            InitializeComponent();

            AppWindow.Closing += WindowClosing;

            ExtendsContentIntoTitleBar = true;
            SetTitleBar(WindowMove);

            PythonPrep.errorMessage = ErrorMessage;
            PythonPrep pythonPrep = PythonPrep.PythonInit();
            pySource = pythonPrep.pySource;
            poetryLibPath = pythonPrep.poetryLibPath;

            PyTest();

            TabView_AddTabButtonClick(TabsArea, null);
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

                //AppTitleTextBlock.Text = "作図アプリ";

                dynamic testlib = Py.Import("testlib");
                Debug("Execution Python Done!");
            }
        }

        public static string Base64Decode(string base64)
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

        void TabView_AddTabButtonClick(TabView sender, object args)
        {
            TabViewItem item = new()
            {
                IconSource = new SymbolIconSource
                {
                    Symbol = Symbol.Document
                },
                Header = "Untitled",
                //Content = new Tabs
                //{
                //    mainWindow = this
                //}
                Content = new OpenFilePicker
                {
                    mainWindow = this
                }
            };
            sender.TabItems.Add(item);
            sender.SelectedItem = item;
        }

        void TabView_TabClose(TabView sender, TabViewTabCloseRequestedEventArgs args)
        {
            sender.TabItems.Remove(args.Tab);
            if (sender.TabItems.Count == 0)
            {
                Process process = Process.GetCurrentProcess();
                process.Kill();
            }
        }
    }
}
