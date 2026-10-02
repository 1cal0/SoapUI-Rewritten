# SoapUI

WinForms client for driving Roblox RCC / RBXGS SOAP services: open jobs, execute Lua, inspect environments, and review stdout - without hand-crafting XML envelopes.

## Features

- **RCC support** - `HelloWorld`, `GetVersion`, `GetStatus`, `OpenJob(Ex)`, `RenewLease`, `Execute(Ex)`, `CloseJob`, `Diag(Ex)`, `GetAllJobs(Ex)`, `CloseExpiredJobs`, `CloseAllJobs`, `BatchJob(Ex)`.
- **RBXGS support** - `HelloWorld`, `GetVersion`, `GetStatus`, `Execute`, `GetStandardOutMessages`, `GetAllEnvironments`, `OpenEnvironment`, `CloseEnvironment(s)`, `Update`.
- **Script arguments** - add typed `LUA_T*` arguments per call; import/export them as JSON (XML files accepted on import).
- **Script loading** - load `.lua` / `.txt` files directly into the editor panes.
- **Log window** - structured per-call output (return counts, typed values, timestamps).

## Protocol reference

Implementation is checked against the real `RCCService.wsdl`
(`targetNamespace="http://roblox.com/"`, document/literal, `127.0.0.1:64989`)
and a proven PHP envelope for 2008-era RCC:

- WSDL: [RCCService.wsdl](https://finobe.lol/Resources/RCCService.wsdl)
- Envelope reference: [RCCSoap08](https://github.com/1cal0/RCCSoap08)
- Background: [Mercury Documentation](https://docs.mercs.dev/services/rcc/),
  [BOOMBLOX Documentation](https://uboomblox.miraheze.org/wiki/RBXGS)

Known gaps (help welcome): `GetExpiration` has no client/UI yet, RCC
`LuaValue[]` results currently render as "Success!" instead of typed log
lines, and response values are not entity-decoded on the way back.

## Prerequisites

| Requirement | Version |
|---|---|
| Windows | 10 / 11 |
| Visual Studio | 2022 (or 2019) with .NET desktop workload |
| .NET Framework targeting pack | 4.7.2 |
| NuGet | Package Restore enabled (Newtonsoft.Json 13.0.3) |

## Getting started

```powershell
# Clone
git clone https://github.com/p0s0/SoapUI.git
cd SoapUI

# Restore + build (VS Developer PowerShell)
msbuild SoapUI.sln /p:Configuration=Debug
# or open SoapUI.sln in Visual Studio and press F5
```

The binary lands in `bin\Debug\SoapUI.exe`.

## Usage

1. Launch the app.
2. Fill in **IP** (and **Port** / **Base URL** for RCC).
3. Pick a **SOAP Action**.
4. Fill in the action-specific panel (job id, script, diag type, …).
5. Optionally add script arguments with **+** (import with **F**, export with **S**).
6. Press **Execute** - results appear in the log window and/or a dialog.
7. Tick **RBXGS Mode** to switch the action list to the legacy `urn:Roblox` endpoint
   (`http://<ip>/RBXGS/WebService.dll`).

Default connection: `127.0.0.1:64989`, base URL `roblox.com`.

## Project structure

```
SoapUI.sln / SoapUI.csproj
Program.cs                  # WinForms entry point (no logic)
App.config
UI/
  MainUI.cs                 # Thin orchestrator: inputs -> clients -> outcomes
  MainUI.Designer.cs/.resx  # WinForms designer (do not hand-edit)
Models/
  Job.cs / Script.cs        # RCC job + script descriptors
  LuaArgument.cs            # Single { type, value } script argument
  LuaType.cs                # LUA_T* constants + CLR mapping
  ServiceEndpoint.cs        # ip/port/baseUrl/mode value object
  ServiceMode.cs            # Rcc | Rbxgs
  Responses/                # ServiceStatus, ResponseOutcome
  Hosting/                  # Local process tracking (service / game server)
Services/
  Soap/
    SoapTransport.cs        # HTTP POST + error mapping (no UI)
    SoapEnvelopeBuilder.cs  # Envelope + job/script XML fragments
    RccServiceClient.cs     # Typed RCC action set
    RbxgsServiceClient.cs   # Typed RBXGS action set
  Parsing/
    SoapResponseParser.cs   # XML -> ResponseOutcome (no UI)
  IO/
    ArgumentFileStore.cs    # JSON import/export of arguments (XML import supported)
Common/
  AppConstants.cs           # Defaults, action lists, repo URL
  Guard.cs                  # Precondition helpers
  XmlHelper.cs              # Escaping + namespace-agnostic lookup
  UnixTime.cs               # Epoch <-> DateTime
Properties/                 # AssemblyInfo, Resources, Settings
docs/
  ARCHITECTURE.md           # Layering + request flow
```

See [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) for the layering rationale.

## SOAP coverage

### RCC (`http://<ip>:<port>`)

| Action | Notes |
|---|---|
| HelloWorld / GetVersion / GetStatus | Status is parsed into version + environment count |
| OpenJob / OpenJobEx | Job + script payload |
| RenewLease | `jobID` + `expirationInSeconds` |
| Execute / ExecuteEx | `jobID` + script payload |
| CloseJob | `jobID` |
| Diag / DiagEx | `type` + `jobID` |
| GetAllJobs / GetAllJobsEx | No parameters |
| CloseExpiredJobs / CloseAllJobs | Returns jobs-closed count |
| BatchJob / BatchJobEx | Client support; no dedicated UI panel |

### RBXGS (`http://<ip>/RBXGS/WebService.dll`)

| Action | Notes |
|---|---|
| HelloWorld / GetVersion / GetStatus | Legacy `roblox:` envelope |
| Execute | Environment + script + typed arguments |
| GetStandardOutMessages | Last 30 messages with level prefixes + local timestamps |
| GetAllEnvironments / OpenEnvironment / CloseEnvironment(s) | Environment lifecycle |
| Update | Update-URL push |

## Development

- Keep `UI/` free of XML string building - that belongs in `Services/Soap/`.
- Keep `Services/` free of `MessageBox` - return data, let the form present it.
- Models are immutable where practical; validation lives in constructors via `Guard`.
- C# language version is pinned to **7.3** (see `SoapUI.csproj`) to stay compatible with the legacy project system.
- Formatting follows [.editorconfig](.editorconfig) (4 spaces, CRLF, braces always).

## Contributing

Issues and PRs are welcome. Please:

1. Describe the RCC/RBXGS build you tested against.
2. Include a sample request/response (redacted) when touching SOAP code.
3. Keep UI, Services, and Models changes in separate commits where possible.

## License

GNU Lesser General Public License v2.1 is used for this repository.
