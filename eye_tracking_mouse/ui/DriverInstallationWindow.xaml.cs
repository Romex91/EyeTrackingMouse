using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Management;
using System.Runtime.InteropServices;

namespace eye_tracking_mouse
{
    public class SysRestore
    {
        [DllImport("srclient.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool SRSetRestorePointW(ref RestorePointInfo pRestorePtSpec, out STATEMGRSTATUS pSMgrStatus);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern uint SearchPath(string lpPath,
                            string lpFileName,
                            string lpExtension,
                            int nBufferLength,
                            [MarshalAs(UnmanagedType.LPTStr)] StringBuilder lpBuffer,
                            string lpFilePart);

        /// <summary>
        /// Contains information used by the SRSetRestorePoint function
        /// </summary>
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct RestorePointInfo
        {
            public int dwEventType; // The type of event
            public int dwRestorePtType; // The type of restore point
            public Int64 llSequenceNumber; // The sequence number of the restore point
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = MaxDescW + 1)]
            public string szDescription; // The description to be displayed so the user can easily identify a restore point
        }

        /// <summary>
        /// Contains status information used by the SRSetRestorePoint function
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        internal struct STATEMGRSTATUS
        {
            public int nStatus; // The status code
            public Int64 llSequenceNumber; // The sequence number of the restore point
        }

        // Type of restorations
        public enum RestoreType
        {
            ApplicationInstall = 0, // Installing a new application
            ApplicationUninstall = 1, // An application has been uninstalled
            ModifySettings = 12, // An application has had features added or removed
            CancelledOperation = 13, // An application needs to delete the restore point it created
            Restore = 6, // System Restore
            Checkpoint = 7, // Checkpoint
            DeviceDriverInstall = 10, // Device driver has been installed
            FirstRun = 11, // Program used for 1st time 
            BackupRecovery = 14 // Restoring a backup
        }

        // Constants
        internal const Int16 BeginSystemChange = 100; // Start of operation 
        internal const Int16 EndSystemChange = 101; // End of operation
        // Windows XP only - used to prevent the restore points intertwined
        internal const Int16 BeginNestedSystemChange = 102;
        internal const Int16 EndNestedSystemChange = 103;

        internal const Int16 DesktopSetting = 2; /* not implemented */
        internal const Int16 AccessibilitySetting = 3; /* not implemented */
        internal const Int16 OeSetting = 4; /* not implemented */
        internal const Int16 ApplicationRun = 5; /* not implemented */
        internal const Int16 WindowsShutdown = 8; /* not implemented */
        internal const Int16 WindowsBoot = 9; /* not implemented */
        internal const Int16 MaxDesc = 64;
        internal const Int16 MaxDescW = 256;

        /// <summary>
        /// Verifies that the OS can do system restores
        /// </summary>
        /// <returns>True if OS is either ME,XP,Vista,7</returns>
        public static bool SysRestoreAvailable()
        {
            int majorVersion = Environment.OSVersion.Version.Major;
            int minorVersion = Environment.OSVersion.Version.Minor;

            StringBuilder sbPath = new StringBuilder(260);

            // See if DLL exists
            if (SearchPath(null, "srclient.dll", null, 260, sbPath, null) != 0)
                return true;

            // Windows ME
            if (majorVersion == 4 && minorVersion == 90)
                return true;

            // Windows XP
            if (majorVersion == 5 && minorVersion == 1)
                return true;

            // Windows Vista
            if (majorVersion == 6 && minorVersion == 0)
                return true;

            // Windows Se7en
            if (majorVersion == 6 && minorVersion == 1)
                return true;

            // All others : Win 95, 98, 2000, Server
            return false;
        }

        /// <summary>
        /// Starts system restore
        /// </summary>
        /// <param name="strDescription">The description of the restore</param>
        /// <param name="rt">The type of restore point</param>
        /// <param name="lSeqNum">Returns the sequence number</param>
        /// <returns>The status of call</returns>
        /// <seealso cref="Use EndRestore() or CancelRestore() to end the system restore"/>
        public static int StartRestore(string strDescription, RestoreType rt, out long lSeqNum)
        {
            RestorePointInfo rpInfo = new RestorePointInfo();
            STATEMGRSTATUS rpStatus = new STATEMGRSTATUS();

            if (!SysRestoreAvailable())
            {
                lSeqNum = 0;
                return -1;
            }

            try
            {
                // Prepare Restore Point
                rpInfo.dwEventType = BeginSystemChange;
                // By default we create a verification system
                rpInfo.dwRestorePtType = (int)rt;
                rpInfo.llSequenceNumber = 0;
                rpInfo.szDescription = strDescription;

                SRSetRestorePointW(ref rpInfo, out rpStatus);
            }
            catch (DllNotFoundException)
            {
                lSeqNum = 0;
                return -1;
            }

            lSeqNum = rpStatus.llSequenceNumber;

            return rpStatus.nStatus;
        }

        /// <summary>
        /// Ends system restore call
        /// </summary>
        /// <param name="lSeqNum">The restore sequence number</param>
        /// <returns>The status of call</returns>
        public static int EndRestore(long lSeqNum)
        {
            RestorePointInfo rpInfo = new RestorePointInfo();
            STATEMGRSTATUS rpStatus = new STATEMGRSTATUS();

            if (!SysRestoreAvailable())
                return -1;

            try
            {
                rpInfo.dwEventType = EndSystemChange;
                rpInfo.llSequenceNumber = lSeqNum;

                SRSetRestorePointW(ref rpInfo, out rpStatus);
            }
            catch (DllNotFoundException)
            {
                return -1;
            }

            return rpStatus.nStatus;
        }

        /// <summary>
        /// Cancels restore call
        /// </summary>
        /// <param name="lSeqNum">The restore sequence number</param>
        /// <returns>The status of call</returns>
        public static int CancelRestore(long lSeqNum)
        {
            RestorePointInfo rpInfo = new RestorePointInfo();
            STATEMGRSTATUS rpStatus = new STATEMGRSTATUS();

            if (!SysRestoreAvailable())
                return -1;

            try
            {
                rpInfo.dwEventType = EndSystemChange;
                rpInfo.dwRestorePtType = (int)RestoreType.CancelledOperation;
                rpInfo.llSequenceNumber = lSeqNum;

                SRSetRestorePointW(ref rpInfo, out rpStatus);
            }
            catch (DllNotFoundException)
            {
                return -1;
            }

            return rpStatus.nStatus;
        }
    }

    /// <summary>
    /// Interaction logic for DriverInstallationWindow.xaml
    /// </summary>
    public partial class DriverInstallationWindow : Window
    {
        public DriverInstallationWindow()
        {
            InitializeComponent();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
        private void Install_Click(object sender, RoutedEventArgs e)
        {
            // Create restore point.
            if (CreateRestorePoint.IsChecked == true)
            {
                long seq_number;
                int status = SysRestore.StartRestore("Before Oblita Interception Driver installation", SysRestore.RestoreType.DeviceDriverInstall, out seq_number);
                if (status != 0)
                {
                    if (MessageBox.Show("An error with code " + status + " occurred while trying to create system restore point." +
                                ".\nCreate restore point manually before continuing. Continue driver installation?",
                                Helpers.application_name, MessageBoxButton.YesNo, MessageBoxImage.Error) == MessageBoxResult.No)
                    {
                        Close();
                    }
                }
            }

            try
            {
                string interception_installer = System.IO.Path.Combine(Environment.CurrentDirectory, "install-interception.exe");
                var process = System.Diagnostics.Process.Start(interception_installer, "/install");
                process.WaitForExit();
                if (process.ExitCode == 0)
                {
                    lock (Helpers.locker)
                    {
                        Options.Instance.key_bindings.is_driver_installed = true;
                        Options.Instance.SaveToFile(Options.Filepath);
                        if (MessageBox.Show("Installation successful. Reboot now?", Helpers.application_name, MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                        {
                            System.Diagnostics.Process.Start("shutdown.exe", "-r -t 0");
                        }
                    }
                }
                else
                {
                    MessageBox.Show("Couldn't install the interception driver: installer returned non-zero exit code.", Helpers.application_name, MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception err)
            {
                MessageBox.Show("Couldn't install the interception driver:" + err.Message, Helpers.application_name, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
