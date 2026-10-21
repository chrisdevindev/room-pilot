# room-pilot

[Português](README.md) | **English**

Control Alexa-connected devices with desktop buttons, global keyboard shortcuts, and a Windows notification-area menu. Start with Light, Outlet, and LED strip, then add, rename, or remove as many controls as you need. Lists scroll to accommodate more devices.

The app is free and uses its own unofficial Amazon connector. Commands require internet, and Amazon service changes may require an update. The Alexa app can stay closed. No Tuya project, local device keys, subscription, or personal server is needed.

## Get started

Requirements: 64-bit Windows, .NET Framework 4.8, and the [Microsoft Edge WebView2 Runtime](https://developer.microsoft.com/microsoft-edge/webview2/). The portable package includes the WebView2 libraries; the Runtime must be installed separately.

1. Extract the complete portable package and open **room-pilot.exe**. Keep the DLL files beside it. When running from source, `Start.cmd` opens the build in `outputs`.
2. Open **Settings → Connect Alexa → Sign in to Amazon**. Select your Alexa account's region. The initial region is `amazon.com.br`; the interface language does not change the account region.
3. Sign in using the account linked to Alexa and complete Amazon's verification. The sign-in page uses `www.amazon.com`, including for Brazilian accounts.
4. In **Devices**, check the suggested assignments or choose devices manually. Use **Add device**, edit the control names, and choose the corresponding Alexa entities. Only compatible power-control entities are listed.
5. Click **Save**, check the dashboard state, and use **Turn on** or **Turn off**.

The dashboard refreshes every two minutes. Toggle reads the current state before sending a command. A command is sent once, then its resulting state is checked. Listing, selecting, and renaming devices do not operate them. Removing a control removes its shortcuts when saved. Renaming preserves its identity and shortcuts.

## Language

Open **Settings → Preferences → Language**, choose **English** or **Português (Brasil)**, and click **Save**. The dashboard and menu update without signing in again. Custom device names and command identifiers are never translated.

A new installation uses Portuguese on a Portuguese Windows installation and English otherwise. Existing configurations keep Portuguese after the update. `--language en-US` and `--language pt-BR` temporarily override the interface language without saving the preference. See [translation guidance](docs/I18N.md).

## Keyboard and notification-area menu

Customize shortcuts in **Settings → Shortcuts**. Assign Turn on, Turn off, Toggle, or Open dashboard. Initial shortcuts are:

| Shortcut | Action |
| --- | --- |
| Ctrl + Alt + 0 | Open dashboard |
| Ctrl + Alt + 1 | Toggle light |
| Ctrl + Alt + 2 | Toggle outlet |
| Ctrl + Alt + 3 | Toggle LED strip |

Use a modifier such as Ctrl or Alt with a key, or F1 through F24. Duplicate shortcuts are rejected; conflicts with other applications are reported. Shortcuts pause while Settings is open.

Closing the dashboard keeps the app in the notification area. Click its house icon to open the menu or double-click to open the dashboard. Choose **Exit** to close the app completely. Starting with Windows is optional in **Preferences**.

## Account data and privacy

Data is stored outside the repository in `%LOCALAPPDATA%\CasaInteligente`. This existing folder name is intentionally retained so the rename preserves saved accounts and browser data. `settings.dat` and `amazon-session.dat` use Windows DPAPI for the current user. The isolated WebView2 sign-in profile is stored in the same private folder. Do not share these files, even encrypted.

Passwords and verification are entered on Amazon's HTTPS page. The connector receives an authorization code and uses PKCE; it does not read password fields or your normal browser profile. New registrations are named **room-pilot**. Existing authorized sessions continue to work.

**Sign out on this PC**, followed by **Save**, removes the connector's session file. It does not revoke Amazon registration or remove the entire browser profile. For complete local removal, exit the app and delete its private data folder. Manage registrations in your Amazon account.

`amazon-status.json` contains only version, step, HTTP status, a recognized error code, and time. It does not record response bodies, passwords, authorization codes, cookies, or tokens. DPAPI does not protect against malware running as the same Windows user or a compromised computer. Additional security details are in [SECURITY.md](SECURITY.md).

## Build and package

Run in PowerShell from the project folder:

```powershell
.\preparar-webview.ps1
.\build.ps1
.\verificar-publicacao.ps1
.\package.ps1
```

The first script downloads the official Microsoft.Web.WebView2 1.0.4258.31 SDK from NuGet and checks its pinned SHA256. Build uses the Windows .NET Framework compiler and runs simulated tests without an Amazon account or real power commands. Translation catalogs are embedded in both the app and test executable.

`outputs/room-pilot-windows-x64.zip` is the portable app. `outputs/room-pilot-source.zip` contains the public source. Packages use explicit file lists and exclude account data, browser profiles, caches, and diagnostics. Each person signs in with their own account.

External commands:

```text
room-pilot.exe --list-devices
room-pilot.exe --device tomada --command on
room-pilot.exe --device fita --command toggle
room-pilot.exe --device "Office light" --command status --language en-US
```

The starter identifiers remain `lamparina`, `tomada`, and `fita` for compatibility. New controls receive stable identifiers. List controls to find their local names and identifiers; Amazon identifiers are not exposed. Names can be used when unique. `--result path.json` saves command output without session credentials. `--connect` opens Settings. Developers can isolate test data with `--data-dir private-folder`.

## Structure and license

The project uses lightweight layers: Domain, Application, Infrastructure, UI, CLI, and Localization. The project file is `room-pilot.csproj`; code uses the `RoomPilot` namespace. Architecture guidance is in [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md).

Project code is licensed under [MIT](LICENSE). Microsoft libraries retain their own licenses, included in the portable package. See [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md). This is not an official Amazon or Microsoft client and does not use Alexa Remote or Alexa Cookie packages.
