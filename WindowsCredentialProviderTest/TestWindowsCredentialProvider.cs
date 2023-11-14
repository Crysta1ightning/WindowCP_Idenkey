namespace WindowsCredentialProviderTest
{
    using System;
    using System.Runtime.InteropServices;
    using CredentialProvider.Interop;

    [ComVisible(true)]
    [Guid(Constants.CredentialProviderUID)]
    [ClassInterface(ClassInterfaceType.None)]
    [ProgId("Rebootify.TestWindowsCredentialProvider")]
    public class TestWindowsCredentialProvider : ITestWindowsCredentialProvider
    {
        private _CREDENTIAL_PROVIDER_USAGE_SCENARIO usageScenario = _CREDENTIAL_PROVIDER_USAGE_SCENARIO.CPUS_INVALID;
        private TestWindowsCredentialProviderTile[] credentialTiles;
        internal ICredentialProviderUserArray users = null;
        internal ICredentialProviderEvents CredentialProviderEvents;
        internal uint CredentialProviderEventsAdviseContext = 0;
        //private bool shouldRecreate = false;

        public TestWindowsCredentialProvider()
        {
            Log.LogText("TestWindowsCredentialProvider: Created object");
        }

        public int SetUsageScenario(_CREDENTIAL_PROVIDER_USAGE_SCENARIO cpus, uint dwFlags)
        {
            Log.LogMethodCall();

            usageScenario = cpus;

            switch (cpus)
            {
                case _CREDENTIAL_PROVIDER_USAGE_SCENARIO.CPUS_CREDUI:
                case _CREDENTIAL_PROVIDER_USAGE_SCENARIO.CPUS_LOGON:
                    //shouldRecreate = true;
                    return HResultValues.S_OK;
                case _CREDENTIAL_PROVIDER_USAGE_SCENARIO.CPUS_UNLOCK_WORKSTATION:
                case _CREDENTIAL_PROVIDER_USAGE_SCENARIO.CPUS_CHANGE_PASSWORD:
                case _CREDENTIAL_PROVIDER_USAGE_SCENARIO.CPUS_PLAP:
                case _CREDENTIAL_PROVIDER_USAGE_SCENARIO.CPUS_INVALID:
                    return HResultValues.E_NOTIMPL;
                default:
                    return HResultValues.E_INVALIDARG;
            }
        }

        public int SetSerialization(ref _CREDENTIAL_PROVIDER_CREDENTIAL_SERIALIZATION pcpcs)
        {
            Log.LogMethodCall();
            return HResultValues.E_NOTIMPL;
        }

        public int Advise(ICredentialProviderEvents pcpe, uint upAdviseContext)
        {
            Log.LogMethodCall();

            if (pcpe != null)
            {
                CredentialProviderEventsAdviseContext = upAdviseContext;
                CredentialProviderEvents = pcpe;
                var intPtr = Marshal.GetIUnknownForObject(pcpe);
                Marshal.AddRef(intPtr);
            }

            return HResultValues.S_OK;
        }

        public int UnAdvise()
        {
            Log.LogMethodCall();

            if (CredentialProviderEvents != null)
            {
                var intPtr = Marshal.GetIUnknownForObject(CredentialProviderEvents);
                Marshal.Release(intPtr);
                CredentialProviderEvents = null;
                CredentialProviderEventsAdviseContext = 0;
            }

            return HResultValues.S_OK;
        }

        public int GetFieldDescriptorCount(out uint pdwCount)
        {
            Log.LogMethodCall();
            pdwCount = (uint)credentialTiles[0].CredentialProviderFieldDescriptorList.Count;
            return HResultValues.S_OK;
        }

        public int GetFieldDescriptorAt(uint dwIndex, [Out] IntPtr ppcpfd) /* _CREDENTIAL_PROVIDER_FIELD_DESCRIPTOR** */
        {
            Log.LogMethodCall();

            if (dwIndex >= credentialTiles[0].CredentialProviderFieldDescriptorList.Count)
            {
                return HResultValues.E_INVALIDARG;
            }

            var listItem = credentialTiles[0].CredentialProviderFieldDescriptorList[(int) dwIndex];
            var pcpfd = Marshal.AllocCoTaskMem(Marshal.SizeOf(listItem)); /* _CREDENTIAL_PROVIDER_FIELD_DESCRIPTOR* */
            Marshal.StructureToPtr(listItem, pcpfd, false); /* pcpfd = &CredentialProviderFieldDescriptorList */
            Marshal.StructureToPtr(pcpfd, ppcpfd, false); /* *ppcpfd = pcpfd */

            return HResultValues.S_OK;
        }

        public int GetCredentialCount(out uint pdwCount, out uint pdwDefault, out int pbAutoLogonWithDefault)
        {
            Log.LogMethodCall();

            pdwCount = (uint)credentialTiles.Length; // Credential tiles number
            pdwDefault = unchecked ((uint)0);
            pbAutoLogonWithDefault = 0; // Try to auto-logon when all credential managers are enumerated (before the tile selection)

            return HResultValues.S_OK;
        }

        public int GetCredentialAt(uint dwIndex, out ICredentialProviderCredential ppcpc)
        {
            Log.LogMethodCall();

            ppcpc = credentialTiles[dwIndex];
            
            return HResultValues.S_OK;
        }

        public int SetUserArray(ICredentialProviderUserArray newUsers)
        {
            if (users != null)
            {
                Marshal.ReleaseComObject(users);
            }
            users = newUsers;
            Marshal.AddRef(Marshal.GetIUnknownForObject(users));
            CreateEnumeratedCredentials();
            return HResultValues.S_OK;
        }

        public int CreateEnumeratedCredentials()
        {
            if (users != null)
            {
                users.GetCount(out uint userCount);
                if (userCount > 0)
                {
                    credentialTiles = new TestWindowsCredentialProviderTile[userCount];
                    for (uint i=0; i<userCount; i++)
                    {
                        credentialTiles[i] = new TestWindowsCredentialProviderTile(this, usageScenario);
                        users.GetAt(i, out ICredentialProviderUser curUser);
                        credentialTiles[i].Init(curUser);

                    }
                    return HResultValues.S_OK;
                    
                }
            }
            return HResultValues.E_FAIL;
        }
    }
}
