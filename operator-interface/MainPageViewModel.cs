using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RovOperatorInterface
{
    public class MainPageViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public enum RovConnectionState
        {
            NotConnected,
            Connected
        }
        
        private bool _IsRovEnabled { get; set; }
        private bool _IsConnectButtonEnabled { get; set; }

        private double _VehiclePitch { get; set; }
        private double _VehicleRoll { get; set; }

        private bool _IsImuEnabled { get; set; }

        private bool _IsConnected { get; set; }

        public ObservableCollection<string> LogMessages { get; protected set; } = new ObservableCollection<string>();

        public bool IsConnected
        {
            get { return _IsConnected; }
            set
            {
                if (value != _IsConnected)
                {
                    _IsConnected = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsConnected)));
                }
            }
        }
        
        public bool IsEnableButtonEnabled
        {
            get
            {
                return IsConnected;
            }
        }

        public bool IsRovEnabled
        {
            get { return _IsRovEnabled; }
            set
            {
                if (value != _IsRovEnabled)
                {
                    _IsRovEnabled = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsRovEnabled)));
                }
            }
        }

        public double VehiclePitch
        {
            get { return _VehiclePitch; }
            set
            {
                if (value != _VehiclePitch)
                {
                    _VehiclePitch = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(VehiclePitch)));
                }
            }
        }

        public double VehicleRoll
        {
            get { return _VehicleRoll; }
            set
            {
                if (value != _VehicleRoll)
                {
                    _VehicleRoll = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(VehicleRoll)));
                }
            }
        }

        public bool IsImuEnabled
        {
            get { return _IsImuEnabled; }
            set
            {
                if (value != _IsImuEnabled)
                {
                    _IsImuEnabled = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsImuEnabled)));
                }
            }
        }
    }
}
