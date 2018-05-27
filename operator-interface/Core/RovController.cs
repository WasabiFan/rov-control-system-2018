using RovOperatorInterface.Communication;
using RovOperatorInterface.Utils;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.Gaming.Input;

namespace RovOperatorInterface.Core
{
    public class OrientationEventArgs : EventArgs
    {
        public double Roll { get; set; }
        public double Pitch { get; set; }
        public double Yaw { get; set; }
    }

    public class EnableStateEventArgs : EventArgs
    {
        public bool EnableState { get; set; }
    }

    public class LogMessageEventArgs : EventArgs
    {
        public string Type { get; set; }
        public string Message { get; set; }
    }

    class RovController
    {
        private RovConnector Connector;

        public event EventHandler<EventArgs> Connected;
        public event EventHandler<EventArgs> Disconnected;
        public event EventHandler<RawStringReceivedEventArgs> TelemetryDataReceived;
        public event EventHandler<LogMessageEventArgs> LogMessageReceived;
        public event EventHandler<OrientationEventArgs> OrientationDataReceived;
        public event EventHandler<EnableStateEventArgs> EnableStateReceived;

        private const int NumGimbalPositions = 10;
        private int CurrentGimbalPosition = NumGimbalPositions / 2;

        private GamepadReading? LastGamepadReading = null;
        
        TimeoutFlag EnableTimeout = new TimeoutFlag(TimeSpan.FromSeconds(1));

        public RovController()
        {
            Connector = new RovConnector();
            Connector.MessageReceived += Connector_MessageReceived;
            Connector.RawStringReceived += Connector_RawStringReceived;

            Connector.Connected += (sender, e) => Connected?.Invoke(this, new EventArgs());
            Connector.Disconnected += (sender, e) => Disconnected?.Invoke(this, new EventArgs());
        }

        public void Initialize()
        {
            Connector.BeginWatching();
        }

        private void Connector_RawStringReceived(object sender, RawStringReceivedEventArgs e)
        {
            LogMessageReceived?.Invoke(this, new LogMessageEventArgs()
            {
                Type = "info",
                Message = e.Text
            });
            Debug.WriteLine(e.Text);
        }

        private void HandleTelemetryMessage(SerialMessage message)
        {
            if (message.Parameters.Length != 5)
            {
                throw new SerialMessageMalformedException($"Telemetry message has the incorrect number of params; expected 5, was {message.Parameters.Length}", message);
            }

            bool.TryParse(message.Parameters[0], out bool IsScalingAtLimit);
            float.TryParse(message.Parameters[1], out float LimitScaleFactor);
            string ThrusterOutputs = message.Parameters[2].Replace(",", ", ");
            string ImuCalibStates = message.Parameters[3].Replace(",", ", ");
            int.TryParse(message.Parameters[4], out int OverallImuCalibState);

            string data = $"Telemetry: {Environment.NewLine}"
                + $"\t{nameof(IsScalingAtLimit)}: {IsScalingAtLimit}{Environment.NewLine}"
                + $"\t{nameof(LimitScaleFactor)}: {LimitScaleFactor}{Environment.NewLine}"
                + $"\t{nameof(ThrusterOutputs)}: {ThrusterOutputs}{Environment.NewLine}"
                + $"\t{nameof(ImuCalibStates)}: {ImuCalibStates}{Environment.NewLine}"
                + $"\t{nameof(OverallImuCalibState)}: {OverallImuCalibState}";

            TelemetryDataReceived?.Invoke(this, new RawStringReceivedEventArgs() { Text = data });
        }

        private void HandleLogMessage(SerialMessage message)
        {
            if (message.Parameters.Length < 1)
            {
                throw new SerialMessageMalformedException($"Log message has the incorrect number of params; expected at least 1, was {message.Parameters.Length}", message);
            }

            LogMessageReceived?.Invoke(this, new LogMessageEventArgs()
            {
                Type = message.Parameters[0],
                Message = string.Join(' ', message.Parameters.Skip(1).ToArray())
            });
        }

        private void HandleOrientationMessage(SerialMessage message)
        {
            if (message.Parameters.Length != 3)
            {
                throw new SerialMessageMalformedException($"Orientation message has the incorrect number of params; expected 3, was {message.Parameters.Length}", message);
            }

            double.TryParse(message.Parameters[0], out double Yaw);
            double.TryParse(message.Parameters[1], out double Roll);
            double.TryParse(message.Parameters[2], out double Pitch);

            OrientationDataReceived?.Invoke(this, new OrientationEventArgs()
            {
                Roll = Roll,
                Pitch = Pitch,
                Yaw = Yaw
            });
        }

        private void HandleEnableStateMessage(SerialMessage message)
        {
            if (message.Parameters.Length != 1)
            {
                throw new SerialMessageMalformedException($"Enable state message has the incorrect number of params; expected 1, was {message.Parameters.Length}", message);
            }

            bool.TryParse(message.Parameters[0], out bool IsEnabled);
            EnableStateReceived?.Invoke(this, new EnableStateEventArgs()
            {
                EnableState = IsEnabled
            });
        }

        private void Connector_MessageReceived(object sender, MessageReceivedEventArgs e)
        {
            EnableTimeout.RegisterUpdate();
            if (e.Message.Type == "telemetry")
            {
                HandleTelemetryMessage(e.Message);
            }
            else if (e.Message.Type == "log")
            {
                HandleLogMessage(e.Message);
            }
            else if (e.Message.Type == "orientation")
            {
                HandleOrientationMessage(e.Message);
            }
            else if (e.Message.Type == "enable_state")
            {
                HandleEnableStateMessage(e.Message);
            }
            else
            {
                Debug.WriteLine($"Unknown message type {e.Message.Type}");
            }
        }

        public async void RequestEnableDisable(bool isEnabled)
        {
            if (Connector.IsConnected)
            {
                try
                {
                    await Connector.Send(new SerialMessage("request_enable_disable", isEnabled.ToString().ToLower()));
                }
                catch(RovSendOperationFailedException)
                {
                    // TODO: do we care?
                }
            }
        }

        private static float ButtonsToAnalog(GamepadReading? reading, GamepadButtons positive, GamepadButtons negative, float magnitude = 1)
        {
            float output = 0;
            if (reading?.Buttons.HasFlag(positive) ?? false)
            {
                output += magnitude;
            }

            if (reading?.Buttons.HasFlag(negative) ?? false)
            {
                output -= magnitude;
            }

            return output;
        }

        public async Task UpdateControlInput(GamepadReading? reading)
        {
            if (Connector.IsConnected)
            {
                try
                {
                    double?[] RigidForceCommands =
                    {
                        -reading?.LeftThumbstickY,
                        -reading?.LeftThumbstickX,
                        reading?.RightTrigger - reading?.LeftTrigger,
                        0,
                        reading?.RightThumbstickY,
                        reading?.RightThumbstickX
                    };
                    await Connector.Send(new SerialMessage("motion_control", RigidForceCommands.Select(f => (f ?? 0).ToString()).ToArray()));

                    await Connector.Send(new SerialMessage("gripper_control",
                        ButtonsToAnalog(reading, GamepadButtons.DPadDown, GamepadButtons.DPadUp, 0.4f).ToString(),
                        ButtonsToAnalog(reading, GamepadButtons.RightShoulder, GamepadButtons.LeftShoulder).ToString()));

                    if (reading?.Buttons.HasFlag(GamepadButtons.A) == true && LastGamepadReading?.Buttons.HasFlag(GamepadButtons.A) == false)
                    {
                        CurrentGimbalPosition--;
                    }
                    else if (reading?.Buttons.HasFlag(GamepadButtons.B) == true && LastGamepadReading?.Buttons.HasFlag(GamepadButtons.B) == false)
                    {
                        CurrentGimbalPosition++;
                    }
                    CurrentGimbalPosition = Math.Max(Math.Min(CurrentGimbalPosition, NumGimbalPositions), 0);
                    double outputVal = CurrentGimbalPosition / (double)NumGimbalPositions;
                    await Connector.Send(new SerialMessage("gimbal_control", outputVal.ToString()));

                    await Connector.Send(new SerialMessage("buzzer_control", (LastGamepadReading?.Buttons.HasFlag(GamepadButtons.Menu) == true).ToString().ToLower()));

                    LastGamepadReading = reading;
                }
                catch (RovSendOperationFailedException)
                {
                    // TODO: do we care?
                }
        }
        }
        
        public bool IsEnableTimeoutExpired
        {
            get => EnableTimeout.IsTimedOut();
        }
    }
}
