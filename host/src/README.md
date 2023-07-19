# FunctionsNetHost

FunctionsNetHost is the Azure functions worker .NET host, which is used for starting a dotnet isolated placeholder.

Below are the sequence of events:

1. Host will start the FunctionsNetHost executable as a child process in placeholder mode.
2. FunctionsNetHost does the GRPC handshake with host and waits for specialization request.
3. When specialization happens, host will send environment reload request to FunctionsNetHost.
4. FunctionsNetHost will load & execute the worker assembly code.
5. All communications from host to worker or vice versa goes through FunctionsNetHost.


## Publish

FunctionsNetHost is written in AOT compatible managed code. We use dotnet AOT compiler to produce the native executable.

Open a terminal here and run `dotnet publish -c release -r win-x64`. This will produce the native exe for win-x64 platform in the `FunctionsNetHost\bin\Release\net7.0\win-x64\publish\` directory.
