using Fido2Net.Interop;
using Fido2Net.Util;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Fido2Net
{
    /// <summary>
    /// A class representing a FIDO2 capable device
    /// </summary>
    public sealed unsafe class FidoDevice : IDisposable
    {
        #region Variables

        private fido_dev_t* _native;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the capabilities of this device
        /// </summary>
        public FidoCapabilities Flags => Native.fido_dev_flags(_native);

        /// <summary>
        /// Gets whether or not this device supports FIDO2
        /// </summary>
        public bool IsFido2 => Native.fido_dev_is_fido2(_native);

        /// <summary>
        /// Gets the protocol version that this device supports
        /// </summary>
        public byte Protocol => Native.fido_dev_protocol(_native);

        /// <summary>
        /// Gets the retry count remaining for this device.  If the retry coutn
        /// is exhausted the device will need to be factory reset before continuing.
        /// </summary>
        /// <exception cref="CtapException">Thrown if an error occurs while retrieving the retry count</exception>
        public int RetryCount
        {
            get {
                int retVal;
                Native.fido_dev_get_retry_count(_native, &retVal).Check();
                return retVal;
            }
        }

        /// <summary>
        /// Gets the version of this device
        /// </summary>
        public Version Version
        {
            get
            {
                var major = Native.fido_dev_major(_native);
                var minor = Native.fido_dev_minor(_native);
                var build = Native.fido_dev_build(_native);
                return new Version(major, minor, build);
            }
        }

        #endregion

        #region Constructors

        static FidoDevice()
        {
            Init.Call();
        }

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <exception cref="OutOfMemoryException" />
        public FidoDevice()
        {
            _native = Native.fido_dev_new();
            if (_native == null) {
                throw new OutOfMemoryException();
            }
        }

        /// <summary>
        /// Finalizer
        /// </summary>
        ~FidoDevice()
        {
            ReleaseUnmanagedResources();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// A cast operator to use this object as a native handle
        /// </summary>
        /// <param name="dev">The object to use as a native handle</param>
        public static explicit operator fido_dev_t*(FidoDevice dev)
        {
            return dev._native;
        }

        /// <summary>
        /// Closes the device, preventing further use
        /// </summary>
        /// <exception cref="CtapException">Thrown if an error occurs while closing</exception>
        public void Close() => Native.fido_dev_close(_native).Check();

        /// <summary>
        /// Forces the use of the FIDO2 standard when generating credentials and assertions
        /// </summary>
        public void ForceFido2() => Native.fido_dev_force_fido2(_native);

        /// <summary>
        /// Forces the use of the U2F standard when generating credentials and assertions
        /// </summary>
        public void ForceU2F() => Native.fido_dev_force_u2f(_native);

        /// <summary>
        /// Uses the device to generate an assertion
        /// </summary>
        /// <param name="assert">The assertion object with its input properties properly set</param>
        /// <param name="pin">The pin set on the device, if applicable</param>
        /// <exception cref="CtapException">Thrown if an error occurs while generating the assertion</exception>
        public void GetAssert(FidoAssertion assert, string pin) =>
            Native.fido_dev_get_assert(_native, (fido_assert_t*) assert, pin).Check();

        /// <summary>
        /// Gets the extended information about this device
        /// </summary>
        /// <returns>The extended information about this device</returns>
        /// <exception cref="CtapException">Thrown if an error occurs while retrieving the information</exception>
        public FidoCborInfo GetCborInfo()
        {
            var retVal = new FidoCborInfo();
            Native.fido_dev_get_cbor_info(_native, (fido_cbor_info_t*) retVal).Check();

            return retVal;
        }

        /// <summary>
        /// Uses the device to generate a credential object
        /// </summary>
        /// <param name="credential">The credential object with its input properties set</param>
        /// <param name="pin">The pin of the device, if applicable</param>
        /// <exception cref="CtapException">Thrown if an error occurs while generating the credential</exception>
        public void MakeCredential(FidoCredential credential, string pin) =>
            Native.fido_dev_make_cred(_native, (fido_cred_t*) credential, pin).Check();

        /// <summary>
        /// Opens the device at the given path (to find the path of a device, use
        /// <see cref="FidoDeviceInfoList"/>
        /// </summary>
        /// <param name="path">The path of the device</param>
        /// <exception cref="CtapException">Thrown if an error occurs while opening the device</exception>
        public void Open(string path) => Native.fido_dev_open(_native, path).Check();

        /// <summary>
        /// Performs a factory reset on the device
        /// </summary>
        /// <exception cref="CtapException">Thrown if an error occurs while resetting</exception>
        public void Reset() => Native.fido_dev_reset(_native).Check();

        /// <summary>
        /// Sets the pin on a device
        /// </summary>
        /// <param name="oldPin">The old pin</param>
        /// <param name="pin">The new pin</param>
        /// <exception cref="CtapException">Thrown if an error occurs while setting the pin</exception>
        public void SetPin(string oldPin, string pin) => Native.fido_dev_set_pin(_native, pin, oldPin).Check();


        public ulong GetDeviceMetadata(string pin)
        {
            var metadata = Native.fido_credman_metadata_new();
            var rk = Native.fido_credman_rk_new();
            try
            {
                int returnval;
                if((returnval = Native.fido_credman_get_dev_metadata(_native, metadata, pin)) != 0)
                {
                    throw new Exception($"Error getting device metadata: {returnval}");
                }

                //return 0;

                
                return Native.fido_credman_rk_existing(metadata);
                //return Native.fido_credman_rk_count(metadata);
            }
            finally
            {
                //TODO: why does this not work?
                //Native.fido_credman_metadata_free(metadata);
                Native.fido_credman_rk_free(rk);
            }
        }

        public IList<String> GetRPsWithDiscoverableCredentials(string pin)
        {
            var rps = Native.fido_credman_rp_new();
            try
            {
                int retval = Native.fido_credman_get_dev_rp(_native, rps, pin);
                if(retval != 0)
                {
                    throw new Exception($"Error getting rps for device: {retval}");
                }

                var rpLenPtr = Native.fido_credman_rp_count(rps);
                var result = new List<String>();
                for(ulong i = 0; i< rpLenPtr.ToUInt64(); i++)
                {
                    string id = Native.fido_credman_rp_id(rps, new UIntPtr(i));
                    result.Add(id);
                }
                return result;
            }
            finally
            {
                Native.fido_credman_rp_free(rps);
            }
        }

        public ulong CountCredentialsForRP(string rp, string pin)
        {
            var rks = Native.fido_credman_rk_new();
            try
            {
                int retval = Native.fido_credman_get_dev_rk(_native, rks, pin);
                if (retval != 0)
                {
                    throw new Exception($"Error getting rks for RP {rp}: {retval}");
                }

                var rpLenPtr = Native.fido_credman_rk_count(rks);
                return rpLenPtr.ToUInt64();
            }
            finally
            {
                Native.fido_credman_rk_free(rks);
            }
        }

        public List<byte[]> CredentialsForRP(string rp, string pin)
        {
            var rks = Native.fido_credman_rk_new();
            try
            {
                int retval = Native.fido_credman_get_dev_rk(_native, rks, pin);
                if (retval != 0)
                {
                    throw new Exception($"Error getting rks for RP {rp}: {retval}");
                }


                List<byte[]> result = new List<byte[]>();
                
                var rpLenPtr = Native.fido_credman_rk_count(rks);

                for(ulong i = 0; i< rpLenPtr.ToUInt64(); i++)
                {
                    var credN = Native.fido_credman_rk(rks, new UIntPtr(i));
                    var cred = new FidoCredential(credN);
                    try
                    {
                        var idN = cred.Id;
                        byte[] id = new byte[idN.Length];
                        idN.CopyTo(id);
                        result.Add(id);
                    }
                    finally
                    {
                        cred.Dispose();
                    }
                }
                return result;
            }
            finally
            {
                Native.fido_credman_rk_free(rks);
            }
        }



        public void SetTimeout(TimeSpan timeout) => Native.fido_dev_set_timeout(_native, (int)timeout.TotalMilliseconds);

        #endregion

        #region Private Methods

        private void ReleaseUnmanagedResources()
        {
            var native = _native;
            Native.fido_dev_free(&native);
            _native = null;
        }

        #endregion

        #region IDisposable

        /// <inheritdoc />
        public void Dispose()
        {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }

        #endregion

        #region Overrides

        /// <inheritdoc />
        public override string ToString()
        {
            return $"proto: 0x{Protocol:X2}, version: {Version}, caps: {Flags}";
        }

        #endregion
    }
}