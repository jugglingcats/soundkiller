using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using CoreAudioApi;
using System.Diagnostics;
using System.Management;
using Microsoft.Win32;
using System.Security;

namespace SoundKiller
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const String REG_KEY = "com.akirkpatrick.SoundKiller";
        public MainWindow()
        {
            InitializeComponent();

            // Visibility = Visibility.Hidden;

            Microsoft.Win32.SystemEvents.SessionSwitch += new Microsoft.Win32.SessionSwitchEventHandler(SessionSwitch);

            try
            {
                RegistryKey rk = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
                rk.SetValue(REG_KEY, System.Reflection.Assembly.GetExecutingAssembly().Location);
                rk.Flush();
                rk.Close();
            }
            catch (UnauthorizedAccessException ex)
            {
            }
            catch (SecurityException ex)
            {
            }
        }

        void SessionSwitch(object sender, Microsoft.Win32.SessionSwitchEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("Got session switch: " + e.Reason);
            switch (e.Reason)
            {
                case SessionSwitchReason.ConsoleDisconnect:
                    EachSession(s => { s.SimpleAudioVolume.Mute = true; });
                    break;

                case SessionSwitchReason.ConsoleConnect:
                    EachSession(s => { s.SimpleAudioVolume.Mute = false; });
                    break;
            }
        }

        private IEnumerable<AudioSessionControl> ActiveSessions
        {
            get
            {
                List<AudioSessionControl> retval = new List<AudioSessionControl>();
                MMDeviceEnumerator DevEnum = new MMDeviceEnumerator();
                MMDeviceCollection devices=DevEnum.EnumerateAudioEndPoints(EDataFlow.eRender, EDeviceState.DEVICE_STATE_ACTIVE);
                for (int j=0; j<devices.Count; j++) 
                {
                    MMDevice device = devices[j]; // DevEnum.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eMultimedia);
                    // Note the AudioSession manager did not have a method to enumerate all sessions in windows Vista
                    // this will only work on Win7 and newer.

                    for (int i = 0; i < device.AudioSessionManager.Sessions.Count; i++)
                    {
                        AudioSessionControl session = device.AudioSessionManager.Sessions[i];
                        if (session.State == AudioSessionState.AudioSessionStateActive)
                            retval.Add(session);
                    }
                }
                return retval;
            }
        }

        private string GetOwner(uint p)
        {
            string query = "Select * From Win32_Process Where ProcessID = " + p;
            ManagementObjectSearcher searcher = new ManagementObjectSearcher(query);
            ManagementObjectCollection processList = searcher.Get();

            foreach (ManagementObject obj in processList)
            {
                string[] argList = new string[] { string.Empty, string.Empty };
                int returnVal = Convert.ToInt32(obj.InvokeMethod("GetOwner", argList));
                if (returnVal == 0)
                {
                    // return DOMAIN\user
                    return /* argList[1] + "\\" + */ argList[0];
                }
            }
            return "unknown";
        }

        private void SessionSelected(object sender, SelectionChangedEventArgs e)
        {
            //this.propertyGrid.SelectedObject = ((SessionInfo) e.AddedItems[0]).Session;
        }

        private delegate void Fun(AudioSessionControl s);

        private void EachSession(Fun fn) 
        {
            foreach (AudioSessionControl s in ActiveSessions)
            {
                fn(s);
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            this.Hide();
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }

    public class SessionInfo
    {
        public String Name { get; set; }
        public AudioSessionControl Session { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }
}
