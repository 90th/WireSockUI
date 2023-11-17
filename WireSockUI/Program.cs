﻿using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;
using WireSockUI.Extensions;
using WireSockUI.Forms;
using WireSockUI.Properties;

namespace WireSockUI
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            if (!Directory.Exists(Global.MainFolder)) Directory.CreateDirectory(Global.MainFolder);
            if (!Directory.Exists(Global.ConfigsFolder)) Directory.CreateDirectory(Global.ConfigsFolder);

            if (IsApplicationAlreadyRunning())
            {
                MessageBox.Show(Resources.AlreadyRunningMessage, Resources.AlreadyRunningTitle, MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                Environment.Exit(1);
            }

            if (!IsWireSockInstalled())
            {
                MessageBox.Show(Resources.AppNoWireSockMessage, Resources.AppNoWireSockTitle, MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                OpenBrowser(Resources.AppWireSockURL);

                Environment.Exit(1);
            }

            Application.Run(new FrmMain());
        }

        private static void OpenBrowser(string url)
        {
            try
            {
                Process.Start(url);
            }
            catch
            {
            }
        }

        /// <summary>
        ///     Determine if this WireSockUI was generated by an automated build from a GitHub repository
        /// </summary>
        /// <returns>Assembly repository if set during build</returns>
        private static string GetRepository()
        {
            var assembly = Assembly.GetExecutingAssembly();

            foreach (var metadata in assembly.GetCustomAttributes<AssemblyMetadataAttribute>())
                if (string.Equals(metadata.Key, "Repository"))
                    return metadata.Value;

            return null;
        }

        /// <summary>
        ///     Determine if the WireSock library components are installed.
        /// </summary>
        /// <returns><c>true</c> if installed, otherwise <c>false</c></returns>
        private static bool IsWireSockInstalled()
        {
            using (var key = Registry.LocalMachine.OpenSubKey("SOFTWARE\\NTKernelResources\\WinpkFilterForVPNClient"))
            {
                if (key == null) return false;
                var wiresockLocation = key.GetValue("InstallLocation") + "bin\\wiresock-client.exe";

                // Add the directory containing the wgbooster.dll to the system's path if it is not added
                var installPath = key.GetValue("InstallLocation").ToString();
                var binPath = Path.Combine(installPath, "bin");

                var environmentPath = Environment.GetEnvironmentVariable("PATH");

                if (environmentPath == null || environmentPath.Contains(binPath))
                    return File.Exists(wiresockLocation);

                environmentPath = $"{binPath};{environmentPath}";
                Environment.SetEnvironmentVariable("PATH", environmentPath);

                return File.Exists(wiresockLocation);
            }
        }

        /// <summary>
        ///     Determines if another instance of the current application is already running.
        /// </summary>
        /// <returns>
        ///     A boolean value that is true if another instance of the application is already running,
        ///     and false if the current instance is the only one running.
        /// </returns>
        /// <remarks>
        ///     This function uses a named Mutex (a synchronization primitive) to check if it has been
        ///     created before. If the Mutex is not new, that means another instance of the application
        ///     is already running.
        /// </remarks>
        private static bool IsApplicationAlreadyRunning()
        {
            const string mutexName = "Global\\WiresockClientService";
            Global.AlreadyRunning = new Mutex(true, mutexName, out var createdNew);

            if (!createdNew)
            {
                Global.AlreadyRunning.Dispose();
                return true;
            }

            return false;
        }
    }
}