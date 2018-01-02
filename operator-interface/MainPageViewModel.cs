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
            Connecting,
            Connected
        }

        private string _RovHostName = "monkeyrov";
        private string _RovServiceName { get; set; } = "5001";
        private bool _ShouldAttemptConnection { get; set; }
        private bool _IsRovEnabled { get; set; }
        private bool _IsConnectButtonEnabled { get; set; }

        private string _MeanTransitTime { get; set; }

        private double _VehiclePitch { get; set; }
        private double _VehicleRoll { get; set; }

        private bool _IsImuEnabled { get; set; }

        private RovConnectionState _ConnectionState { get; set; }

        public ObservableCollection<string> LogMessages { get; protected set; } = new ObservableCollection<string>();

        public RovConnectionState ConnectionState
        {
            get { return _ConnectionState; }
            set
            {
                _ConnectionState = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs(nameof(ConnectButtonText)));
                    PropertyChanged(this, new PropertyChangedEventArgs(nameof(IsConnectButtonEnabled)));
                    PropertyChanged(this, new PropertyChangedEventArgs(nameof(IsEnableButtonEnabled)));
                }
            }
        }

        public string RovHostName
        {
            get { return _RovHostName; }
            set
            {
                if (value != _RovHostName)
                {
                    _RovHostName = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(RovHostName)));
                }
            }
        }

        public string RovServiceName
        {
            get { return _RovServiceName; }
            set
            {
                if (value != _RovServiceName)
                {
                    _RovServiceName = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(RovServiceName)));
                }
            }
        }

        public bool ShouldAttemptConnection
        {
            get { return _ShouldAttemptConnection; }
            set
            {
                if (value != _ShouldAttemptConnection)
                {
                    _ShouldAttemptConnection = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ShouldAttemptConnection)));
                }
            }
        }

        public string ConnectButtonText
        {
            get => ConnectionState == RovConnectionState.Connected ? "Connected" :
                    ConnectionState == RovConnectionState.Connecting ? "Connecting..." :
                    "Connect";
        }

        public bool IsConnectButtonEnabled
        {
            get
            {
                return ConnectionState == RovConnectionState.NotConnected;
            }
        }

        public bool IsEnableButtonEnabled
        {
            get
            {
                return ConnectionState == RovConnectionState.Connected;
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

        public string MeanTransitTime
        {
            get { return _MeanTransitTime; }
            set
            {
                if (value != _MeanTransitTime)
                {
                    _MeanTransitTime = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(MeanTransitTime)));
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
