using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace RovOperatorInterface.Controls
{
    public sealed partial class WebcamSelector : UserControl
    {
        public delegate void WebcamSelectionChangedEventHandler(object sender, WebcamSelectedEventArgs e);
        public event WebcamSelectionChangedEventHandler WebcamSelectionChanged;

        public DeviceInformation SelectedWebcam => ViewModel.SelectedWebcam?.RawDeviceInfo;

        private WebcamSelectorViewModel ViewModel = new WebcamSelectorViewModel();
        public WebcamSelector()
        {
            this.InitializeComponent();
            DataContext = ViewModel;
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            DeviceWatcher Watcher = DeviceInformation.CreateWatcher(DeviceClass.VideoCapture);
            Watcher.Added += Watcher_Added;
            Watcher.Removed += Watcher_Removed;
            Watcher.Updated += Watcher_Updated;
            Watcher.Start();
            /*DeviceInformationCollection DeviceInfo = await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture);
            foreach (DeviceInformation Device in DeviceInfo)
                ViewModel.KnownWebcams.Add(new WebcamViewModel(Device));

            ViewModel.SelectedWebcam = ViewModel.KnownWebcams.FirstOrDefault();*/
        }

        private void Watcher_Updated(DeviceWatcher sender, DeviceInformationUpdate args)
        {
            throw new NotImplementedException();
        }

        private void Watcher_Removed(DeviceWatcher sender, DeviceInformationUpdate args)
        {
            throw new NotImplementedException();
        }

        private async void Watcher_Added(DeviceWatcher sender, DeviceInformation args)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal,async () =>
            {
                ViewModel.KnownWebcams.Add(new WebcamViewModel(await DeviceInformation.CreateFromIdAsync(args.Id)));
                if (ViewModel.SelectedWebcam == null)
                {
                    ViewModel.SelectedWebcam = ViewModel.KnownWebcams.FirstOrDefault();
                }
            });
        }

        private void WebcamSelectorDropdown_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            WebcamSelectionChanged?.Invoke(this, new WebcamSelectedEventArgs(SelectedWebcam));
        }

        public class WebcamViewModel
        {
            public WebcamViewModel(DeviceInformation device)
            {
                this.DisplayName = device.Name;
                this.DeviceId = device.Id;
                this.RawDeviceInfo = device;

                // TODO: Thumbnail
            }

            public string DisplayName { get; set; }
            public string DeviceId { get; set; }
            public DeviceInformation RawDeviceInfo { get; set; }
        }

        public class WebcamSelectorViewModel : INotifyPropertyChanged
        {
            private WebcamViewModel _SelectedWebcam = null;

            public ObservableCollection<WebcamViewModel> KnownWebcams { get; set; } = new ObservableCollection<WebcamViewModel>();
            public WebcamViewModel SelectedWebcam
            {
                get { return _SelectedWebcam; }
                set
                {
                    if(_SelectedWebcam != value)
                    {
                        _SelectedWebcam = value;
                        if(PropertyChanged != null)
                            PropertyChanged(this, new PropertyChangedEventArgs(nameof(SelectedWebcam)));
                    }
                }
            }

            public event PropertyChangedEventHandler PropertyChanged;
        }
    }

    public class WebcamSelectedEventArgs : EventArgs
    {
        public DeviceInformation SelectedWebcam { get; protected set; }

        public WebcamSelectedEventArgs(DeviceInformation selectedWebcam)
        {
            this.SelectedWebcam = selectedWebcam;
        }
    }
}
