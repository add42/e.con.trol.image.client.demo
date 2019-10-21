using NetMQ;
using NetMQ.Sockets;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace E.CON.TROL.CHECK.DEMO
{
    class Backend : IDisposable
    {
        Thread CommunicationThread1 { get; }

        Thread CommunicationThread2 { get; }

        public bool IsDisposed { get; private set; }

        public bool IsRunning { get; private set; }

        public List<string> ImageFiles { get; } = new List<string>();

        public string BoxId { get; private set; }

        public string BoxType { get; private set; }

        public Exception LastException { get; private set; }

        public Backend()
        {
            CommunicationThread1 = new Thread(RunImageCommunication) { IsBackground = true };
            CommunicationThread1.Start();

            CommunicationThread2 = new Thread(RunControlCommunication) { IsBackground = true };
            CommunicationThread2.Start();
        }

        ~Backend()
        {
            Dispose();
        }

        public void Dispose()
        {
            IsDisposed = true;

            ImageFiles.ForEach(file =>
            {
                try
                {
                    File.Delete(file);
                }
                catch { }
            });

            ImageFiles.Clear();

            CommunicationThread1?.Join(5000);

            CommunicationThread2?.Join(5000);
        }

        private void RunControlCommunication()
        {
            try
            {
                IsRunning = true;
                using (var subSocket = new SubscriberSocket())
                {
                    subSocket.Options.ReceiveHighWatermark = 50;
                    subSocket.Connect("tcp://localhost:55555");
                    subSocket.SubscribeToAnyTopic();
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
            finally
            {
                IsRunning = false;
            }
        }

        private void RunImageCommunication()
        {
            try
            {
                IsRunning = true;
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
            finally
            {
                IsRunning = false;
            }
        }



        private void OnReceiveControlMessage(object sender, NetMQ.NetMQSocketEventArgs e)
        {
            bool more = false;
            var buffer = e.Socket.ReceiveFrameBytes(out more);
            if (!more)
            {
                int infoLength = BitConverter.ToInt32(buffer, 0);

                using (MemoryStream ms = new MemoryStream(buffer, 4, infoLength))
                {
                    using (BsonDataReader reader = new BsonDataReader(ms))
                    {
                        JsonSerializer serializer = new JsonSerializer();

                        var jObject = serializer.Deserialize(reader) as JObject;

                        var messageType = jObject?.GetPropertyValueFromJObject("MessageType");
                        if (messageType.StartsWith("NetMq.Messages.StateMessage"))
                        {

                        }
                        else if (messageType.StartsWith("NetMq.Messages.AcquisitionStartMessage"))
                        {

                        }
                        else if (messageType.StartsWith("NetMq.Messages.ProcessStartMessage"))
                        {
                            BoxId = jObject?.GetPropertyValueFromJObject("ID");
                            BoxType = jObject?.GetPropertyValueFromJObject("BoxType");
                        }
                        else
                        {

                        }
                    }
                }
            }
        }

        private void OnReceiveImageMessage(object sender, NetMQ.NetMQSocketEventArgs e)
        {
            bool more = false;
            var buffer = e.Socket.ReceiveFrameBytes(out more);
            if (!more)
            {
                var path = Path.Combine(Path.GetTempPath(), $"{DateTime.Now.Ticks}.bmp");
                int infoLength = BitConverter.ToInt32(buffer, 0);
                int fileLength = buffer.Length - infoLength - 4;

                using (var file = File.OpenWrite(path))
                {
                    using (var writer = new BinaryWriter(file))
                    {
                        writer.Write(buffer, buffer.Length - fileLength, fileLength);
                    }
                }

                ImageFiles.Add(path);

                while (ImageFiles.Count > 10)
                {
                    try
                    {
                        var file = ImageFiles[0];
                        File.Delete(file);
                        ImageFiles.RemoveAt(0);
                    }
                    catch { }
                }
            }
        }
    }
}