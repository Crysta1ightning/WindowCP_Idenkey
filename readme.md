
# Windows Credential Provider
_Made only with C#, .NET_

This project is built above the foundation of
https://github.com/phaetto/windows-credentials-provider
Huge credit to Phaetto!

## *Read this before you start*
I will recommend you try this CP (credential provider) using a virtual machine.

I personally use Oracle VirtualBox and Win10, but anything above Win8 should be fine.
_Consider yourself warned._

## Installation
To install the credential provider, you should 
1. Open the `WindowsCredentialProvider.sln` and build it using Visual Studio
2. Register the CP, by using the following commands
```
C:\\Windows\Microsoft.NET\Framework64\v4.0.30319\RegAsm.exe "C:\Windows\System32\WindowsCredentialProviderTest.dll" /tlb /codebase
```
```
regedit register-credentials-provider.reg
```
If you understand Chinese, check out https://iam9527.pixnet.net/blog/post/351771139-%5Bc%23%5Dcredential-provider if you still don't know how to install.

This CP requires internet to function, if you don't have internet, it won't let you login.
The projects are setup for x64 systems - you might need to change that if you want it to run on 32bit platforms. Same goes for registry installation.

## What it can do
It allows you to login using your phone, so you only need to type the password on the first login.

For the first time login:
1. Shows QRCode, use your phone app to scan and register
2. It will then send a notification to your phone, after you confirm it, you can enter your password and login.
   
After the first time login:
1. Send a notification to your phone, confirm it, and login (without typing the password).

