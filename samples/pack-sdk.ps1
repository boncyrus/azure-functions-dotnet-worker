$packageSuffix = "dev" + [datetime]::UtcNow.Ticks.ToString()
$outputDirectory = "C:\buildoutput"
$project = "C:\dotnetworker6\azure-functions-dotnet-worker\sdk\Sdk\Sdk.csproj"
dotnet --version
dotnet build
$cmd = "pack", "$project", "-o", $outputDirectory, "--no-build", "--version-suffix", "-$packageSuffix"
& dotnet $cmd