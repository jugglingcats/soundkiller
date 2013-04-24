using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Threading;
using System.Diagnostics;
using System.Security.Principal;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using CoreAudioApi;

namespace AppMain
{
    static class Program
    {
        private const String APP_NAME = "com.akirkpatrick.SoundKiller";

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Debug.WriteLine("SoundKiller starting");

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Mutex MyApplicationMutex = new Mutex(true, APP_NAME);

            if (MyApplicationMutex.WaitOne(0, false))
            {
                Application.Run(new SoundKillerAppContext());
            }
            else
            {
                MessageBox.Show("SoundKiller is already running. To uninstall please run again as administrator.", "SoundKiller");
            }
        }
    }
    class SoundKillerAppContext : ApplicationContext
    {
        public SoundKillerAppContext()
        {
            Microsoft.Win32.SystemEvents.SessionSwitch += new Microsoft.Win32.SessionSwitchEventHandler(SessionSwitch);
        }

        void SessionSwitch(object sender, Microsoft.Win32.SessionSwitchEventArgs e)
        {
            Debug.WriteLine("Got session switch: " + e.Reason);
            try
            {
                switch (e.Reason)
                {
                    case SessionSwitchReason.ConsoleDisconnect:
                        EachSession(s => { SwitchSession(s, true); });
                        break;

                    case SessionSwitchReason.ConsoleConnect:
                        EachSession(s => { SwitchSession(s, false); });
                        break;
                }
            }
            catch (Exception)
            {
                // don't fail whatever
            }
        }

        private void SwitchSession(AudioSessionControl s, bool p)
        {
            s.SimpleAudioVolume.Mute = p;
        }

        private IEnumerable<AudioSessionControl> ActiveSessions
        {
            get
            {
                List<AudioSessionControl> retval = new List<AudioSessionControl>();
                MMDeviceEnumerator DevEnum = new MMDeviceEnumerator();
                MMDeviceCollection devices = DevEnum.EnumerateAudioEndPoints(EDataFlow.eRender, EDeviceState.DEVICE_STATE_ACTIVE);
                for (int j = 0; j < devices.Count; j++)
                {
                    MMDevice device = devices[j]; // DevEnum.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eMultimedia);
                    // Note the AudioSession manager did not have a method to enumerate all sessions in windows Vista
                    // this will only work on Win7 and newer.

                    for (int i = 0; i < device.AudioSessionManager.Sessions.Count; i++)
                    {
                        AudioSessionControl session = device.AudioSessionManager.Sessions[i];
                        if (session.State != AudioSessionState.AudioSessionStateExpired)
                            retval.Add(session);
                    }
                }
                return retval;
            }
        }

        private delegate void Fun(AudioSessionControl s);

        private void EachSession(Fun fn)
        {
            foreach (AudioSessionControl s in ActiveSessions)
            {
                fn(s);
            }
        }
    }
}
