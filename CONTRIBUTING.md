# Contributing to AccessUtility

First off, thanks for taking the time to contribute! 🎉

## How to Contribute

1. **Fork the repo**.
2. **Create a branch** for your feature or bug fix: `git checkout -b my-new-feature`
3. **Commit your changes**: `git commit -am 'Add some feature'`
4. **Push to the branch**: `git push origin my-new-feature`
5. **Submit a pull request**.

## Development Rules for .NET 10 Native AOT

This project requires extreme care regarding .NET Native AOT compilation:
- **No Reflection**: Do not use `System.Reflection` to dynamically load assemblies or instantiate types.
- **No Dynamic Code Generation**: Avoid `System.Reflection.Emit`.
- **System.Text.Json Contexts**: All JSON serialization *must* use source generation via `JsonSerializerContext`.
- **Minimal External Dependencies**: Be wary of installing NuGet packages that aren't AOT compatible (Entity Framework, Dapper, traditional Serilog DB Sinks, etc).

## Testing

Ensure all xUnit tests pass before submitting a PR:
```bash
dotnet test AccessUtility.slnx
```
