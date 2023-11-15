# SandstormServer
Mod.io Proxy Server to allow true offline modding. Originally for Insurgency: Sandstorm

A mod.io proxy server to intercept the auth request of mod.io and respond with our own JSON model.

Upon first run the proxy will require you install a certificate (`rootCert.pfx`). This is required to handle HTTPS traffic (YOU MUST INSTALL IT FOR EVERYTHING TO WORK CORRECTLY!). Additionally, the proxy may fail to find `rootCert.pfx` on first run, this does not affect the proxy, but the game itself may be unable to interact with it. You will have to restart the proxy for it to find the `rootCert.pfx`.

## Privacy
SandstormServer is an open source project. Your commit credentials as author of a commit will be visible by anyone. Please make sure you understand this before submitting a PR.
Feel free to use a "fake" username and email on your commits by using the following commands:
```bash
git config --local user.name "USERNAME"
git config --local user.email "USERNAME@SOMETHING.com"
```

## Requirements
- .NET 7 SDK
- Visual Studio Code

## Build
The primary way to build is using [Cake](https://cakebuild.net/).

1. Install Cake: `dotnet tool install Cake.Tool --version 3.2.0`
2. Run `dotnet-cake build.cake` (from `/project/` root).
3. Output will be located in `project/Build/*`.

Files placed in `{root}/bin/SandstormServer` will be copied over to `project/Build`. It is recommended to place the generated `rootCert.pfx` in this folder to prevent needing to regenerate and adding a new certification every build.

### Developers
If you intend to publish a build, use the above guide.

1. Install tools via `dotnet tool restore`.
    - If you make any changes to the code use `dotnet csharpier .` in the project root to format.
2. Run `install.ps1 -build <project> -config <config>`
    - `<project>` options:
        - `proxy`
    - `<config>` options:
        - `Release` | `Release-Publish`
        - `Debug` | `Release-Debug`
3. Output will be located in `project/Sandstorm.Proxy/bin/{Release/Debug/publish}/net7.0/win10-x64`

## Running
The nature of this project attempting to be as dynamic as possible means it comes with some prior setup to make the most out of.

1. Run the program once to generate the programs configuration file: `SandstormServer.json`.
2. Exit out of the program and modify the `ModioGameId` field to your desired game id.
3. Generate an API key for Mod.io at [https://mod.io/me/access](https://mod.io/me/access).
4. Replace the `PLACE_API_KEY_HERE` text in the configuration file with your API key.
5. Modify the `AddToSubscription` field with your desired mod ids.
    - Every id must be written as a string. `"AddToSubscription": ["00000", "10000", "20000"],`
    - If you have previously added a mod, but no longer want to include it, put the id in `DoNotAddToSubscription`.
6. Run the program, and it should automatically fetch the mods you want to add, and automatically find the `Subscription.json`
7. The proxy server should now be up and running, and you can now launch your game.

If the proxy server crashes, you do not safely exit, your computer crashes, your internet breaks, etc., you may have to clear your proxy settings. Typically you can simply switch your proxy to be off in Windows. Settings -> Network & internet -> Proxy -> Manual proxy setup -> Setup -> Use a proxy server -> Toggle off. The method to do this may depend on your operating system.

## Advanced Users (Mod.io API)
The API requires a valid API key to use. Go to [https://mod.io/me/access](https://mod.io/me/access) to generate one.

*Sandstorm.Proxy does not assist in downloading and installing mods. To download and install mods, you must do so directly through [mod.io](https://mod.io/g).

- Usage: `Sandstorm.Launcher.exe <args>`
    - `--gameid <int>`
        - Specify the game id. This will override the game id specified in your configuration file.
    - `--subscribe <int>`
        - Manually subscribe to a mod. This function uses the [Get Mod](https://docs.mod.io/#get-mod) request to fetch a JSON [mod object](https://docs.mod.io/#mod-object). Upon a successful request, the mod object will be written to a local JSON file (`./SandstormServer_Data/{gameId}/Mods/*.json`).
    - `--build`
        - The `build` function will grab all mod object JSONs and combine them into an array of mod objects (`./SandstormServer_Data/{gameId}/Subscription.json`).
    - `--launch <path>`
        - Launch a program after the proxy has started. Typically a game.

## Titanium.Web.Proxy
The included version of Titanium.Web.Proxy has a few differences from the `develop` branch and NuGet.
- Branch: `develop`
- Added: `Models/LocalHostAddr`
- Modified `ProxyServer.cs`

I have added a parameter to `SetAsSystemProxy()` to include the option to specifically set the local domain to use when the proxy is set. When using 'localhost' the game ignores our proxy unless the proxy is set to use '127.0.0.1'. The change lets us specify if we want to use 'localhost' or '127.0.0.1'. Default: localhost

### Suggestions & PRs
Suggestions and pull requests are very appreciated, just keep in mind to follow the project architecture to keep it consistent.

### License
See LICENSE file.