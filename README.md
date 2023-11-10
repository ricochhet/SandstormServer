# SandstormServer
Mod.io Proxy Server to allow true offline modding. Originally for Insurgency: Sandstorm

A mod.io proxy server to intercept the auth request of mod.io and respond with our own JSON model.

## Guide

### Sandstorm.Proxy.Client
- Build: `./install.ps1 -build proxy` or `dotnet build ./Sandstorm.Proxy.Cient` (From `project` root).
- Usage: `SandstormProxy <path/to/model.json>` (See Sandstorm.Api.Client).
- **READ:** Upon first run the proxy will require you install a certificate (`rootCert.pfx`). This is required to handle HTTPS traffic (YOU MUST INSTALL IT FOR EVERYTHING TO WORK CORRECTLY!). Additionally, the proxy may fail to find `rootCert.pfx` on first run, this does not affect the proxy, but the game itself may be unable to interact with it. You will have to restart the proxy for it to find the `rootCert.pfx`.
#### Quirks
- Whether it's exclusive to the tested game (Insurgency: Sandstorm) or not, the proxy settings of your device must be set to use `127.0.0.1`, using localhost makes the game ignore the proxy settings entirely. If in the future it is found different games behave differently, this may become an explicit CLI setting.
- (If your internet breaks) If the proxy server crashes, you do not safely exit, your computer crashes, etc., you may have to clear your proxy settings. Typically you can simply switch your proxy to be off in Windows.
    - Settings -> Network & internet -> Proxy -> Manual proxy setup -> Setup -> Use a proxy server -> Toggle off. The method to do this may depend on your operating system.
- The proxy does not handle every single `mod.io` request. It will handle anything under the `mod.io` domain by sending a 404 (Not Found) under everything except the `/v1/me/subscribed` path. In which it will send the appropriate data model. Mod.io cdn is not handled whatsoever, so you may still see images or possibly other information despite being "offline." Additionally, CDN images and thumbnails are cached.
- Due to the proxy sending a 404 (Not Found) request to most paths, the in-game mod browser and subscribed mods section will appear blank, but your mods should still be effective.

### Sandstorm.Api.Cient
Sandstorm.Api.Client requires a valid API key to use. This can be obtained by creating an account or signing into [mod.io](https://mod.io/g), going into the bottom right, and looking for the "My account" button. Click "Access" and folowing the steps to create an API access token.

- Build: `./install.ps1 -build api` or `dotnet build ./Sandstorm.Api.Cient` (From `project` root).
- Usage: `SandstormApi <add/build> <args>`
    - Usage: `SandstormApi get <gameId> <modId> <apiKey>`
        - The `get` function uses the [Get Mod](https://docs.mod.io/#get-mod) request to fetch a JSON [mod object](https://docs.mod.io/#mod-object). Upon a successful request, the mod object will be written to a local JSON file (`./SandstormServerData/{gameId}/Mods/*.json`).
    - Usage: `SandstormApi build <gameId>`
        - The `build` function will grab all mod object JSONs and combine them into a singular object that follows the schema of `/v1/me/subscribed`. The finalized `Subscription.json` can be found in `./SandstormServerData/{gameId}/Subscription.json`.

### Titanium.Web.Proxy
The included version of Titanium.Web.Proxy has a few differences from the `develop` branch and NuGet.
- Branch: `develop`
- Added: `Models/LocalHostAddr`
- Modified `ProxyServer.cs`

I have added a parameter to `SetAsSystemProxy()` to include the option to specifically set the local domain to use when the proxy is set. When using 'localhost' the game ignores our proxy unless the proxy is set to use '127.0.0.1'. The change lets us specify if we want to use 'localhost' or '127.0.0.1'. Default: localhost

### Suggestions & PRs
Suggestions and pull requests are very appreciated, just keep in mind to follow the project architecture to keep it consistent.

### TODO
- Possibly handle additional mod.io API requests so we can at the minimum display the users subscribed mods.
- Possibly allow users to automatically open the game process after the proxy server has started.
    - Additionally have the proxy server close when the game closes.
- Create a release build setup / script.
- Create a faster solution for adding mods with Sandstorm.Api, possibly read all of the ids from a file.
    - Additionally map the game name to the game id.
- Add more control for running with admin rights.

### License
See LICENSE file.