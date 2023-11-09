# SandstormServer
Mod.io Proxy Server to allow true offline modding. Originally for Insurgency: Sandstorm

A mod.io proxy server to intercept the auth request of mod.io and respond with our own JSON model.

## Guide

### Titanium.Web.Proxy

The included version of Titanium.Web.Proxy has a few differences from the `develop` branch and NuGet.
- Branch: `develop`
- Added: `Models/LocalHostAddr`
- Modified `ProxyServer.cs`

I have added a parameter to `SetAsSystemProxy()` to include the option to specifically set the local domain to use when the proxy is set. When using 'localhost' the game ignores our proxy unless the proxy is set to use '127.0.0.1'. The change lets us specify if we want to use 'localhost' or '127.0.0.1'. Default: localhost

### Setup

- `dotnet build ./Sandstorm` - builds the entire project.
- On your first run of the server it will ask you to install a certificate, this is safe to do, and allows the server to work correctly.
- With a live copy of a game utilizing mod.io, install the mods of your choosing, after everything has downloaded, exit the game. Install and run Fiddler Classic, make sure to have HTTPS support on. With Fiddler Classic open, run the game, and either look for or filter for `*.mod.io` requests. Specifically look for a `GET` request containing `/v1/me/subscribed`. Click on that request, click decrypt. Copy the corresponding JSON into subscription.json *(You have may have to convert from stringified json, to an object. You may also have to validate/lint as well as making sure all characters are valid)*.
- `Models/subscription.json` should be placed next to Sandstorm.exe
- Assuming the proxy server is running, and Fiddler is running, your game should allow you to load mods offline (without an active internet connection).
- The model is a representation of the HTTPS request from https://api.mod.io/v1/me/subscribed. It was directly ripped from a live copy of Insurgency: Sandstorm. The model JSON has the following mods installed:
    - https://mod.io/g/insurgencysandstorm/m/more-ammo-mutator
    - https://mod.io/g/insurgencysandstorm/m/inspectweapon1
    - https://mod.io/g/insurgencysandstorm/m/ismcmod1
    - https://mod.io/g/insurgencysandstorm/m/aiplus
    - https://mod.io/g/insurgencysandstorm/m/ismcr
    - https://mod.io/g/insurgencysandstorm/m/tpc