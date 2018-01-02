using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Gaming.Input;
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
    public sealed partial class GamepadSelector : UserControl
    {
        public delegate void GamepadSelectionChangedEventHandler(object sender, GamepadSelectedEventArgs e);
        public event GamepadSelectionChangedEventHandler GamepadSelectionChanged;
        
        public Gamepad SelectedGamepad => (GamepadSelectorDropdown.SelectedItem as GamepadViewModel)?.InternalGamepad;

        private GamepadSelectorViewModel ViewModel = new GamepadSelectorViewModel();

        public GamepadSelector()
        {
            this.InitializeComponent();
            DataContext = ViewModel;

        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            Gamepad.GamepadAdded += async (s, gamepad) => await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                ViewModel.KnownGamepads.Add(new GamepadViewModel(gamepad));
                if(ViewModel.SelectedGamepad == null)
                    ViewModel.SelectedGamepad = ViewModel.KnownGamepads.FirstOrDefault();

            });

            Gamepad.GamepadRemoved += async (s, gamepad) =>
            {
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    ViewModel.KnownGamepads.Remove(ViewModel.KnownGamepads.First(g => g.InternalGamepad == gamepad));
                    if (ViewModel.SelectedGamepad != null && ViewModel.SelectedGamepad.InternalGamepad == gamepad)
                        ViewModel.SelectedGamepad = ViewModel.KnownGamepads.FirstOrDefault();

                });
            };
            foreach (Gamepad g in Gamepad.Gamepads)
                ViewModel.KnownGamepads.Add(new GamepadViewModel(g));

            ViewModel.SelectedGamepad = ViewModel.KnownGamepads.FirstOrDefault();
        }

        private void GamepadSelectorDropdown_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (GamepadSelectionChanged != null)
                GamepadSelectionChanged(this, new GamepadSelectedEventArgs(SelectedGamepad));
        }

        public class GamepadViewModel
        {
            public string DisplayName { get; set; }
            public Gamepad InternalGamepad { get; set; }

            public GamepadViewModel(Gamepad internalGamepad)
            {
                InternalGamepad = internalGamepad;
                DisplayName = $"{(internalGamepad.IsWireless ? "Wireless" : "Wired")} Xbox controller";
                // TODO: Signal when buttons are being pressed
            }
        }

        public class GamepadSelectorViewModel : INotifyPropertyChanged
        {
            private GamepadViewModel _SelectedGamepad = null;

            public ObservableCollection<GamepadViewModel> KnownGamepads { get; set; } = new ObservableCollection<GamepadViewModel>();
            public GamepadViewModel SelectedGamepad
            {
                get { return _SelectedGamepad; }
                set
                {
                    if(_SelectedGamepad != value)
                    {
                        _SelectedGamepad = value;
                        if (PropertyChanged != null)
                            PropertyChanged(this, new PropertyChangedEventArgs(nameof(SelectedGamepad)));
                    }
                }
            }

            public event PropertyChangedEventHandler PropertyChanged;
        }
    }

    public class GamepadSelectedEventArgs : EventArgs
    {
        public Gamepad SelectedGamepad { get; protected set; }

        public GamepadSelectedEventArgs(Gamepad selectedGamepad)
        {
            this.SelectedGamepad = selectedGamepad;
        }
    }
}
