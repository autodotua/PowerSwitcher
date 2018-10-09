using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowerSwitcher.Helper
{
    public static class Startup
    {
        static Startup()
        {
            AppName = Process.GetCurrentProcess().ProcessName;
            Common = false;
        }

        public static string AppName { get; set; }
        private static bool common;
        public static bool Common
        {
            get => common;
            set
            {
                if (!Common)
                {
                    startupFolderFilePath = Environment.GetFolderPath(Environment.SpecialFolder.StartMenu) + "\\" + AppName + ".lnk";

                    registryKey = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run");
                }
                else
                {
                    startupFolderFilePath = Environment.GetFolderPath(Environment.SpecialFolder.CommonStartup) + "\\" + AppName + ".lnk";

                    registryKey = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run");
                }
                common = value;
            }
        }

        private static readonly string programFileName = Process.GetCurrentProcess().MainModule.FileName;
        private static string startupFolderFilePath;
        private static RegistryKey registryKey = null;



        public static void CreateRegistryKey(string arguments = null)
        {
            string value = "\"" + programFileName + "\"";
            if (!string.IsNullOrWhiteSpace(arguments))
            {
                value += " " + arguments;
            }
            registryKey.SetValue(AppName, value);

        }

        public static bool IsRegistryKeyExist()
        {
            return registryKey.GetValue(AppName) != null;
        }
        public static void DeleteRegistryKey()
        {
            registryKey.DeleteValue(AppName);
        }






    }

}
