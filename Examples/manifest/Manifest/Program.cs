using System;
using System.Threading;
using Fido2Net;
using Fido2Net.Interop;

namespace Manifest
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var devlist = new FidoDeviceInfoList(64))
            {
                foreach (var di in devlist)
                {
                    if (di.Path.Equals("windows://hello")) continue;

                    Console.WriteLine($"Using device {di}");

                    using (var dev = new FidoDevice())
                    {
                        dev.Open(di.Path);
                        ListCredentials(dev);
                    }

                    
                }
            }
        }

        static void ListCredentials(FidoDevice dev)
        {
            try
            {
                var deviceMetadata = dev.GetDeviceMetadata("pin");
                Console.WriteLine($"Existing residential keys {deviceMetadata}");
            }catch(Exception e)
            {
                Console.WriteLine("");
            }
        }
    }
}
