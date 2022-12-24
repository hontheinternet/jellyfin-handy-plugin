# Jellyfin TheHandy Plugin

Recognizes the `.funscript` file in the same folder with the **exact same** name as the source video and uses it to play the script. 
Once installed on your Jellyfin server, it will work with any Jellyfin client without any special setup. 

## How to use

0. Build or download the latest release binaries from the releases section

1. Locate the Jellyfin Plugin folder 

    The plugins folder is located in different locations depending on your install:
    - `%UserProfile%\AppData\Local\jellyfin\plugins` for direct installs
    - `%ProgramData%\Jellyfin\Server\plugins` for tray installs

    You can find more info about where to find the plugins folder here: [Plugins | Jellyfin](https://jellyfin.org/docs/general/server/plugins/)

2. The folder `TheHandy_1.0.0.0` extracted from the release binary zip should be placed in the plugin folder, and then restart the Jellyfin server to recognize the plugion and load. 

3. And now go to your Dashboard -> Plugins in the Jellyfin interface and click on the newly installed TheHandy Plugin -> Here you can set your Connection Key and then click Save. 

Once setup, it'll work with any Jellyfin client. 

### Quirks/Known Issues 

* [ ] Takes significant initial time to load the script (even though the video plays) (Probably will reduce the loading time in the next release)
    - Try to pause and play a few times to make time the script is loaded and synced correctly. Once synced it will play well. 
* [ ] Changing playback position doesn't sync the script, you need to pause and play to resync the current playback position. (Probably will fix in the next release)

## How to Build

### Prerequisites 

* [Dotnet SDK 6.0](https://dotnet.microsoft.com/download)


* Following dependencies are needed for building this plugin

```
cd TheHandyPlugin
dotnet add package Jellyfin.Model
dotnet add package Jellyfin.Controller
dotnet add package Newtonsoft.Json
```


### Build 

#### Debug build
```console
dotnet build /property:GenerateFullPaths=true /consoleloggerparameters:NoSummary
```

#### Release build
```console
dotnet build /property:GenerateFullPaths=true /consoleloggerparameters:NoSummary --configuration Release
```

Once built, it creates a .dll file with additional files at `bin\Release\net6.0` or `bin\Debug\net6.0` paths. You can follow the How to install section to install your newly built plugin in Jellyfin.
