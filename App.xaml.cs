using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;
using WpfSingleInstanceByEventWaitHandle;

namespace SoundKiller
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            WpfSingleInstance.Make();

            MainWindow wnd = new MainWindow();
            wnd.InitializeComponent();
            wnd.Show();

            base.OnStartup(e);
        }
    }
}
