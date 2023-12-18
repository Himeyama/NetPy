using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Python.Runtime;
using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace NetPy
{
    public sealed partial class Tabs : Page
    {
        public MainWindow mainWindow;

        public Tabs()
        {
            InitializeComponent();
            string currentDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string assetsPath = Path.Combine(currentDirectory, "Assets");
            string editorPage = Path.Join(assetsPath, "Editor.html");
            if (File.Exists(editorPage))
                Code.Source = new Uri(editorPage);
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            mainWindow = e.Parameter as MainWindow;
        }

        async void Open(object sender, RoutedEventArgs e)
        {
            TabViewItem tabViewItem = mainWindow.TabsArea.SelectedItem as TabViewItem;
            Frame frame = tabViewItem.Content as Frame;
            string fileName = await OpenFilePicker.AsyncOpenFilenamePicker(frame, mainWindow);
        }

        async Task<string> GetEditorValue()
        {
            string base64 = await Code.ExecuteScriptAsync("btoa(unescape(encodeURIComponent(editor.getValue())))");
            base64 = base64.Substring(1, base64.Length - 2);
            return MainWindow.Base64Decode(base64);
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

                sys.path.append(mainWindow.poetryLibPath);
                sys.path.append(mainWindow.pySource);
                sys.path.append(pyGraphPath);

                try
                {
                    dynamic graph = Py.Import(basename);
                }
                catch (Exception ex)
                {
                    MainWindow.Debug(ex.Message);
                }
            }

            if (File.Exists(svgPath))
                Graph.Source = new Uri(svgPath);
        }
    }
}
