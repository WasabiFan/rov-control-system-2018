using RovOperatorInterface.Communication;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Gaming.Input;

namespace RovOperatorInterface.Core
{
    public class TelemetryDataReceivedEventArgs : EventArgs
    {
        public string Text { get; set; }
    }

    class RovController
    {
        private RovConnector Connector;

        public event EventHandler<RawStringReceivedEventArgs> TelemetryDataReceived;

        private const int NumGimbalPositions = 10;
        private int CurrentGimbalPosition = NumGimbalPositions / 2;

        private GamepadReading? LastGamepadReading = null;

        public RovController()
        {
            Connector = new RovConnector();
            Connector.MessageReceived += Connector_MessageReceived;
            Connector.RawStringReceived += Connector_RawStringReceived;
        }

        public void Initialize()
        {
            Connector.BeginWatching();
        }

        private void Connector_RawStringReceived(object sender, RawStringReceivedEventArgs e)
        {
            Debug.WriteLine(e.Text);
        }

        private void HandleTelemetryMessage(SerialMessage message)
        {
            if (message.Parameters.Length != 3)
            {
                throw new SerialMessageMalformedException($"Telemetry message has the incorrect number of params; expected 3, was {message.Parameters.Length}", message);
            }
            
            bool.TryParse(message.Parameters[0], out bool IsScalingAtLimit);
            float.TryParse(message.Parameters[1], out float LimitScaleFactor);
            string ThrusterOutputs = message.Parameters[2].Replace(",", ", ");

            string data = $"Telemetry: {Environment.NewLine}"
                + $"\t{nameof(IsScalingAtLimit)}: {IsScalingAtLimit}{Environment.NewLine}"
                + $"\t{nameof(LimitScaleFactor)}: {LimitScaleFactor}{Environment.NewLine}"
                + $"\t{nameof(ThrusterOutputs)}: {ThrusterOutputs}";

            TelemetryDataReceived?.Invoke(this, new RawStringReceivedEventArgs() { Text = data });
        }

        private void Connector_MessageReceived(object sender, MessageReceivedEventArgs e)
        {
            if (e.Message.Type == "telemetry")
            {
                HandleTelemetryMessage(e.Message);
            }
            else
            {
                Debug.WriteLine($"Unknown message type {e.Message.Type}");
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
                else if(reading?.Buttons.HasFlag(GamepadButtons.B) == true && LastGamepadReading?.Buttons.HasFlag(GamepadButtons.B) == false)
                {
                    CurrentGimbalPosition++;
                }
                CurrentGimbalPosition = Math.Max(Math.Min(CurrentGimbalPosition, NumGimbalPositions), 0);
                double outputVal = CurrentGimbalPosition / (double)NumGimbalPositions;
                await Connector.Send(new SerialMessage("gimbal_control", outputVal.ToString()));
                
                LastGamepadReading = reading;
            }
        }
    }
}
