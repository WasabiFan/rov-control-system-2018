using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.SerialCommunication;

namespace RovOperatorInterface.Communication
{
    public class MessageReceivedEventArgs : EventArgs
    {
        public SerialMessage Message { get; set; }
    }

    class RovConnector
    {
        private const ushort UsbVendorId = 0x16C0;
        private const ushort UsbProductId = 0x0483;

        private readonly string DeviceSelector;
        DeviceWatcher Watcher;

        Connection OpenConnection = null;

        public event EventHandler<MessageReceivedEventArgs> MessageReceived;

        public RovConnector()
        {

            DeviceSelector = SerialDevice.GetDeviceSelectorFromUsbVidPid(UsbVendorId, UsbProductId);
            Watcher = DeviceInformation.CreateWatcher(DeviceSelector);

            Watcher.Added += Watcher_Added;
        }

        private void Watcher_Added(DeviceWatcher sender, DeviceInformation args)
        {
            if (OpenConnection == null)
            {
                OpenConnection = new Connection(args.Id,
                    message => MessageReceived?.Invoke(this, new MessageReceivedEventArgs() { Message = message }
                ));
                OpenConnection.Open();
            }
            else
            {
                Debug.WriteLine("Connection already opened");
                // TODO
            }
        }

        public void BeginWatching()
        {
            Watcher.Start();
        }

        private class Connection
        {
            private Task ReadTask;
            private StreamWriter Writer;

            private readonly string Id;
            private readonly Action<SerialMessage> OnReceived;

            public Connection(string id, Action<SerialMessage> onReceived)
            {
                Id = id;
                OnReceived = onReceived;
            }

            public async void Open()
            {
                if (ReadTask != null || Writer != null)
                {
                    throw new InvalidOperationException();
                }

                SerialDevice ConnectedDevice = await SerialDevice.FromIdAsync(Id);
                ConnectedDevice.BaudRate = 9600;
                ConnectedDevice.StopBits = SerialStopBitCount.One;
                ConnectedDevice.DataBits = 8;
                ConnectedDevice.Parity = SerialParity.None;
                ConnectedDevice.Handshake = SerialHandshake.None;
                ConnectedDevice.WriteTimeout = TimeSpan.FromMilliseconds(10);
                ConnectedDevice.ReadTimeout = TimeSpan.FromMilliseconds(10);

                Writer = new StreamWriter(ConnectedDevice.OutputStream.AsStreamForWrite())
                {
                    NewLine = "\n"
                };

                ReadTask = Task.Run(async () =>
                {

                    StreamReader reader = new StreamReader(ConnectedDevice.InputStream.AsStreamForRead());
                    while (true)
                    {
                        string line = await reader.ReadLineAsync();
                        if (SerialMessage.TryParse(line, out SerialMessage message))
                        {
                            OnReceived(message);
                        }
                        else
                        {
                            // TODO
                            Debug.WriteLine("Error while parsing incoming content as message");
                        }
                    }
                });
            }

            public async void Send(SerialMessage message)
            {
                if (ReadTask == null || Writer == null)
                {
                    throw new InvalidOperationException();
                }

                await Writer.WriteLineAsync(message.Serialize());
            }
        }
    }
}
