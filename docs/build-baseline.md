# Cloud Build Baseline

## Purpose
- Stabilize repeatable local verification for `IIoT.CloudPlatform` without changing architecture or public APIs.
- Use project-level entry points as the primary baseline instead of `dotnet build IIoT.CloudPlatform.slnx`.

## Standard Sequence
Run all commands from repository root: `IIoT.CloudPlatform`.

```powershell
dotnet build .\src\hosts\IIoT.HttpApi\IIoT.HttpApi.csproj --no-restore -p:BuildInParallel=false -nologo -v:minimal
dotnet build .\src\hosts\IIoT.DataWorker\IIoT.DataWorker.csproj --no-restore -p:BuildInParallel=false -nologo -v:minimal
dotnet build .\src\hosts\IIoT.MigrationWorkApp\IIoT.MigrationWorkApp.csproj --no-restore -p:BuildInParallel=false -nologo -v:minimal
dotnet build .\src\hosts\IIoT.AppHost\IIoT.AppHost.csproj --no-restore -p:BuildInParallel=false -nologo -v:minimal
dotnet test .\src\tests\IIoT.ServiceLayer.Tests\IIoT.ServiceLayer.Tests.csproj --no-build -p:BuildInParallel=false -nologo -v:minimal
```

## One-Command Entry
Use the script below to run the same sequence and capture logs.

```powershell
.\scripts\verify-cloud-baseline.ps1
```

Optional:

```powershell
.\scripts\verify-cloud-baseline.ps1 -SkipSolutionObservation
```

## Notes
- This baseline intentionally excludes `IIoT.EndToEndTests` from automatic pass/fail gating.
- E2E validation is performed manually in an environment where Docker/Aspire is available.
- Solution-level (`.slnx`) behavior is still observed and logged as a compatibility signal, not as a business-code quality signal.
