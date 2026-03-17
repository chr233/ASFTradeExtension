$runtimes = "linux-x64", "win-x64", "win-arm64", "linux-arm64"
$config = "Release"
$projectName = "ArchiSteamFarm/ArchiSteamFarm"
  
dotnet restore $projectName
  
foreach ($runtime in $runtimes) {
    Write-Debug "Publishing for runtime: $runtime"
    $outputDir = "./tmp/ASF-$runtime"

    dotnet publish $projectName --output $outputDir --self-contained --runtime $runtime --configuration $config --no-restore --nologo -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:ContinuousIntegrationBuild=true -p:UseAppHost=true
}