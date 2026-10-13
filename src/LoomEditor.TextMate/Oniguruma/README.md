# Oniguruma

Interop with the native Oniguruma regex library (BSD-2-Clause), built by `src/native/oniguruma/build.ps1`.

Rules:
- Only `[LibraryImport]` (source-generated, AOT-safe). Never `[DllImport]`.
- Nothing outside this folder knows Oniguruma exists: everything goes through `IRegexEngine`.
- Every native handle is wrapped in a `SafeHandle`.
