# slnx Build Compatibility Record

## Status
- `dotnet build IIoT.CloudPlatform.slnx` may fail with:
  - `Build FAILED.`
  - `0 Warning(s)`
  - `0 Error(s)`
- This behavior should be tracked as a build-entry compatibility issue, not a direct business-code error.

## Reproduction (Observed)
- Working directory: `IIoT.CloudPlatform`
- SDK: locked by `global.json` (`10.0.100`)
- Command:

```powershell
dotnet build .\IIoT.CloudPlatform.slnx --no-restore -p:BuildInParallel=false -nologo -v:diag
```

## Failure Node (Typical)
- In solution entry context, failure can occur during project reference target framework discovery.
- One observed path:
  - `IIoT.Core.Employees.csproj`
  - target: `_GetProjectReferenceTargetFrameworkProperties`
  - inner `MSBuild` call for `..\..\shared\IIoT.SharedKernel\IIoT.SharedKernel.csproj` (`GetTargetFrameworks`)
- Result: command exits non-zero without explicit C# compiler errors.

## Triage Rules
- Do not classify this as an application-domain compile regression until the same failure reproduces on direct project build.
- Use project-level baseline script first:
  - `.\scripts\verify-cloud-baseline.ps1`
- If project-level baseline passes and `.slnx` fails, keep issue in this compatibility bucket.

## Evidence Retention
- Keep command logs under:
  - `artifacts/build-baseline/`
- Include:
  - failing command
  - timestamp
  - first failing project/target
  - SDK version (`dotnet --version`)
