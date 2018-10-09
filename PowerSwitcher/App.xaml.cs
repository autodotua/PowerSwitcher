using PowerSwitcher.Helper;
using PowerSwitcher.Configuration;
using PowerSwitcher.Services;
using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows;

namespace PowerSwitcher
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public HotKeyService HotKeyManager { get; private set; }
        public bool HotKeyFailed { get; private set; }

        public IPowerManager PowerManager { get; private set; }
        public TrayApp TrayApp { get; private set; }
        public ConfigurationInstance<PowerSwitcherSettings> Configuration { get; private set; }

        private Mutex mMutex;
        private void ApplicationStartup(object sender, StartupEventArgs e)
        {
            if (!TryToCreateMutex())
            {
                return;
            }

            var configurationManager = new ConfigurationManagerXML<PowerSwitcherSettings>(Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Petrroll", "PowerSwitcher", "PowerSwitcherSettings.xml"
                ));

            Configuration = new ConfigurationInstance<PowerSwitcherSettings>(configurationManager);
            migrateSettings();

            HotKeyManager = new HotKeyService();
            HotKeyFailed = false;

            PowerManager = new PowerManager();
            MainWindow = new MainWindow();
            TrayApp = new TrayApp(PowerManager, Configuration); //Has to be last because it hooks to MainWindow

            Configuration.Data.PropertyChanged += ConfigurationPropertyChanged;
            if (Configuration.Data.ShowOnShortcutSwitch) { registerHotkeyFromConfiguration(); }

            TrayApp.CreateAltMenu();
        }

        private void migrateSettings()
        {
            //Migration of shortcut because Creators update uses WinShift + S for screenshots
            if (Configuration.Data.ShowOnShortcutKey == System.Windows.Input.Key.S &&
                Configuration.Data.ShowOnShortcutKeyModifier == (KeyModifier.Shift | KeyModifier.Win))
            {
                Configuration.Data.ShowOnShortcutKey = System.Windows.Input.Key.L;
                Configuration.Save();
            }
        }

        private void ConfigurationPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(PowerSwitcherSettings.ShowOnShortcutSwitch))
            {
                if (Configuration.Data.ShowOnShortcutSwitch) { registerHotkeyFromConfiguration(); }
                else { unregisterHotkeyFromConfiguration(); }
            }
        }

        private void unregisterHotkeyFromConfiguration()
        {
            HotKeyManager.Unregister(new HotKey(Configuration.Data.ShowOnShortcutKey, Configuration.Data.ShowOnShortcutKeyModifier));
        }

        private bool registerHotkeyFromConfiguration()
        {
            var newHotKey = new HotKey(Configuration.Data.ShowOnShortcutKey, Configuration.Data.ShowOnShortcutKeyModifier);

            bool success = HotKeyManager.Register(newHotKey);
            if (!success) { HotKeyFailed = true; return false; }
            newHotKey.HotKeyFired += (this.MainWindow as MainWindow).ToggleWindowVisibility;

            return true;
        }

        private bool TryToCreateMutex()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var mutexName = string.Format(CultureInfo.InvariantCulture, "Local\\{{{0}}}{{{1}}}", assembly.GetType().GUID, assembly.GetName().Name);

            bool mutexCreated;

            mMutex = new Mutex(true, mutexName, out mutexCreated);
            if (mutexCreated) { return true; }

            mMutex = null;
            Current.Shutdown();
            return false;
        }

        private void DisposeMutex()
        {
            if (mMutex == null) return;
            mMutex.ReleaseMutex();
            mMutex.Close();
            mMutex = null;
        }

        private void AppExit(object sender, ExitEventArgs e)
        {
            DisposeMutex();
            PowerManager?.Dispose();
            HotKeyManager?.Dispose();
        }

        ~App()
        {
            DisposeMutex();
        }

    }
}
