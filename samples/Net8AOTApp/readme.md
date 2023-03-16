
1. Create published output by running `dotnet publish -r win-x64 -c release` in project root.
2. Update worker config to rename `defaultWorkerPath` prop value to end with `.exe` (instead of `.dll)