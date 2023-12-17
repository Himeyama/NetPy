using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.System;

namespace NetPy
{
    public class FileInfo
    {
        public string Icon1 { get; set; }
        public string Icon2 { get; set; }
        public string IconColor1 { get; set; }
        public string IconColor2 { get; set; }
        public string FileName { get; set; }
    }

    public partial class OpenFilePicker : Page
    {
        static string _saveTo;
        static TaskCompletionSource<bool> _tcs = new();
        public static string saveTo
        {
            get { return _saveTo; }
            set
            {
                if (_saveTo != value)
                    _saveTo = value;
                _tcs.TrySetResult(true);
            }
        }

        string presentWorkingDir = @"C:\";
        public MainWindow mainWindow;
        //public TabViewItem tabViewItem;
        //public TabView tabView;
        Stack<string> backStack = new();
        Stack<string> forwardStack = new();
        IDictionary<string, IList<string>> FileTypeChoices = new Dictionary<string, IList<string>> { };
        IList<string> defaultFileType = new List<string> { };
        public string selectedPath = string.Empty;
        public string selectedContent = string.Empty;

        public static async Task<string> AsyncOpenFilenamePicker(Frame frame, MainWindow mainWindow)
        {
            frame.Navigate(typeof(OpenFilePicker), mainWindow);
            await WaitForChange();
            return saveTo;
        }

        public OpenFilePicker()
        {
            InitializeComponent();

            FileTypeChoices.Add("テキスト ドキュメント", new List<string>() { ".txt" });
            FileTypeChoices.Add("すべてのファイル", new List<string>() { "*" });

            bool defaultFileTypeFlag = true;
            foreach (string key in FileTypeChoices.Keys)
            {
                IList<string> values = FileTypeChoices[key];
                if (defaultFileTypeFlag)
                {
                    defaultFileType = values;
                    defaultFileTypeFlag = false;
                }
                IList<string> avalues = new List<string>(values).Select(s =>
                {
                    if (s == "*") return "*.*";
                    return "*" + s;
                }).ToList();
                FileTypes.Items.Add($"{key} ({string.Join(",", avalues)})");
            }
            FileTypes.SelectedIndex = 0;
            FileTypes.SelectionChanged += FileTypes_SelectionChanged;

            AddressBar.Text = presentWorkingDir;
            Refresh();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            mainWindow = e.Parameter as MainWindow;
        }

        void FileTypes_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox comboBox = sender as ComboBox;
            string selected = (string)comboBox.SelectedItem;
            foreach (string key in FileTypeChoices.Keys)
            {
                IList<string> values = FileTypeChoices[key];
                IList<string> avalues = new List<string>(values).Select(s =>
                {
                    if (s == "*") return "*.*";
                    return "*" + s;
                }).ToList();
                string displayName = $"{key} ({string.Join(",", avalues)})";
                if (selected == displayName)
                {
                    defaultFileType = values;
                    Refresh();
                    return;
                }
            }
        }

        async void _FileItem_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            string fileName = ((FileInfo)((ListView)sender).SelectedValue).FileName;
            string dirPath = presentWorkingDir;
            string newDirPath = Path.Combine(dirPath, fileName).TrimEnd('\\') + "\\";

            string fullPath = Path.Combine(dirPath, fileName);
            if (File.Exists(fullPath))
            {
                // 処理
                ContentDialog dialog = new();

                // XamlRoot must be set in the case of a ContentDialog running in a Desktop app
                dialog.XamlRoot = this.XamlRoot;
                dialog.Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style;
                dialog.Title = "Confirm Save As";
                dialog.PrimaryButtonText = "Yes";
                dialog.SecondaryButtonText = "No";
                dialog.DefaultButton = ContentDialogButton.Secondary;
                dialog.Content = new StackPanel();
                ((StackPanel)dialog.Content).Children.Add(new TextBlock
                {
                    Text = "Do you want to overwrite the file?"
                });

                ContentDialogResult result = await dialog.ShowAsync();
                if (result == ContentDialogResult.Primary)
                {
                    File.WriteAllText(fullPath, "SAVE");
                }

                return;
            }

            if (!Directory.Exists(newDirPath))
                return;

            backStack.Push(presentWorkingDir);
            forwardStack.Clear();
            ForwardButton.IsEnabled = false;
            BackButton.IsEnabled = true;

            AddressBar.Text = newDirPath;
            presentWorkingDir = newDirPath;
            ChangeParentDirectoryButton.IsEnabled = true;
            Refresh();
        }

        async void FileItem_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            string fileName = ((FileInfo)((ListView)sender).SelectedValue).FileName;
            string dirPath = presentWorkingDir;
            string newDirPath = Path.Combine(dirPath, fileName).TrimEnd('\\') + "\\";

            string fullPath = Path.Combine(dirPath, fileName);
            if (File.Exists(fullPath))
            {
                selectedPath = fullPath;
                selectedContent = File.ReadAllText(fullPath);
                ((Frame)((TabViewItem)mainWindow.TabsArea.SelectedItem).Content).GoBack();
                saveTo = fullPath;
                return;
            }

            if (!Directory.Exists(newDirPath))
                return;

            backStack.Push(presentWorkingDir);
            forwardStack.Clear();
            ForwardButton.IsEnabled = false;
            BackButton.IsEnabled = true;

            AddressBar.Text = newDirPath;
            presentWorkingDir = newDirPath;
            ChangeParentDirectoryButton.IsEnabled = true;
            Refresh();
        }

        void Refresh()
        {
            ChangeParentDirectoryButton.IsEnabled = presentWorkingDir != string.Empty;

            string dirPath = presentWorkingDir;
            if (dirPath == string.Empty)
            {
                FileList.Items.Clear();
                string[] drives = Directory.GetLogicalDrives();
                foreach (string drive in drives)
                {
                    FileList.Items.Add(
                        new FileInfo
                        {
                            FileName = drive,
                            Icon1 = "\xEDA2",
                            Icon2 = "\xEDA2",
                            IconColor1 = "#9E9E9E",
                            IconColor2 = "#F5F5F5"
                        }
                    );
                }
                return;
            }

            string[] directories = Directory.GetDirectories(dirPath);
            string[] files = Directory.GetFiles(dirPath);


            FileList.Items.Clear();
            foreach (string dir in directories)
            {
                DirectoryInfo dirInfo = new(dir);
                if ((dirInfo.Attributes & FileAttributes.System) != FileAttributes.System && (dirInfo.Attributes & FileAttributes.Hidden) != FileAttributes.Hidden)
                    FileList.Items.Add(
                        new FileInfo
                        {
                            FileName = Path.GetFileName(dir),
                            Icon1 = "\xE8D5",
                            Icon2 = "\xE8B7",
                            IconColor1 = "#FFCF48",
                            IconColor2 = "#FFE0B2"
                        }
                    );
            }
            foreach (string file in files)
            {
                if (!IsFileTypeIncluded(file))
                    continue;

                FileAttributes attributes = File.GetAttributes(file);
                if ((attributes & FileAttributes.System) != FileAttributes.System && (attributes & FileAttributes.Hidden) != FileAttributes.Hidden)
                    FileList.Items.Add(
                        new FileInfo
                        {
                            FileName = Path.GetFileName(file),
                            Icon1 = "\xE729",
                            Icon2 = "\xE7C3",
                            IconColor1 = "#9E9E9E",
                            IconColor2 = "#F5F5F5"
                        }
                    );
            }
        }

        bool IsFileTypeIncluded(string fileName)
        {
            if (defaultFileType.Count == 1 && defaultFileType[0] == "*")
                return true;

            string extension = Path.GetExtension(fileName);
            return defaultFileType.Contains(extension);
        }

        async void AddressBar_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Enter)
            {
                // Enterキーが押されたときの処理をここに書く
                string dirPath = AddressBar.Text;
                if (!Directory.Exists(dirPath))
                {
                    ErrorDialog($"'{dirPath}' は見つかりません。綴りを確認して再実行してください。");
                    return;
                }

                backStack.Push(presentWorkingDir);
                forwardStack.Clear();
                ForwardButton.IsEnabled = false;
                BackButton.IsEnabled = true;

                presentWorkingDir = dirPath.TrimEnd('\\') + "\\";
                ChangeParentDirectoryButton.IsEnabled = true;
                Refresh();
            }
        }

        void RefreshDirectory(object sender, RoutedEventArgs e) => Refresh();

        void ChangeParentDirectory(object sender, RoutedEventArgs e)
        {
            if (string.Empty == presentWorkingDir)
            {
                ErrorDialog("親のディレクトリを表示できません。");
                return;
            }
            if (!Directory.Exists(presentWorkingDir))
            {
                ErrorDialog($"現在のディレクトリ '{presentWorkingDir}' が存在しません。");
                return;
            }

            DirectoryInfo dirInfo = new(presentWorkingDir);
            DirectoryInfo? newDirInfo = dirInfo.Parent;
            string rootPath = Path.GetPathRoot(presentWorkingDir);
            ChangeParentDirectoryButton.IsEnabled = true;
            if (null == newDirInfo)
            {
                if (presentWorkingDir.TrimEnd('\\') == rootPath.TrimEnd('\\'))
                {
                    presentWorkingDir = "";
                    AddressBar.Text = "Computer";
                    ChangeParentDirectoryButton.IsEnabled = false;
                    Refresh();
                }
                else
                {
                    ErrorDialog($"'{presentWorkingDir}' の親ディレクトリを取得できません。");
                }
                return;
            }
            backStack.Push(presentWorkingDir);
            forwardStack.Clear();
            ForwardButton.IsEnabled = false;
            BackButton.IsEnabled = true;

            presentWorkingDir = newDirInfo.FullName;
            AddressBar.Text = presentWorkingDir;
            Refresh();
        }

        async void ErrorDialog(string message)
        {
            StackPanel title = new() { Orientation = Orientation.Horizontal };
            title.Children.Add(new FontIcon { Glyph = "\xEB90", Margin = new Thickness(0, 4, 0, 0), Foreground = new SolidColorBrush(ColorHelper.FromArgb(255, 232, 17, 35)) });
            title.Children.Add(new TextBlock { Text = "エラー", Margin = new Thickness(8, 0, 0, 0) });
            ContentDialog dialog = new()
            {
                Title = title,
                Content = message,
                CloseButtonText = "OK"
            };

            dialog.XamlRoot = mainWindow.Content.XamlRoot;
            ContentDialogResult result = await dialog.ShowAsync();
        }

        void Back(object sender, RoutedEventArgs e)
        {
            if (backStack.Count == 0) return;
            forwardStack.Push(presentWorkingDir);
            string backAddress = backStack.Pop();
            ForwardButton.IsEnabled = true;
            BackButton.IsEnabled = backStack.Count > 0;
            AddressBar.Text = backAddress;
            presentWorkingDir = backAddress;
            Refresh();
        }

        void Forward(object sender, RoutedEventArgs e)
        {
            if (forwardStack.Count == 0) return;
            backStack.Push(presentWorkingDir);
            string forwardAddress = forwardStack.Pop();
            BackButton.IsEnabled = true;
            ForwardButton.IsEnabled = forwardStack.Count > 0;
            AddressBar.Text = forwardAddress;
            presentWorkingDir = forwardAddress;
            Refresh();
        }

        void FileItem_Tapped(object sender, TappedRoutedEventArgs e)
        {
            string fileName = ((FileInfo)((ListView)sender).SelectedValue).FileName;
            string filePath = Path.Combine(presentWorkingDir, fileName);
            if (File.Exists(filePath))
                FileName.Text = fileName;
        }

        public static async Task WaitForChange()
        {
            await _tcs.Task;
            _tcs = new TaskCompletionSource<bool>();
        }
    }
}
