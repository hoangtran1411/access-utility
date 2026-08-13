# Feature 07 - Auto Updater

## 📌 Overview
To ensure users always have the latest bug fixes and features, AccessUtility will include an automatic update mechanism. The CLI will query the GitHub Releases API to check for a newer version. If one is found, it can automatically download, extract, and replace the current executable.

---

## 📐 Technical Specification

### 1. Version Checking
- Make a standard `HttpClient` GET request to `https://api.github.com/repos/hoangtran1411/access-utility/releases/latest`.
- Parse the JSON response to extract the `tag_name` (e.g., `v1.0.1`).
- Compare `tag_name` with the current running version of the executable (`Assembly.GetExecutingAssembly().GetName().Version`).

### 2. Downloading & Replacing
- If an update is available, locate the correct asset in the release JSON based on the current OS architecture (e.g., `AccessUtility-win-x64.zip`).
- Download the compressed asset to a temporary directory.
- Extract the asset.
- Replace the current running executable. (On Windows, this requires a small trick: rename the running `.exe` to `.old`, move the new `.exe` into place, and instruct the user to restart, or launch a quick batch script).

---

## 🎯 User Interface Integration

### CLI Command
```bash
AccessUtility.exe update
```
- Or optionally, add a `--check-updates` flag that runs gracefully on startup.
