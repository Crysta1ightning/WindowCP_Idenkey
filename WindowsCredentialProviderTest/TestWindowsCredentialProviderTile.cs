// Uncomment for autologin
// #define AUTOLOGIN

using CredentialProvider.Interop;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using WindowsCredentialProviderTest.Internet;
using WindowsCredentialProviderTest.LSACred;
using WindowsCredentialProviderTest.OnDemandLogon;
using WindowsCredentialProviderTest.QRCode;

namespace WindowsCredentialProviderTest
{
    [ComVisible(true)]
    [Guid(Constants.CredentialProviderTileUID)]
    [ClassInterface(ClassInterfaceType.None)]
    public sealed class TestWindowsCredentialProviderTile : ITestWindowsCredentialProviderTile
    {
        public List<_CREDENTIAL_PROVIDER_FIELD_DESCRIPTOR> CredentialProviderFieldDescriptorList = new List<_CREDENTIAL_PROVIDER_FIELD_DESCRIPTOR> {
            new _CREDENTIAL_PROVIDER_FIELD_DESCRIPTOR
            {
                cpft = _CREDENTIAL_PROVIDER_FIELD_TYPE.CPFT_TILE_IMAGE,
                dwFieldID = 0,
                pszLabel = "Icon",
                guidFieldType = Guid.Parse("2d837775-f6cd-464e-a745-482fd0b47493") // CPFG_CREDENTIAL_PROVIDER_LOGO
            },
            new _CREDENTIAL_PROVIDER_FIELD_DESCRIPTOR
            {
                cpft = _CREDENTIAL_PROVIDER_FIELD_TYPE.CPFT_SMALL_TEXT,
                dwFieldID = 1,
                pszLabel = "Rebootify Awesomeness",
                guidFieldType = Guid.Parse("286BBFF3-BAD4-438F-B007-79B7267C3D48") // CPFG_CREDENTIAL_PROVIDER_LABEL
            },
            new _CREDENTIAL_PROVIDER_FIELD_DESCRIPTOR
            {
                cpft = _CREDENTIAL_PROVIDER_FIELD_TYPE.CPFT_SUBMIT_BUTTON,
                dwFieldID = 2,
                pszLabel = "Login",
            },
            new _CREDENTIAL_PROVIDER_FIELD_DESCRIPTOR
            {
                cpft = _CREDENTIAL_PROVIDER_FIELD_TYPE.CPFT_COMMAND_LINK,
                dwFieldID = 3,
                pszLabel = "Clear Credentials",
            },
            new _CREDENTIAL_PROVIDER_FIELD_DESCRIPTOR
            {
                cpft = _CREDENTIAL_PROVIDER_FIELD_TYPE.CPFT_TILE_IMAGE,
                dwFieldID = 4,
                pszLabel = "", // QRCode
            },
            new _CREDENTIAL_PROVIDER_FIELD_DESCRIPTOR
            {
                cpft = _CREDENTIAL_PROVIDER_FIELD_TYPE.CPFT_PASSWORD_TEXT,
                dwFieldID = 5,
                pszLabel = "Password",
            },

        };

        private readonly TestWindowsCredentialProvider testWindowsCredentialProvider;
        private readonly _CREDENTIAL_PROVIDER_USAGE_SCENARIO usageScenario;
        private ICredentialProviderCredentialEvents credentialProviderCredentialEvents;
        // For users
        private ICredentialProviderUser pcpUser;
        private string userSid;
        //
        private TimerOnDemandLogon timerOnDemandLogon;
        private readonly LSACredStore lsaCredStore;
        private readonly QRCodeBitmap qRCodeBitmap;
        private readonly InternetConnect internet;
        private bool shouldAutoLogin = false;
        private string newPassword;
        private readonly string username;
        private string idenkeyID;
        private string password;
        private bool shouldRegister;
        private bool shouldShowPasswordBox;
        private bool permissionGranted = false;
        private bool alreadyInit = false;
        private bool isConnected;


        public TestWindowsCredentialProviderTile(
            TestWindowsCredentialProvider testWindowsCredentialProvider,
            _CREDENTIAL_PROVIDER_USAGE_SCENARIO usageScenario,
            ICredentialProviderUser pcpUser
        )
        {
            this.testWindowsCredentialProvider = testWindowsCredentialProvider;
            this.usageScenario = usageScenario;
            lsaCredStore = new LSACredStore();
            qRCodeBitmap = new QRCodeBitmap();
            internet = new InternetConnect();

            // get username IdenkeyID password
            this.pcpUser = pcpUser;
            pcpUser.GetSid(out userSid);

            // username
            // PKEY_Identity_UserName is not defined in C#, so I create it manually
            _tagpropertykey propertyKey;
            propertyKey.fmtid = new Guid("{C4322503-78CA-49C6-9ACC-A68E2AFD7B6B}");
            propertyKey.pid = 100;

            pcpUser.GetStringValue(ref propertyKey, out string _username);

            if (!_username.Contains(@"\"))
            {
                // if it is root domain user
                username = @".\" + _username;
            } else
            {
                username = _username;
            }

            // IdenkeyID
            idenkeyID = lsaCredStore.FetchIdenkeyID(userSid);
            if (idenkeyID == null)
            {
                shouldRegister = true;
            }
            else
            {
                shouldRegister = false;
            }

            // password
            password = lsaCredStore.FetchPassword(userSid);
            if (password == null)
            {
                shouldShowPasswordBox = true;
            }
            else
            {
                shouldShowPasswordBox = false;
            }
        }


        public int Advise(ICredentialProviderCredentialEvents pcpce)
        {
            Log.LogMethodCall();

            if (pcpce != null)
            {
                credentialProviderCredentialEvents = pcpce;
                var intPtr = Marshal.GetIUnknownForObject(pcpce);
                Marshal.AddRef(intPtr);
            }

            return HResultValues.S_OK;
        }

        public int UnAdvise()
        {
            Log.LogMethodCall();

            if (credentialProviderCredentialEvents != null)
            {
                var intPtr = Marshal.GetIUnknownForObject(credentialProviderCredentialEvents);
                Marshal.Release(intPtr);
                credentialProviderCredentialEvents = null;
            }

            return HResultValues.S_OK;
        }

        async public void _init()
        {
            // first check internet connection

            isConnected = await internet.CheckInternetConnection();
            _SetStatusText("Connected: " + isConnected);
            if (isConnected)
            {
                string todo = await internet.FetchAPIPostTest();
                _SetStatusText("Connected: " + isConnected + " TODO: " + todo);
            }

            // after setting pcpce, we can call _RegisterIdenkey()
            if (shouldRegister)
            {
                await Task.Run(_RegisterIdenkey);
            }
            else
            {
                await Task.Run(_NotifyIdenkey);
            }
        }

        public int SetSelected(out int pbAutoLogon)
        {
            Log.LogMethodCall();
            if (alreadyInit)
            {
                pbAutoLogon = 0;
                return HResultValues.S_OK;
            }

            alreadyInit = true;
            _init();

            //if (username == null)
            //{
            //    SetUsernameText("Username Not Found");
            //}
            //else
            //{
            //    SetUsernameText(username + " - " + lsaCredStore.GetPassword());
            //}

#if AUTOLOGIN
            if (!shouldAutoLogin)
            {
                timerOnDemandLogon = new TimerOnDemandLogon(
                    testWindowsCredentialProvider.CredentialProviderEvents,
                    credentialProviderCredentialEvents,
                    this,
                    CredentialProviderFieldDescriptorList[1].dwFieldID,
                    testWindowsCredentialProvider.CredentialProviderEventsAdviseContext);

                timerOnDemandLogon.TimerEnded += TimerOnDemandLogon_TimerEnded; // when onTimeEnded() is called, this func is called

                pbAutoLogon = 0;
            }
            else
            {
                // We got the info from the async timer
                pbAutoLogon = 1;
            }
#else
            pbAutoLogon = 0; // Auto-logon when the tile is selected
#endif

            return HResultValues.S_OK;
        }

        private void TimerOnDemandLogon_TimerEnded()
        {
            // Sync other data from your async service here
            shouldAutoLogin = true;
            // when count down is finished, shouldAutoLogin = true
        }

        public int SetDeselected()
        {
            Log.LogMethodCall();

            timerOnDemandLogon?.Dispose();
            timerOnDemandLogon = null;

            return HResultValues.E_NOTIMPL;
        }

        public int GetFieldState(uint dwFieldID, out _CREDENTIAL_PROVIDER_FIELD_STATE pcpfs,
            out _CREDENTIAL_PROVIDER_FIELD_INTERACTIVE_STATE pcpfis)
        {
            Log.LogMethodCall();

            //  var descriptor = CredentialProviderFieldDescriptorList.First(x => x.dwFieldID == dwFieldID);

            pcpfis = _CREDENTIAL_PROVIDER_FIELD_INTERACTIVE_STATE.CPFIS_NONE;
   
            if (dwFieldID == 4 && !shouldRegister) // QRCode
            {
                pcpfs = _CREDENTIAL_PROVIDER_FIELD_STATE.CPFS_HIDDEN;
            }
            else if (dwFieldID == 5 && !shouldShowPasswordBox) // Password
            {
                pcpfs = _CREDENTIAL_PROVIDER_FIELD_STATE.CPFS_HIDDEN;
            }
            else
            {
                pcpfs = _CREDENTIAL_PROVIDER_FIELD_STATE.CPFS_DISPLAY_IN_BOTH;
            }
            return HResultValues.S_OK;
        }

        // Set the values of CPDescriptorList to be their pszLabel, except password
        public int GetStringValue(uint dwFieldID, out string ppsz)
        {
            Log.LogMethodCall();

            var searchFunction = FieldSearchFunctionGenerator(dwFieldID, new[]
            {
                _CREDENTIAL_PROVIDER_FIELD_TYPE.CPFT_SMALL_TEXT,
                _CREDENTIAL_PROVIDER_FIELD_TYPE.CPFT_LARGE_TEXT,
                _CREDENTIAL_PROVIDER_FIELD_TYPE.CPFT_EDIT_TEXT,
                _CREDENTIAL_PROVIDER_FIELD_TYPE.CPFT_PASSWORD_TEXT,
                _CREDENTIAL_PROVIDER_FIELD_TYPE.CPFT_COMMAND_LINK,
            });

            if (!CredentialProviderFieldDescriptorList.Any(searchFunction))
            {
                ppsz = string.Empty;
                return HResultValues.E_NOTIMPL;
            }

            var descriptor = CredentialProviderFieldDescriptorList.First(searchFunction);
            if (descriptor.cpft == _CREDENTIAL_PROVIDER_FIELD_TYPE.CPFT_PASSWORD_TEXT)
            {
                ppsz = string.Empty;
            }
            else
            {
                ppsz = descriptor.pszLabel;
            }
            return HResultValues.S_OK;
        }

        public int GetBitmapValue(uint dwFieldID, IntPtr phbmp)
        {
            Log.LogMethodCall();

            var searchFunction = FieldSearchFunctionGenerator(dwFieldID, new[] { _CREDENTIAL_PROVIDER_FIELD_TYPE.CPFT_TILE_IMAGE });

            if (!CredentialProviderFieldDescriptorList.Any(searchFunction))
            {
                phbmp = IntPtr.Zero;
                return HResultValues.E_NOTIMPL;
            }

            Bitmap tileIcon = null;
            if (dwFieldID == 0) // Icon
            {

                tileIcon = Properties.Resources._480_360_sample;
                Log.LogMethodCall();

            }
            else if (dwFieldID == 4 && shouldRegister) // QRCode
            {
                tileIcon = qRCodeBitmap.GetBitmap();
                Log.LogMethodCall();
            }

            Marshal.WriteIntPtr(phbmp, tileIcon.GetHbitmap());
            return HResultValues.S_OK;
        }

        public int GetCheckboxValue(uint dwFieldID, out int pbChecked, out string ppszLabel)
        {
            Log.LogMethodCall();

            var searchFunction = FieldSearchFunctionGenerator(dwFieldID, new[] { _CREDENTIAL_PROVIDER_FIELD_TYPE.CPFT_CHECKBOX });

            if (!CredentialProviderFieldDescriptorList.Any(searchFunction))
            {
                pbChecked = 0;
                ppszLabel = string.Empty;
                return HResultValues.E_NOTIMPL;
            }

            var descriptor = CredentialProviderFieldDescriptorList.First(searchFunction);
            pbChecked = 0; // TODO: selection state
            ppszLabel = descriptor.pszLabel;

            return HResultValues.E_NOTIMPL;
        }

        public int GetSubmitButtonValue(uint dwFieldID, out uint pdwAdjacentTo)
        {
            Log.LogMethodCall();

            var searchFunction = FieldSearchFunctionGenerator(dwFieldID, new[] { _CREDENTIAL_PROVIDER_FIELD_TYPE.CPFT_SUBMIT_BUTTON });

            if (!CredentialProviderFieldDescriptorList.Any(searchFunction))
            {
                pdwAdjacentTo = 0;
                return HResultValues.E_NOTIMPL;
            }

            var descriptor = CredentialProviderFieldDescriptorList.First(searchFunction);

            pdwAdjacentTo = descriptor.dwFieldID - 1; // TODO: selection state

            return HResultValues.S_OK;
        }

        public int GetComboBoxValueCount(uint dwFieldID, out uint pcItems, out uint pdwSelectedItem)
        {
            Log.LogMethodCall();

            var searchFunction = FieldSearchFunctionGenerator(dwFieldID, new[] { _CREDENTIAL_PROVIDER_FIELD_TYPE.CPFT_COMBOBOX });

            if (!CredentialProviderFieldDescriptorList.Any(searchFunction))
            {
                pcItems = 0;
                pdwSelectedItem = 0;
                return HResultValues.E_NOTIMPL;
            }

            var descriptor = CredentialProviderFieldDescriptorList.First(searchFunction);
            pcItems = 0; // TODO: selection state
            pdwSelectedItem = 0;

            return HResultValues.E_NOTIMPL;
        }

        public int GetComboBoxValueAt(uint dwFieldID, uint dwItem, out string ppszItem)
        {
            Log.LogMethodCall();
            ppszItem = string.Empty;
            return HResultValues.E_NOTIMPL;
        }

        public int SetStringValue(uint dwFieldID, string psz)
        {
            Log.LogMethodCall();
            newPassword = psz;


            return HResultValues.S_OK;
        }

        public int SetCheckboxValue(uint dwFieldID, int bChecked)
        {
            Log.LogMethodCall();

            // TODO: change state

            return HResultValues.E_NOTIMPL;
        }

        public int SetComboBoxSelectedValue(uint dwFieldID, uint dwSelectedItem)
        {
            Log.LogMethodCall();

            // TODO: change state

            return HResultValues.E_NOTIMPL;
        }

        public int CommandLinkClicked(uint dwFieldID)
        {
            Log.LogMethodCall();
            lsaCredStore.CleanLSA(userSid);
            password = lsaCredStore.FetchPassword(userSid);
            if (password == null)
            {
                _SetStatusText("Password is null");
            }
            else
            {
                _SetStatusText("Password: " + lsaCredStore.FetchPassword(userSid));
            }
            
            return HResultValues.S_OK;
        }

        // function I wrote to dispaly debug messages
        public void _SetStatusText(string text)
        {
            Log.LogMethodCall();
            credentialProviderCredentialEvents.SetFieldString(
                    this,
                    CredentialProviderFieldDescriptorList[1].dwFieldID, // 1
                    text);
        }

        public string _PromptPassword()
        {
            _SetStatusText("Prompt Password...");

            if (newPassword == null)
            {
                credentialProviderCredentialEvents.SetFieldState(
                this,
                CredentialProviderFieldDescriptorList[5].dwFieldID, // password
                _CREDENTIAL_PROVIDER_FIELD_STATE.CPFS_DISPLAY_IN_BOTH);
                return null;
            }

            lsaCredStore.StorePassword(userSid, newPassword);
            return newPassword;
        }

        public async Task _RegisterIdenkey()
        {
            
            for (int i = 0; i < 8; i++)
            {
                _SetStatusText("Register..." + i + " seconds");
                await Task.Delay(1000);
            }
            _SetStatusText("Finish Register");

            idenkeyID = "123456";
            lsaCredStore.StoreIdenkeyID(userSid, idenkeyID);

            credentialProviderCredentialEvents.SetFieldState(
            this,
            CredentialProviderFieldDescriptorList[4].dwFieldID, // qrcode
            _CREDENTIAL_PROVIDER_FIELD_STATE.CPFS_HIDDEN);

            await Task.Run(_NotifyIdenkey);
        }

        public async Task _NotifyIdenkey()
        {

            for (int i = 0; i < 8; i++)
            {
                //_SetStatusText("Notify..." + i + " seconds");
                await Task.Delay(1000);
            }
            //_SetStatusText("Finish Notify");
            permissionGranted = true;
        }

        public int GetSerialization(out _CREDENTIAL_PROVIDER_GET_SERIALIZATION_RESPONSE pcpgsr,
            out _CREDENTIAL_PROVIDER_CREDENTIAL_SERIALIZATION pcpcs, out string ppszOptionalStatusText,
            out _CREDENTIAL_PROVIDER_STATUS_ICON pcpsiOptionalStatusIcon)
        {
            Log.LogMethodCall();

            try
            {
                pcpgsr = _CREDENTIAL_PROVIDER_GET_SERIALIZATION_RESPONSE.CPGSR_RETURN_CREDENTIAL_FINISHED;
                pcpcs = new _CREDENTIAL_PROVIDER_CREDENTIAL_SERIALIZATION();

                // Step1: Get Username
                if (username == null)
                {
                    ppszOptionalStatusText = "Failed to get username";
                    _SetStatusText(ppszOptionalStatusText);
                    pcpsiOptionalStatusIcon = _CREDENTIAL_PROVIDER_STATUS_ICON.CPSI_ERROR;
                    return HResultValues.E_FAIL;
                }

                // Step2: Get IdnekeyID
                if (idenkeyID == null)
                {
                    ppszOptionalStatusText = "Not Register Yet";
                    _SetStatusText(ppszOptionalStatusText);
                    pcpsiOptionalStatusIcon = _CREDENTIAL_PROVIDER_STATUS_ICON.CPSI_ERROR;
                    return HResultValues.E_FAIL;
                    
                }

                if (permissionGranted == false)
                {
                    ppszOptionalStatusText = "Permission Not Granted Yet";
                    _SetStatusText(ppszOptionalStatusText);
                    pcpsiOptionalStatusIcon = _CREDENTIAL_PROVIDER_STATUS_ICON.CPSI_ERROR;
                    return HResultValues.E_FAIL;
                }

                // Step3: Get Password
                if (shouldShowPasswordBox)
                {
                    // get password from password box
                    password = _PromptPassword();
                    if (password == null || password == "")
                    {
                        // user has not enter password yet
                        ppszOptionalStatusText = "Failed to get password";
                        _SetStatusText(ppszOptionalStatusText);
                        pcpsiOptionalStatusIcon = _CREDENTIAL_PROVIDER_STATUS_ICON.CPSI_ERROR;
                        return HResultValues.E_FAIL;
                    }
                }
                else
                {
                    // if this doesn't success, the password stored in LSA is wrong
                    // so we should show password box and let user enter correct password
                    shouldShowPasswordBox = true;
                    _PromptPassword();
                }


                var inCredSize = 0;
                var inCredBuffer = Marshal.AllocCoTaskMem(0);

                if (!PInvoke.CredPackAuthenticationBuffer(0, username, password, inCredBuffer, ref inCredSize))
                {
                    Marshal.FreeCoTaskMem(inCredBuffer);
                    inCredBuffer = Marshal.AllocCoTaskMem(inCredSize);

                    if (PInvoke.CredPackAuthenticationBuffer(0, username, password, inCredBuffer, ref inCredSize))
                    {
                        ppszOptionalStatusText = string.Empty;
                        pcpsiOptionalStatusIcon = _CREDENTIAL_PROVIDER_STATUS_ICON.CPSI_SUCCESS;

                        pcpcs.clsidCredentialProvider = Guid.Parse(Constants.CredentialProviderUID);
                        pcpcs.rgbSerialization = inCredBuffer;
                        pcpcs.cbSerialization = (uint)inCredSize;

                        RetrieveNegotiateAuthPackage(out var authPackage);
                        pcpcs.ulAuthenticationPackage = authPackage;

                        return HResultValues.S_OK;
                    }

                    ppszOptionalStatusText = "Failed to pack credentials";
                    pcpsiOptionalStatusIcon = _CREDENTIAL_PROVIDER_STATUS_ICON.CPSI_ERROR;
                    return HResultValues.E_FAIL;
                }
            }
            catch (Exception)
            {
                // In case of any error, do not bring down winlogon
            }
            finally
            {
                shouldAutoLogin = false; // Block auto-login from going full-retard
            }

            pcpgsr = _CREDENTIAL_PROVIDER_GET_SERIALIZATION_RESPONSE.CPGSR_NO_CREDENTIAL_NOT_FINISHED;
            pcpcs = new _CREDENTIAL_PROVIDER_CREDENTIAL_SERIALIZATION();
            ppszOptionalStatusText = string.Empty;
            pcpsiOptionalStatusIcon = _CREDENTIAL_PROVIDER_STATUS_ICON.CPSI_NONE;
            return HResultValues.E_NOTIMPL;
        }

        public int ReportResult(int ntsStatus, int ntsSubstatus, out string ppszOptionalStatusText,
            out _CREDENTIAL_PROVIDER_STATUS_ICON pcpsiOptionalStatusIcon)
        {
            Log.LogMethodCall();
            ppszOptionalStatusText = string.Empty;
            pcpsiOptionalStatusIcon = _CREDENTIAL_PROVIDER_STATUS_ICON.CPSI_NONE;
            return HResultValues.E_NOTIMPL;
        }

        private int RetrieveNegotiateAuthPackage(out uint authPackage)
        {
            // TODO: better checking on the return codes

            var status = PInvoke.LsaConnectUntrusted(out var lsaHandle);

            using (var name = new PInvoke.LsaStringWrapper("Negotiate"))
            {
                status = PInvoke.LsaLookupAuthenticationPackage(lsaHandle, ref name._string, out authPackage);
            }

            PInvoke.LsaDeregisterLogonProcess(lsaHandle);

            return (int)status;
        }

        private Func<_CREDENTIAL_PROVIDER_FIELD_DESCRIPTOR, bool> FieldSearchFunctionGenerator(uint dwFieldID, _CREDENTIAL_PROVIDER_FIELD_TYPE[] allowedFieldTypes)
        {
            return x =>
                x.dwFieldID == dwFieldID
                && allowedFieldTypes.Contains(x.cpft);
        }

        public int GetUserSid(out string sid)
        {
            sid = userSid;
            return HResultValues.S_OK;
        }

        public int GetFieldOptions(uint fieldID, out CREDENTIAL_PROVIDER_CREDENTIAL_FIELD_OPTIONS options)
        {
            options = CREDENTIAL_PROVIDER_CREDENTIAL_FIELD_OPTIONS.CPCFO_NONE;

            if (fieldID == 5)
            {
                options = CREDENTIAL_PROVIDER_CREDENTIAL_FIELD_OPTIONS.CPCFO_ENABLE_PASSWORD_REVEAL;
            }
            return HResultValues.S_OK;
        }
    }
}
