# Architecture

## Layering

```
UI  ->  Services  ->  Models / Common
```

- **UI (`UI/MainUI.cs`)** — WinForms only. Reads text boxes, builds `ServiceEndpoint` /
  `Job` / `Script` objects, calls one client method, renders the returned
  `ResponseOutcome` (log lines + optional dialog). No XML templating here.
- **Services/Soap** — `RccServiceClient` and `RbxgsServiceClient` own the typed
  action sets. `SoapEnvelopeBuilder` owns every angle bracket. `SoapTransport`
  owns HTTP (headers, URL selection, timeout) and maps `WebException` to a
  `SoapTransportException` carrying the server's response body. No `MessageBox`
  below this layer.
- **Services/Parsing** — `SoapResponseParser` turns raw XML into a
  `ResponseOutcome`. Namespace-agnostic (`Name.LocalName`) because RCC and
  RBXGS use different namespaces for the same concepts.
- **Services/IO** — `ArgumentFileStore` owns the JSON shape of exported
  arguments (`[{ "type", "value" }]`).
- **Models** — immutable-ish domain objects with constructor validation
  (`Job`, `Script`, `LuaArgument`, `ServiceEndpoint`, `ServiceMode`,
  `Responses/*`, `Hosting/*`).
- **Common** — `Guard`, `XmlHelper` (escaping done once, correctly:
  `&` first via `SecurityElement.Escape`), `UnixTime`, `AppConstants`.

## Request flow

```
[Form inputs] -> ServiceEndpoint + Job + Script
              -> RccServiceClient.Execute() / RbxgsServiceClient.Execute()
              -> SoapEnvelopeBuilder fragment -> SoapTransport.Send()
              -> raw XML -> SoapResponseParser.Parse()
              -> ResponseOutcome -> log window / MessageBox
```

## Key decisions

- **One transport, two clients.** RCC and RBXGS differ in envelope + URL, not
  in HTTP mechanics, so they share `SoapTransport` and diverge in builders.
- **Exceptions, not dialogs, from services.** `SoapTransportException` carries
  `Action` + `ResponseBody`; the form decides how to display it. This keeps
  services unit-testable without a message loop.
- **Single `LuaArgument` model.** The legacy tree had three overlapping types
  (`LuaValue`, `LuaValueNew`, `JSONArgument`). One serialisable type covers
  grid rows, JSON files, and envelope fragments.
- **No `dynamic` in new code.** Legacy parsing used `dynamic` everywhere;
  the parser now uses typed locals and safe `ChildValue` lookups.
- **C# 7.3 ceiling.** The legacy (non-SDK) project format builds cleanly on
  older toolsets when the language version stays pinned.

## What would come next

- Async execution (`async`/`await` + cancellation) so long calls don't freeze
  the form (currently the form disables itself synchronously).
- `HttpClient` + DI instead of `new`-ing clients per click.
- Unit tests around `SoapEnvelopeBuilder` (escaping!) and
  `SoapResponseParser` with recorded XML fixtures.
- SDK-style project + `PackageReference` (drops `packages.config`).
