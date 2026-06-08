# Publishing — cheat-sheet

## 1. Bump version (only when re-publishing — a published version can't be overwritten)
Set the new version in:
- `package.json` (root), `Core/js/package.json`, `React/react/package.json`, `React/react-mui/package.json`, `examples/full/frontend/package.json` — incl. the `@omni2fa/*` dependency pins
- `.Net/Directory.Build.props` (`<Version>`)
- `Core/protocol/omni2fa.openapi.yaml` `info.version` — **only on a contract change** (= `x.y.0`)

## 2. npm  (granular/automation token — no interactive login, no OTP)
One-time: at npmjs.com → **Access Tokens**, create a **Granular Access Token** with **Read and write** on the `@omni2fa` packages (or the whole scope). Automation/granular tokens bypass 2FA, so no `--otp`.

Set the token once in your user npm config (lands in `~/.npmrc`, nothing to commit):
```powershell
npm config set //registry.npmjs.org/:_authToken npm_xxxxxxxxxxxxxxxxxxxx
```
Then publish (both packages already set `publishConfig.access: public`):
```powershell
npm whoami            # confirms the token authenticates
npm run build
npm publish --workspace @omni2fa/core
npm publish --workspace @omni2fa/react
```
- order matters: core before react. `react-mui` is `private` → skipped.

## 3. NuGet  (personal API key; no org/2FA)
```powershell
Remove-Item artifacts -Recurse -Force -ErrorAction SilentlyContinue
dotnet pack .Net/Omni2FA.slnx -c Release -o artifacts
Get-ChildItem artifacts\*.nupkg | ForEach-Object { dotnet nuget push $_.FullName --api-key <KEY> --source https://api.nuget.org/v3/index.json --skip-duplicate }
```
- order doesn't matter. `.snupkg` symbols push automatically.
- after push, nuget.org **validates + indexes** for a few minutes (up to ~1h for brand-new IDs) before the package is installable — the "not indexed yet" banner is normal; you only get an email if validation fails.

## When to bump
- Any change to already-published code → bump (npm and/or NuGet refuse a duplicate version).
- Patch `x.y.Z` for fixes; minor `x.y.0` for new features/contract changes.
