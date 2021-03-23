# BAOO-SecureProxy
A proxy server for Batman: Arkham Origins Online that allows you to use the secure HTTPS connection with the game server.

You need to own a copy of Batman: Arkham Origins on PC (Steam) to be able to play the game's multiplayer mode with this program.

---

#### Table of contents
1. [Usage](#usage)
2. [Disclosure](#disclosure)
3. [Questions, answers](#questions-answers)
4. [Credits, used packages](#credits-used-packages)
5. [Contribution](#contribution)

---

### Usage
If you wish to compile the program yourself, feel free to do so.
I used the following command to create the exe file found in the [Releases](https://github.com/reugen-programs/BAOO-SecureProxy/releases).
> `dotnet publish -r win-x64 -c Release -p:PublishSingleFile=true --self-contained true -p:PublishReadyToRun=true -p:PublishReadyToRunShowWarnings=true -p:PublishTrimmed=true -p:IncludeNativeLibrariesForSelfExtract=true`

1. Put the "BAOOProxy.exe" file in a designated folder and run it from there. On the first run it will create a "Config" folder and a "Config.json" file.
1. If you wish to enable the "game progress backup" module then close the program, modify the config file and then restart the program.
1. Start the game only when the program tells you that you can do so.
1. When you are finished with the game you can close the program. However you can keep the program running if you plan to jump back into the game later.

#### Configuration
A default config file has the following structure:
```json
{
    "BAOInstallationFolder": null,
    "BackupEnabled": false,
    "ProfileHistoryAmount": 100,
    "InventoryHistoryAmount": 25
}
```
    Options:

Name | Type | Description
------------ | ------------- | -------------
BAOInstallationFolder | `string` | The folder in which the game is installed. The program tries to discover the path automatically, but if needed you can enter the path maually. <br /> Example value: `"C:\\Program Files (x86)\\Steam\\steamapps\\common\\Batman Arkham Origins"` <br /> (Notice the double backslashes.)
BackupEnabled | `bool` | Determines if the backup module should be enabled or not. If you wish to enable it, set this to `true`.
ProfileHistoryAmount | `int` | The amount of historical profile data that should be stored in the backup file. Be reasonable and don't raise it too much.
InventoryHistoryAmount | `int` | The amount of historical inventory data that should be stored in the backup file. Be reasonable and don't raise it too much.

---

### Disclosure
The program does modify some data in the traffic. Exactly 2 things are modified and nothing else.
* In the outgoing data only the addressing is changed to match the address of the actual server, since the game will address this proxy server and not the real one. **This change is necessary to make the connection work.**
* The second change is in the incoming data. Here only the news sent by the server are changed. Furthermore this change does only occure as long as the server sends the same old news. In case the server sends a new information you will get that unchanged.

---

### Questions, answers
**What does this program do?** <br />
It does many things. It finds your game installation, modifies the necessary game config file, optionally creates backup of your game progress but most importantly it serves as a proxy server. That means this program will stand between your game and the actual server on the internet and manage the communication. This program utilizes the secure HTTPS connection to the server assuring your privacy and security.

**How can this program use the HTTPS connection if the game cannot?** <br />
The game cannot connect to the server because the server has a bad (expired) certificate. The program uses its own server certificate validation method. In this method good and valid certificates are allowed through, in case they renew their server certificate. However I made one exception to allow the game server's bad certificate through. By doing so the program can utilize the data encryption provided by the HTTPS protocol. I made precautions to only accept the game server's certificate, everything else will be rejected if invalid.

**I checked the game's config file and it still has http. Is this bad?** <br />
No it's fine. The communication between the game and this program will still use the unencrypted HTTP protocol. This is not a problem as the data flow is still on yor local machine. Between this program and the server however the communication will use the HTTPS protocol. So your data is securely encrypted while traveling on the internet.

**What's the point of the backup function?** <br />
Nothing much really. It's just there to backup your game progress. This backup can't be used for anything yet, but who knows what the future holds.

---

### Credits, used packages
* The TCP proxy is forked from ngbrown's project (https://github.com/ngbrown/netproxy/tree/branch1)
  * which is based on Stormancer's netproxy (https://github.com/Stormancer/netproxy)
* Microsoft.Win32.Registry package is used to find Steam installation (https://github.com/dotnet/runtime)
* Gameloop.Vdf package is used to gather information about the Steam and game installation folders (https://github.com/shravan2x/Gameloop.Vdf)
* Newtonsoft.Json package is used for handling all the JSON files (https://www.newtonsoft.com/json)

---

### Contribution
Feel free to contribute and fix my coding. I know there is much room for improvement. <br />
Also if you are not into coding feel free to correct this README page. I am not a native english speaker, so there could be mistakes here.
