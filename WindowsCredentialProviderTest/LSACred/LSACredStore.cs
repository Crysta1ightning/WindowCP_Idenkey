using CredentialProvider.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace WindowsCredentialProviderTest.LSACred
{
    public sealed class LSACredStore
    {
        private string idenkeyID;
        private string password;

        public LSACredStore()
        {

        }
        private bool StoreCredentialsToLSA(string key, string pwd)
        {
            LSA_OBJECT_ATTRIBUTES objectAttributes = new LSA_OBJECT_ATTRIBUTES();
            objectAttributes.Length = Marshal.SizeOf(objectAttributes);
            var ntsResult = LsaOpenPolicy(IntPtr.Zero, ref objectAttributes, 1, out IntPtr policyHandle);
            if (ntsResult != 0)
            {
                return false;
            }
            if (CreatePrivateDataObject(policyHandle, key, pwd))
            {
                LsaClose(policyHandle);
                return true;
            }
            LsaClose(policyHandle);
            return false;
        }

        public void StorePassword(string userSid, string pwd)
        {
            string key = "L$" + userSid + "_Password";
            StoreCredentialsToLSA(key, pwd);
        }
        public void StoreIdenkeyID(string userSid, string idenkeyID)
        {
            string key = "L$" + userSid + "_IdenkeyID";
            StoreCredentialsToLSA(key, idenkeyID);
        }

        private string FetchCredentialsFromLSA(string key)
        {
            LSA_OBJECT_ATTRIBUTES objectAttributes = new LSA_OBJECT_ATTRIBUTES();
            objectAttributes.Length = Marshal.SizeOf(objectAttributes);
            var ntsResult = LsaOpenPolicy(IntPtr.Zero, ref objectAttributes, 1, out IntPtr policyHandle);
            if (ntsResult != 0)
            {
                return null;
            }
            string password = RetrievePrivateDataObject(policyHandle, key);
            LsaClose(policyHandle);
            return password;
        }

        public string FetchPassword(string userSid)
        {
            string key = "L$" + userSid + "_Password";
            return FetchCredentialsFromLSA(key);
        }

        public string FetchIdenkeyID(string userSid)
        {
            string key = "L$" + userSid + "_IdenkeyID";
            return FetchCredentialsFromLSA(key);
        }

        public void CleanLSA(string userSid)
        {
            // clean the data in LSA
            StorePassword(userSid, null);
            StoreIdenkeyID(userSid, null);
        }
        public string GetIdenkeyID()
        {
            return idenkeyID;
        }

        public string GetPassword()
        {
            return password;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct LSA_UNICODE_STRING
        {
            public ushort Length;
            public ushort MaximumLength;
            public IntPtr Buffer;
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct LSA_OBJECT_ATTRIBUTES
        {
            public int Length;
            public IntPtr RootDirectory;
            public IntPtr ObjectName;
            public int Attributes;
            public IntPtr SecurityDescriptor;
            public IntPtr SecurityQualityOfService;
        }
        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern int LsaOpenPolicy(
            IntPtr SystemName,
            ref LSA_OBJECT_ATTRIBUTES ObjectAttributes,
            int AccessMask,
            out IntPtr PolicyHandle
        );
        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern int LsaStorePrivateData(
            IntPtr PolicyHandle,
            ref LSA_UNICODE_STRING KeyName,
            ref LSA_UNICODE_STRING PrivateData
        );
        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern int LsaRetrievePrivateData(
            IntPtr PolicyHandle,
            ref LSA_UNICODE_STRING KeyName,
            out IntPtr PrivateData
        );
        [DllImport("advapi32.dll", SetLastError = false)]
        public static extern int LsaNtStatusToWinError(int Status);
        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern int LsaFreeMemory(IntPtr Buffer);

        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern int LsaClose(IntPtr PolicyHandle);
        public static bool InitLsaString(ref LSA_UNICODE_STRING lsaString, string value)
        {
            lsaString.Length = (ushort)(value.Length * sizeof(char));
            lsaString.MaximumLength = (ushort)((value.Length + 1) * sizeof(char));
            lsaString.Buffer = Marshal.StringToHGlobalUni(value);
            return true;
        }
        public static bool CreatePrivateDataObject(IntPtr policyHandle, string keyName, string privateData)
        {
            LSA_UNICODE_STRING lucKeyName = new LSA_UNICODE_STRING();
            LSA_UNICODE_STRING lucPrivateData = new LSA_UNICODE_STRING();

            if (!InitLsaString(ref lucKeyName, keyName) || !InitLsaString(ref lucPrivateData, privateData))
            {
                Console.WriteLine("Failed InitLsaString");
                return false;
            }

            int ntsResult = LsaStorePrivateData(policyHandle, ref lucKeyName, ref lucPrivateData);
            if (ntsResult != 0)
            {
                Console.WriteLine("Store private object failed: " + LsaNtStatusToWinError(ntsResult));
                return false;
            }

            Console.WriteLine("Private data object created and stored successfully.");
            return true;
        }
        public static string RetrievePrivateDataObject(IntPtr policyHandle, string keyName)
        {
            LSA_UNICODE_STRING lucKeyName = new LSA_UNICODE_STRING();
            IntPtr privateDataPtr;

            if (!InitLsaString(ref lucKeyName, keyName))
            {
                // Console.WriteLine("Failed InitLsaString");
                return null;
            }

            int ntsResult = LsaRetrievePrivateData(policyHandle, ref lucKeyName, out privateDataPtr);
            if (ntsResult != 0)
            {
                // Console.WriteLine("Retrieve private object failed: " + LsaNtStatusToWinError(ntsResult));
                return null;
            }

            LSA_UNICODE_STRING retrievedData = Marshal.PtrToStructure<LSA_UNICODE_STRING>(privateDataPtr);
            string data = Marshal.PtrToStringUni(retrievedData.Buffer, retrievedData.Length/sizeof(char));

            LsaFreeMemory(privateDataPtr);

            // Console.WriteLine("Retrieved private data: " + data);
            return data;
        }
    }
}
