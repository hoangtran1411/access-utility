# 05 - Building, Testing & CI/CD Guide

This guide covers building the solution with modern `.slnx` files, running the xUnit test suite, and configuring cross-platform GitHub Actions CI/CD workflows.

---

## 🛠 Building with Modern `.slnx` Solution Files

In .NET 10, solution files use the modern XML format (`.slnx`):

```bash
# Restore solution dependencies
dotnet restore AccessUtility.slnx

# Build all projects in Release mode
dotnet build AccessUtility.slnx -c Release
```

---

## 🧪 Running xUnit Unit Tests

The test project `AccessUtility.Tests` validates lock file inspection, compacting, repair, and exporters:

```bash
dotnet test AccessUtility.slnx
```

Example test output:
```text
Passed!  - Failed: 0, Passed: 8, Skipped: 0, Total: 8, Duration: 447 ms - AccessUtility.Tests.dll (net10.0)
```

---

## 🚀 Native AOT Publishing

To compile a self-contained native executable (`AccessUtility.exe`) for Windows:

```bash
dotnet publish AccessUtility.csproj -c Release -r win-x64 -p:PublishAot=true -o ./publish
```

---

## ⚙️ GitHub Actions CI/CD Pipeline (`.github/workflows/ci-cd.yml`)

The repository includes an automated GitHub Actions workflow:

1. **Build & Test**: Triggered on `push` or `pull_request` to `main`/`master`. Builds `.slnx` and executes all unit tests on .NET 10.
2. **Native AOT Cross-Platform Matrix**: Compiles Native AOT binaries for Windows (`win-x64`), Linux (`linux-x64`), and macOS (`osx-arm64`).
3. **Automated GitHub Release**: Triggered when pushing a git tag like `v1.0.0`; automatically zips and attaches cross-platform executables to the release.

---

## ⏩ Navigation
- ⬅️ **Previous:** [04 - CLI & Web UI Guide](04-cli-and-web-ui-guide.md)
- ➡️ **Next:** [06 - Complete CLI Usage Commands](06-cli-usage.md)
