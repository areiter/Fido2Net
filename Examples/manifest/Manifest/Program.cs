using System;
using System.Net.NetworkInformation;
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
                String pin = "1234";
                var deviceMetadata = dev.GetDeviceMetadata(pin);
                Console.WriteLine($"Existing residential keys {deviceMetadata}");
                
                var rpIds = dev.GetRPsWithDiscoverableCredentials(pin);
                Console.WriteLine("Following Relying Parties with Discoverable credentials exist:");

                foreach(String rp in rpIds)
                {
                    Console.WriteLine(rp);
                }

                String myRP = "dummytesting";
                //dev.CountCredentialsForRP(myRP, pin);

                //var credentials = dev.CredentialsForRP(myRP, pin);

                //if (!rpIds.Contains(myRP))
                {
                    MakeDiscoverableCredential(dev, myRP, pin);
                }

            }catch(Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        static void MakeDiscoverableCredential(FidoDevice dev, String rp, string pin)
        {
            byte[] Cd = {
            0xf8, 0x64, 0x57, 0xe7, 0x2d, 0x97, 0xf6, 0xbb,
            0xdd, 0xd7, 0xfb, 0x06, 0x37, 0x62, 0xea, 0x26,
            0x20, 0x44, 0x8e, 0x69, 0x7c, 0x03, 0xf2, 0x31,
            0x2f, 0x99, 0xdc, 0xaf, 0x3e, 0x8a, 0x91, 
        };

            byte[] UserId = {
            0x77, 0x1c, 0x78, 0x60, 0xad, 0x88, 0xd2, 0x63,
            0x32, 0x62, 0x2a, 0xf1, 0x74, 0x5d, 0xed, 0xb2,
            0xe7, 0xa4, 0x2b, 0x44, 0x89, 0x29, 0x39, 0xc5,
            0x56, 0x64, 0x01, 0x27, 0x0d, 0xbb, 0xc4, 
        };


            using (var cred = new FidoCredential())
            {
                cred.SetType(FidoCose.ES256);
                cred.SetExtensions(FidoExtensions.None);
                cred.SetClientData(Cd);
                cred.SetResidentKeyRequired(true);
                cred.SetUserVerificationRequried(true);

                cred.Rp = new FidoCredentialRp
                {
                    Id = rp
                };

                cred.SetUser(new FidoCredentialUser
                {
                    Id = UserId                    
                });

                dev.MakeCredential(cred, pin);

                cred.Verify();

            }
        }
    }
}
