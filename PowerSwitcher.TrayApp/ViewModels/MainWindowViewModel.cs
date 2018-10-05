﻿using PowerSwitcher.Helper;
using PowerSwitcher.Configuration;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;

namespace PowerSwitcher.ViewModels
{
    public class MainWindowViewModel : ObservableObject
    {
        private IPowerManager pwrManager;
        private ConfigurationInstance<PowerSwitcherSettings> config;

        public INotifyCollectionChanged Schemas { get; private set; }
        public IPowerSchema ActiveSchema
        {
            get { return pwrManager.CurrentSchema; }
            set { if (value != null && !value.IsActive) { pwrManager.SetPowerSchema(value); } }
        }
        public string BatteryLavel { get; set; }

        public MainWindowViewModel()
        {
            App currApp = System.Windows.Application.Current as App;
            if (currApp == null) { return; }

            this.pwrManager = currApp.PowerManager;
            this.config = currApp.Configuration;

            pwrManager.PropertyChanged += PwrManager_PropertyChanged;
            config.Data.PropertyChanged += SettingsData_PropertyChanged;

            Schemas = pwrManager.Schemas.WhereObservableSwitchable<ObservableCollection<IPowerSchema>, IPowerSchema>
                (
                sch => defaultGuids.Contains(sch.Guid) || sch.IsActive,
                config.Data.ShowOnlyDefaultSchemas
                );
        }

        private void SettingsData_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(PowerSwitcherSettings.ShowOnlyDefaultSchemas))
            {
                updateOnlyDefaultSchemasSetting();
            }
        }

        private void updateOnlyDefaultSchemasSetting()
        {

            (Schemas as ObservableCollectionWhereSwitchableShim<ObservableCollection<IPowerSchema>, IPowerSchema>).FilterOn = config.Data.ShowOnlyDefaultSchemas;
        }

        private void PwrManager_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(IPowerManager.CurrentSchema))
            {
                RaisePropertyChangedEvent(nameof(ActiveSchema));
            }
        }

        private Guid[] defaultGuids = { new Guid("8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c"), new Guid("381b4222-f694-41f0-9685-ff5bb260df2e"), new Guid("a1841308-3541-4fab-bc81-f71556f20b4a") };

        public void SetGuidAsActive(Guid guid)
        {
            pwrManager.SetPowerSchema(guid);
        }

        public void Refresh()
        {
            pwrManager.UpdateSchemas();
            UpdateBatteryInfo();
        }

        private void UpdateBatteryInfo()
        {
            var status = Microsoft.WindowsAPICodePack.ApplicationServices.PowerManager.GetCurrentBatteryState();
            if (Microsoft.WindowsAPICodePack.ApplicationServices.PowerManager.IsBatteryPresent)
            {
                BatteryLavel = Microsoft.WindowsAPICodePack.ApplicationServices.PowerManager.BatteryLifePercent.ToString() + "%";

                try
                {
                    if (status.ChargeRate != 0)
                    {
                        BatteryLavel += "    " + (status.ChargeRate > 0 ? "+" : "")
                            + (status.ChargeRate / 1000.0).ToString("0.0") + "W";
                    }

                    if (status.MaxCharge > 0)
                    {
                        BatteryLavel += "    " + (status.CurrentCharge / 1000.0).ToString("0") + "Wh" + " / " + (status.MaxCharge / 1000.0).ToString("0") + "Wh";
                    }

                    if (status.EstimatedTimeRemaining != TimeSpan.MinValue
                        && status.EstimatedTimeRemaining != TimeSpan.Zero
                        && status.EstimatedTimeRemaining.TotalHours < 1000)
                    {
                        BatteryLavel += "    " + status.EstimatedTimeRemaining.Hours.ToString()
                          + ":" + status.EstimatedTimeRemaining.Minutes.ToString("00");
                    }

                    if (status.ACOnline)
                    {
                        BatteryLavel += "    🔌";
                    }
                }
                catch
                {

                }


            }
            else
            {
                BatteryLavel = "外接电源";
            }
            RaisePropertyChangedEvent(nameof(BatteryLavel));
        }
    }
}
