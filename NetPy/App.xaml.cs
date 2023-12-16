using Microsoft.UI.Xaml;

namespace NetPy
{
    public partial class App : Application
    {
        private Window mainWindow { get; set; }
        public App()
        {
            InitializeComponent();
        }

        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            mainWindow = new MainWindow
            {
                app = this
            };
            mainWindow.Activate();
        }
    }
}
