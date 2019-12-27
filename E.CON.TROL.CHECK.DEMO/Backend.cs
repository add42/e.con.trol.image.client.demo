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
        public Config Config { get; }

        Thread CommunicationThread1 { get; }

        Thread CommunicationThread2 { get; }

        Thread CommunicationSendToCore { get; }

        Thread ThreadProcessing { get; }

        ConcurrentQueue<NetMq.Messages.BaseMessage> Queue { get; } = new ConcurrentQueue<NetMq.Messages.BaseMessage>();

        public ConcurrentQueue<NetMq.Messages.ImageMessage> QueueImages { get; } = new ConcurrentQueue<NetMq.Messages.ImageMessage>();

        public bool IsDisposed { get; private set; }

        public Exception LastException { get; private set; }

        public int CounterStateMessage { get; private set; } = 0;

        public event EventHandler<string> LogEventOccured;

        public Backend()
        {
            Config = Config.LoadConfig();

            CommunicationThread1 = new Thread(RunImageCommunication) { IsBackground = true };
            CommunicationThread1.Start();

            CommunicationThread2 = new Thread(RunControlCommunication) { IsBackground = true };
            CommunicationThread2.Start();

            CommunicationSendToCore = new Thread(RunPushCommunication) { IsBackground = true };
            CommunicationSendToCore.Start();
        }

        ~Backend()
        {
            Dispose();
        }

        public void Dispose()
        {
            IsDisposed = true;

            Config.SaveConfig();

            CommunicationThread1?.Join(5000);

            CommunicationThread2?.Join(5000);

            CommunicationSendToCore?.Join(5000);
        }

        private void Log(string message, int level = 1)
        {
            if (!string.IsNullOrEmpty(message))
            {
                if (level >= this.Config.LogLevel)
                {
                    LogEventOccured?.Invoke(this, $"{DateTime.Now.ToString("HH-mm-ss,fff")} - {message}");
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

        private void RunPushCommunication()
        {
            PushSocket pushSocket = null;
            try
            {
                pushSocket = new PushSocket();
                pushSocket.Options.SendHighWatermark = 10;
                pushSocket.Connect(this.Config.ConnectionStringCoreTransmit);

                var watch = Stopwatch.StartNew();

                while (!IsDisposed)
                {
                    NetMq.Messages.BaseMessage baseMessage = null;
                    if (Queue.TryDequeue(out baseMessage))
                    {
                        var buffer = baseMessage.Buffer;
                        if (pushSocket.TrySendFrame(TimeSpan.FromMilliseconds(100), buffer, false))
                        {
                            Log($"{baseMessage.MessageType} was transfered to {pushSocket.Options.LastEndpoint}", 0);
                        }
                        else
                        {
                            Log("Error sending message");
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

        private void RunControlCommunication()
        {
            try
            {
                using (var subSocket = new SubscriberSocket())
                {
                    subSocket.Options.ReceiveHighWatermark = 50;
                    subSocket.Connect(this.Config.ConnectionStringCoreReceive);
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

        private void RunImageCommunication()
        {
            try
            {
                using (var subSocket = new SubscriberSocket())
                {
                    subSocket.Options.ReceiveHighWatermark = 10;
                    subSocket.Connect("tcp://localhost:55565");
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
                        if (watch.ElapsedMilliseconds > 2000)
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
                    Log($"(Box)ID: {id} -> Processing {images?.Count} images...");

                    //****** HIER dann die Bildverarbeitung durchfuehren ******
                    //...
                    //...
                    //...
                    //...
                    //****** ENDE ******

                    SendResult(id, BoxCheckStates.IO, BoxFailureReasons.BOX_FAILURE_NONE);
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
                    Log($"Error in {nameof(OnReceiveControlMessage)} - Received message is a null-reference");
                }
                else if (message.GetType() == typeof(NetMq.Messages.StateMessage))
                {
                    CounterStateMessage++;
                    var stateMessage = message as NetMq.Messages.StateMessage;
                    Log($"Received StateMessage -> CounterStateMessage: {CounterStateMessage} - State: {stateMessage?.State}", 0);
                }
                else if (message.GetType() == typeof(NetMq.Messages.AcquisitionStartMessage))
                {
                    var acquisitionStartMessage = message as NetMq.Messages.AcquisitionStartMessage;
                    Log($"Received AcquisitionStartMessage -> (Box)ID: {acquisitionStartMessage?.ID} - BoxType: {acquisitionStartMessage?.Type}");
                }
                else if (message.GetType() == typeof(NetMq.Messages.ProcessStartMessage))
                {
                    var processStartMessage = message as NetMq.Messages.ProcessStartMessage;
                    Log($"Received ProcessStartMessage -> (Box)ID: {processStartMessage?.ID} - BoxType: {processStartMessage?.BoxType}");
                    Task.Run(() => ProcessImages(processStartMessage.ID));
                }
                else if (message.GetType() == typeof(NetMq.Messages.ProcessCancelMessage))
                {
                    var processCancelMessage = message as NetMq.Messages.ProcessCancelMessage;
                    Log($"Received ProcessCancelMessage -> (Box)ID: {processCancelMessage?.ID}");
                }
                else
                {
                    Log($"Received {message.MessageType}");
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
    }
}