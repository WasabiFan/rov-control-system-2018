using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.SerialCommunication;
using Windows.Storage.Streams;

namespace RovOperatorInterface.Communication
{
    public class MessageReceivedEventArgs : EventArgs
    {
        public SerialMessage Message { get; set; }
    }

    public class RawStringReceivedEventArgs : EventArgs
    {
        public string Text { get; set; }
    }

    class RovConnector
    {
        private const ushort UsbVendorId = 0x16C0;
        private const ushort UsbProductId = 0x0483;

        private readonly string DeviceSelector;
        DeviceWatcher Watcher;

        Connection OpenConnection = null;

        public event EventHandler<MessageReceivedEventArgs> MessageReceived;
        public event EventHandler<RawStringReceivedEventArgs> RawStringReceived;

        public bool IsConnected => OpenConnection != null;

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
                    message => MessageReceived?.Invoke(this, new MessageReceivedEventArgs() { Message = message }),
                    rawString => RawStringReceived?.Invoke(this, new RawStringReceivedEventArgs() { Text = rawString })
                );
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

        public void Send(SerialMessage message)
        {
            if (!IsConnected)
            {
                throw new InvalidOperationException();
            }

            OpenConnection.Send(message);
        }

        private class Connection
        {
            private Task IoTask;

            private readonly string Id;
            private readonly Action<SerialMessage> OnMessageReceived;
            private readonly Action<string> OnRawStringReceived;
            
            private DataWriter Writer;

            public Connection(string id, Action<SerialMessage> onMessageReceived, Action<string> onRawStringReceived)
            {
                Id = id;
                OnMessageReceived = onMessageReceived;
                OnRawStringReceived = onRawStringReceived;
            }

            public async void Open()
            {
                if (IoTask != null)
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

                Writer = new DataWriter(ConnectedDevice.OutputStream);

                // TODO: exception handling in loop
                IoTask = Task.Run(async () =>
                {
                    DataReader reader = new DataReader(ConnectedDevice.InputStream);
                    StringBuilder builder = new StringBuilder();
                    while (true)
                    {
                        // TODO: investigate larger chunks
                        await reader.LoadAsync(1);
                        char nextChar = reader.ReadString(1)[0];
                        if (nextChar == '\n')
                        {
                            string line = builder.ToString();
                            builder.Clear();
                            if (SerialMessage.TryParse(line, out SerialMessage message))
                            {
                                OnMessageReceived(message);
                            }
                            else
                            {
                                OnRawStringReceived(line);
                            }
                        }
                        else
                        {
                            builder.Append(nextChar);
                        }
                    }
                });
            }
            
            public void Send(SerialMessage message)
            {
                if (IoTask == null)
                {
                    throw new InvalidOperationException();
                }

                Writer.WriteString(message.Serialize() + "\n");
            }
        }
    }
}
