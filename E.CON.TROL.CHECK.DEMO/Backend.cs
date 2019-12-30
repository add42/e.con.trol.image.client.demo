using NetMQ;
using NetMQ.Sockets;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace E.CON.TROL.CHECK.DEMO
{
    class Backend : IDisposable
    {
        public Config Config
        {
            get { return Config.Instance; }
        }

        Thread ComThreadReceiveImages { get; }

        Thread ComThreadReceiveControlMessages { get; }

        Thread ComThreadTransmitControlMessages { get; }

        Thread ThreadProcessing { get; }

        ConcurrentQueue<NetMq.Messages.BaseMessage> Queue { get; } = new ConcurrentQueue<NetMq.Messages.BaseMessage>();

        public ConcurrentQueue<NetMq.Messages.ImageMessage> QueueImages { get; } = new ConcurrentQueue<NetMq.Messages.ImageMessage>();

        public bool IsDisposed { get; private set; }

        public Exception LastException { get; private set; }

        public int CounterStateMessage { get; private set; } = 0;

        public Backend()
        {
            Config.LoadConfig();

            ComThreadReceiveImages = new Thread(ExecuteComThreadReceiveImages) { IsBackground = true };
            ComThreadReceiveImages.Start();

            ComThreadReceiveControlMessages = new Thread(ExecuteComThreadReceiveControlMessages) { IsBackground = true };
            ComThreadReceiveControlMessages.Start();

            ComThreadTransmitControlMessages = new Thread(ExecuteComThreadTransmitControlMessages) { IsBackground = true };
            ComThreadTransmitControlMessages.Start();
        }

        ~Backend()
        {
            Dispose();
        }

        public void Dispose()
        {
            IsDisposed = true;

            Config.SaveConfig();

            ComThreadReceiveImages?.Join(5000);

            ComThreadReceiveControlMessages?.Join(5000);

            ComThreadTransmitControlMessages?.Join(5000);
        }

        private void ExecuteComThreadTransmitControlMessages()
        {
            PushSocket pushSocket = null;
            try
            {
                pushSocket = new PushSocket();
                pushSocket.Options.SendHighWatermark = 10;
                pushSocket.Connect(this.Config.GetConnectionStringCore4Transmit());

                var watch = Stopwatch.StartNew();

                while (!IsDisposed)
                {
                    NetMq.Messages.BaseMessage baseMessage = null;
                    if (Queue.TryDequeue(out baseMessage))
                    {
                        var buffer = baseMessage.Buffer;
                        if (pushSocket.TrySendFrame(TimeSpan.FromMilliseconds(100), buffer, false))
                        {
                            this.Log($"{baseMessage.MessageType} was transfered to {pushSocket.Options.LastEndpoint}", 0);
                        }
                        else
                        {
                            this.Log("Error sending message");
                        }
                    }
                    else
                    {
                        Thread.Sleep(10);
                    }

                    if (watch.ElapsedMilliseconds > 999)
                    {
                        watch.Restart();
                        SendStateMessage();
                    }
                }
            }
            catch (Exception exp)
            {
                LastException = exp;
            }
            finally
            {
                pushSocket?.Dispose();
            }
        }

        private void ExecuteComThreadReceiveControlMessages()
        {
            try
            {
                using (var subSocket = new SubscriberSocket())
                {
                    subSocket.Options.ReceiveHighWatermark = 50;
                    subSocket.Connect(this.Config.GetConnectionStringCore4Receiving());
                    subSocket.Subscribe(this.Config.Name);
                    subSocket.ReceiveReady += OnReceiveControlMessage;

                    while (!IsDisposed)
                    {
                        var success = subSocket.Poll(TimeSpan.FromSeconds(1));
                    }
                }
            }
            catch (Exception exp)
            {
                LastException = exp;
            }
        }

        private void ExecuteComThreadReceiveImages()
        {
            try
            {
                using (var subSocket = new SubscriberSocket())
                {
                    subSocket.Options.ReceiveHighWatermark = 2;
                    subSocket.Connect(Config.GetConnectionString4Images());
                    subSocket.SubscribeToAnyTopic();
                    subSocket.ReceiveReady += OnReceiveImageMessage;

                    while (!IsDisposed)
                    {
                        var success = subSocket.Poll(TimeSpan.FromSeconds(1));
                    }
                }
            }
            catch (Exception exp)
            {
                LastException = exp;
            }
        }

        private void OnReceiveControlMessage(object sender, NetMQ.NetMQSocketEventArgs e)
        {
            bool more = false;
            var buffer = e.Socket.ReceiveFrameBytes(out more);
            if (!more)
            {
                var message = NetMq.Messages.BaseMessage.FromRawBuffer(buffer);
                if (message == null)
                {
                    this.Log($"Error in {nameof(OnReceiveControlMessage)} - Received message is a null-reference");
                }
                else if (message.GetType() == typeof(NetMq.Messages.StateMessage))
                {
                    CounterStateMessage++;
                    var stateMessage = message as NetMq.Messages.StateMessage;
                    this.Log($"Received StateMessage -> CounterStateMessage: {CounterStateMessage} - State: {stateMessage?.State}", 0);
                }
                else if (message.GetType() == typeof(NetMq.Messages.AcquisitionStartMessage))
                {
                    var acquisitionStartMessage = message as NetMq.Messages.AcquisitionStartMessage;
                    this.Log($"Received AcquisitionStartMessage -> (Box)ID: {acquisitionStartMessage?.ID} - BoxType: {acquisitionStartMessage?.Type}");
                }
                else if (message.GetType() == typeof(NetMq.Messages.ProcessStartMessage))
                {
                    var processStartMessage = message as NetMq.Messages.ProcessStartMessage;
                    this.Log($"Received ProcessStartMessage -> (Box)ID: {processStartMessage?.ID} - BoxType: {processStartMessage?.BoxType}");
                    Task.Run(() => ProcessImages(processStartMessage.ID));
                }
                else if (message.GetType() == typeof(NetMq.Messages.ProcessCancelMessage))
                {
                    var processCancelMessage = message as NetMq.Messages.ProcessCancelMessage;
                    this.Log($"Received ProcessCancelMessage -> (Box)ID: {processCancelMessage?.ID}");
                }
                else
                {
                    this.Log($"Received {message.MessageType}");
                }
            }
        }

        private void OnReceiveImageMessage(object sender, NetMQ.NetMQSocketEventArgs e)
        {
            bool more = false;
            var buffer = e.Socket.ReceiveFrameBytes(out more);
            if (!more)
            {
                var message = NetMq.Messages.BaseMessage.FromRawBuffer(buffer) as NetMq.Messages.ImageMessage;

                if (message != null)
                {
                    QueueImages.Enqueue(message);
                }

                while (QueueImages.Count > 10)
                {
                    NetMq.Messages.ImageMessage tmp;
                    QueueImages.TryDequeue(out tmp);
                }
            }
        }

        private void SendStateMessage()
        {
            var stateMessage = new NetMq.Messages.StateMessage(this.Config.Name, 10);
            Queue.Enqueue(stateMessage);
        }

        private void SendResult(int boxTrackingId, BoxCheckStates boxCheckState, BoxFailureReasons boxFailureReason)
        {
            var processFinishedMessage = new NetMq.Messages.ProcessFinishedMessage(this.Config.Name, boxTrackingId, (int)boxCheckState, (int)boxFailureReason);
            Queue.Enqueue(processFinishedMessage);
        }

        private void ProcessImages(int id)
        {
            try
            {
                List<NetMq.Messages.ImageMessage> images = null;
                var watch = Stopwatch.StartNew();
                while (!IsDisposed)
                {
                    images = QueueImages.ToList().FindAll(item => item.BoxTrackingId == id);
                    if (images?.Count < 1)
                    {
                        Thread.Sleep(10);
                        if (watch.ElapsedMilliseconds > 5000)
                        {
                            break;
                        }
                    }
                    else
                    {
                        break;
                    }
                }

                if (images?.Count > 0)
                {
                    this.Log($"(Box)ID: {id} -> Processing {images?.Count} images...");

                    //****** HIER dann die Bildverarbeitung durchfuehren ******
                    //...
                    //...
                    // !!!In Abhaengigkeit der Konfigurationseinstellung wird aktuell entweder IO oder NIO zurueck gegeben!!!
                    var boxCheckState = Config.ReturnBoxResultIo ? BoxCheckStates.IO : BoxCheckStates.NIO;
                    var boxFailureReason = Config.ReturnBoxResultIo ? BoxFailureReasons.BOX_FAILURE_NONE : BoxFailureReasons.BOX_FAILURE_UNKNOWN;
                    //...
                    //...
                    //****** ENDE ******

                    SendResult(id, boxCheckState, boxFailureReason);

                    this.Log($"(Box)ID: {id} -> Processing finished! BoxCheckState: {boxCheckState} / BoxFailureReason: {boxFailureReason}");
                }
            }
            catch (Exception exp)
            {
                LastException = exp;
            }
        }
    }
}