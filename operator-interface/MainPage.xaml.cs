using RovOperatorInterface.Controls;
using RovOperatorInterface.Core;
using RovOperatorInterface.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Display;
using Windows.Media.Capture;
using Windows.System.Display;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace RovOperatorInterface
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private MainPageViewModel ViewModel = new MainPageViewModel();

        private DispatcherTimer InputTimer;

        private string CurrentWebcamDeviceId = null;
        private MediaCapture Capture;
        private DisplayRequest DisplayRequest;
        private bool IsStreamingVideo = false;

        RovController Controller;

        public MainPage()
        {
            this.InitializeComponent();
            DataContext = ViewModel;

            Window.Current.CoreWindow.KeyDown += Page_KeyRouted;
            Window.Current.CoreWindow.KeyUp += Page_KeyRouted;

            Controller = new RovController();
            Controller.TelemetryDataReceived += async (sender, e) =>
            {
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => ViewModel.TelemetryData = e.Text);
            };

            Controller.OrientationDataReceived += async (sender, e) =>
            {
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    ViewModel.VehicleRoll = e.Roll;
                    ViewModel.VehiclePitch = e.Pitch;
                    ViewModel.VehicleYaw = e.Yaw;
                });
            };

            Controller.LogMessageReceived += async (sender, e) =>
            {
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    ViewModel.LogMessages.Add($"[{e.Type}] {e.Message}");
                });
            };

            InputTimer = new DispatcherTimer() { Interval = TimeSpan.FromMilliseconds(30) };
            InputTimer.Tick += InputTimer_Tick;
            InputTimer.Start();
        }

        private async Task StartPreviewAsync(DeviceInformation newDevice)
        {
            if (newDevice?.Id != CurrentWebcamDeviceId)
            {
                await StopVideoStreamAsync();
                if (newDevice != null)
                {
                    try
                    {
                        Capture = new MediaCapture();
                        await Capture.InitializeAsync(new MediaCaptureInitializationSettings() { VideoDeviceId = newDevice.Id });
                        Capture.VideoDeviceController.DesiredOptimization = Windows.Media.Devices.MediaCaptureOptimization.Latency;

                        PreviewControl.Source = Capture;
                        await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
                        {
                            await Capture.StartPreviewAsync();

                            DisplayRequest = new DisplayRequest();
                            DisplayRequest.RequestActive();
                            DisplayInformation.AutoRotationPreferences = DisplayOrientations.Landscape;

                            CurrentWebcamDeviceId = newDevice.Id;
                        });
                    }
                    catch (UnauthorizedAccessException)
                    {
                        // This will be thrown if the user denied access to the camera in privacy settings
                        System.Diagnostics.Debug.WriteLine("The app was denied access to the camera");
                        Capture = null;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine("MediaCapture initialization failed. {0}", ex.Message);
                    }
                }
            }
        }

        private async Task StopVideoStreamAsync()
        {
            if (Capture != null)
                await Capture.StopPreviewAsync();

            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                PreviewControl.Source = null;
                if (DisplayRequest != null)
                {
                    DisplayRequest.RequestRelease();
                }

                if (Capture != null)
                {
                    Capture.Dispose();
                    Capture = null;
                }

                DisplayRequest = null;
            });
        }

        private async void InputTimer_Tick(object sender, object e)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
            {
                var Reading = GamepadSelector.SelectedGamepad?.GetCurrentReading();
                await Controller.UpdateControlInput(Reading);
            });
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            await StartPreviewAsync(WebcamSelector.SelectedWebcam);
            Controller.Initialize();

        }

        private async void WebcamSelector_WebcamSelectionChanged(object sender, WebcamSelectedEventArgs e)
        {
            await StartPreviewAsync(e.SelectedWebcam);
        }

        private void Page_KeyRouted(Windows.UI.Core.CoreWindow sender, Windows.UI.Core.KeyEventArgs args)
        {
            if (args.VirtualKey >= Windows.System.VirtualKey.GamepadA && args.VirtualKey <= Windows.System.VirtualKey.GamepadRightThumbstickLeft)
            {
                args.Handled = true;
            }
        }

        private void EnableToggle_Checked(object sender, RoutedEventArgs e)
        {
            // TODO
        }

        private void EnableToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            // TODO
        }
    }
}
