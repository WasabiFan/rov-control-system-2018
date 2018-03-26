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

        public async Task UpdateControlInput(GamepadReading? reading)
        {
            if (Connector.IsConnected)
            {
                double?[] RigidForceCommands =
                {
                    reading?.LeftThumbstickY,
                    reading?.LeftThumbstickX,
                    reading?.RightTrigger - reading?.LeftTrigger,
                    0,
                    reading?.RightThumbstickY,
                    reading?.RightThumbstickX
                };
                await Connector.Send(new SerialMessage("motion_control", RigidForceCommands.Select(f => (f ?? 0).ToString()).ToArray()));
            }
        }
    }
}
