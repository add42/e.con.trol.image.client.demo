using Newtonsoft.Json.Linq;
using System;
using System.Drawing;
using System.IO;

namespace E.CON.TROL.CHECK.DEMO
{
    static class Extensions
    {
        public static string GetLocalStorageDirectory(this object obj)
        {
            object[] arr = obj.GetType().Assembly.GetCustomAttributes(typeof(System.Runtime.InteropServices.GuidAttribute), false);

            var assemblyGuidString = "+++";
            if (arr.Length > 0)
            {
                assemblyGuidString = ((System.Runtime.InteropServices.GuidAttribute)arr[0]).Value;
            }

            var path = Path.Combine(Path.GetTempPath(), assemblyGuidString);

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            return path;
        }

        public static Bitmap GetBitmap(this NetMq.Messages.ImageMessage imageMessage)
        {
            using (var memstream = new MemoryStream(imageMessage.Buffer, imageMessage.DataStartIndex, imageMessage.DataLength))
            {
                return new Bitmap(memstream);
            }
        }
    }
}
