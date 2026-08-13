# Contributing to AccessUtility

Thank you for your interest in contributing to **AccessUtility**! We welcome contributions to improve Microsoft Access 97 `.mdb` file recovery, database compacting algorithms, `.ldb` lock handling, and UI features.

## 🛠 Prerequisites & Development Setup

1. **.NET 10 SDK**: Install the .NET 10 SDK (`dotnet --version` should report `10.0.x`).
2. **C# IDE / Editor**: Visual Studio 2022+ or VS Code with C# Dev Kit.
3. **Repository Setup**:
   ```bash
   git clone https://github.com/hoangtran1411/access-utility.git
   cd access-utility
   ```

## 🏗 Building & Testing

We use the modern XML solution file (`AccessUtility.slnx`):

```bash
# Restore packages
dotnet restore AccessUtility.slnx

# Build the solution
dotnet build AccessUtility.slnx -c Release

# Run the xUnit test suite
dotnet test AccessUtility.slnx
```

### Testing Native AOT Publishing
To test Native AOT compilation locally:
```bash
dotnet publish AccessUtility.csproj -c Release -r win-x64 -p:PublishAot=true
```

## 📐 Code Guidelines & Native AOT Rules

- **Native AOT Compatibility**: Avoid un-annotated reflection, dynamic code generation (`System.Reflection.Emit`), or untyped JSON serialization.
- **JSON Serialization**: Always use `System.Text.Json` source generator attributes registered in `Web/AppJsonContext.cs`.
- **Fault-Tolerant Parsing**: Jet 3.5 byte reading routines must gracefully handle corrupt byte streams and log issues to `DiagnosticReport` / `RepairResult` rather than throwing unhandled exceptions.

## 📥 Pull Request Process

1. Create a new branch: `git checkout -b feature/your-feature-name`.
2. Ensure all xUnit tests pass (`dotnet test AccessUtility.slnx`).
3. Commit your changes with clear messages (`git commit -m "feat: improve compact page allocation map"`).
4. Push to your fork and submit a Pull Request against the `main` branch.
