using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
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
        
        public event EventHandler<EventArgs> Connected;
        public event EventHandler<EventArgs> Disconnected;

        public event EventHandler<MessageReceivedEventArgs> MessageReceived;
        public event EventHandler<RawStringReceivedEventArgs> RawStringReceived;

        public bool IsConnected => OpenConnection != null && OpenConnection.IsOpen;

        public RovConnector()
        {
            DeviceSelector = SerialDevice.GetDeviceSelectorFromUsbVidPid(UsbVendorId, UsbProductId);
            Watcher = DeviceInformation.CreateWatcher(DeviceSelector);

            Watcher.Added += Watcher_Added;
            Watcher.Removed += Watcher_Removed;
            Watcher.Updated += Watcher_Updated;
        }

        private void Watcher_Updated(DeviceWatcher sender, DeviceInformationUpdate args)
        {
            throw new NotImplementedException();
        }

        private async void Watcher_Removed(DeviceWatcher sender, DeviceInformationUpdate args)
        {
            if (args.Id != null && OpenConnection?.ConnectedDeviceId == args.Id)
            {
                await OpenConnection.Close();
                OpenConnection = null;
                Disconnected?.Invoke(this, new EventArgs());
            }
        }

        private void Watcher_Added(DeviceWatcher sender, DeviceInformation args)
        {
            if (OpenConnection == null)
            {
                Debug.WriteLine("New serial connection available; opening");
                OpenConnection = new Connection(args.Id,
                    message => MessageReceived?.Invoke(this, new MessageReceivedEventArgs() { Message = message }),
                    rawString => RawStringReceived?.Invoke(this, new RawStringReceivedEventArgs() { Text = rawString })
                );
                OpenConnection.Open();
                Connected?.Invoke(this, new EventArgs());
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

        public async Task Send(SerialMessage message)
        {
            if (!IsConnected)
            {
                throw new RovNotConnectedException();
            }

            await OpenConnection.Send(message);
        }

        private class Connection
        {
            private SerialDevice ConnectedDevice;
            private CancellationTokenSource ReadTaskCancellationToken;
            private Task ReadTask;
            private DataWriter Writer;

            public bool IsOpen { get; protected set; } = false;

            public readonly string ConnectedDeviceId;
            private readonly Action<SerialMessage> OnMessageReceived;
            private readonly Action<string> OnRawStringReceived;
            
            public Connection(string id, Action<SerialMessage> onMessageReceived, Action<string> onRawStringReceived)
            {
                ConnectedDeviceId = id;
                OnMessageReceived = onMessageReceived;
                OnRawStringReceived = onRawStringReceived;
            }

            public async void Open()
            {
                if (ReadTask != null)
                {
                    throw new InvalidOperationException("An attempt was made to open the connection, but there is already an open connection.");
                }

                ConnectedDevice = await SerialDevice.FromIdAsync(ConnectedDeviceId);
                ConnectedDevice.BaudRate = 115200;
                ConnectedDevice.StopBits = SerialStopBitCount.One;
                ConnectedDevice.DataBits = 8;
                ConnectedDevice.Parity = SerialParity.None;
                ConnectedDevice.Handshake = SerialHandshake.None;
                ConnectedDevice.WriteTimeout = TimeSpan.FromMilliseconds(10);
                ConnectedDevice.ReadTimeout = TimeSpan.FromMilliseconds(10);

                Writer = new DataWriter(ConnectedDevice.OutputStream);

                ReadTaskCancellationToken = new CancellationTokenSource();
                ReadTask = Task.Run(async () =>
                {
                    DataReader reader = new DataReader(ConnectedDevice.InputStream);
                    StringBuilder builder = new StringBuilder();
                    while (true)
                    {
                        try
                        {
                            // TODO: investigate larger chunks
                            // TODO: Can larger code points cause issues?
                            var loadTask = reader.LoadAsync(1);
                            var loadResult = await loadTask.AsTask(ReadTaskCancellationToken.Token);
                            if (loadResult < 1)
                            {
                                continue;
                            }

                            if (loadResult != 1 || reader.UnconsumedBufferLength != 1)
                            {
                                Debugger.Break();
                            }

                            char nextChar = reader.ReadString(1)[0];
                            if (nextChar == '\n')
                            {
                                string line = builder.ToString();
                                if (SerialMessage.TryParse(line, out SerialMessage message))
                                {
                                    try
                                    {
                                        OnMessageReceived(message);
                                    }
                                    catch (Exception e)
                                    {
                                        Debug.WriteLine(e.ToString());
                                        Debugger.Break();
                                    }
                                }
                                else
                                {
                                    try
                                    {
                                        OnRawStringReceived(line);
                                    }
                                    catch (Exception e)
                                    {
                                        Debug.WriteLine(e.ToString());
                                        Debugger.Break();
                                    }
                                }
                                builder.Clear();
                            }
                            else
                            {
                                builder.Append(nextChar);
                            }
                        }
                        // The operation attempted to access data outside the valid range
                        catch (System.Runtime.InteropServices.COMException e) when (e.HResult == -2147483637)
                        {
                            // TODO
                        }
                    }
                });

                IsOpen = true;
            }
            
            public async Task Close()
            {
                IsOpen = false;
                ReadTaskCancellationToken.Cancel();
                // TODO: Will this ever hang?
                try
                {
                    await ReadTask;
                }
                catch(Exception e)
                {
                    // All we care about is that the thread is dead; we're throwing it away anyway.
                }

                ReadTaskCancellationToken.Dispose();
                Writer.Dispose();
                ConnectedDevice.Dispose();

                ReadTaskCancellationToken = null;
                Writer = null;
                ConnectedDevice = null;
            }

            public async Task Send(SerialMessage message)
            {
                if (Writer == null)
                {
                    throw new InvalidOperationException("An attempt was made to send a message, but there is no underlying data writer open.");
                }

                Writer.WriteString(message.Serialize() + "\n");
                try
                {
                    await Writer.StoreAsync();
                }
                catch (Exception e) when (
                        // The semaphore timeout period has expired. (Exception from HRESULT: 0x80070079)
                           e.HResult == unchecked((int)0x80070079)
                        // The device does not recognize the command. (Exception from HRESULT: 0x80070016)
                        || e.HResult == unchecked((int)0x80070016)
                        // A device attached to the system is not functioning. (Exception from HRESULT: 0x8007001F)
                        || e.HResult == unchecked((int)0x8007001F)
                        // System.IO.FileNotFoundException: 'The system cannot find the file specified. (Exception from HRESULT: 0x80070002)'
                        || e.HResult == unchecked((int)0x80070002)
                    )
                {
                    throw new RovSendOperationFailedException(e);
                }
            }
        }
    }
}
