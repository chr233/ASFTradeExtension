$PROJECT_NAME = "ASFTradeExtension"
$PLUGIN_NAME = "ASFTradeExtension.dll"

dotnet publish $PROJECT_NAME -o ./publish/ -c Release

Copy-Item -Path .\publish\$PLUGIN_NAME -Destination .\dist\ 

$dirs = Get-ChildItem -Path ./publish -Directory
foreach ($dir in $dirs) {
    $subFiles = Get-ChildItem -Path $dir.FullName -File -Filter *.resources.dll
    
    foreach ($file in $subFiles) {
        $resourceName = [System.IO.Path]::GetFileNameWithoutExtension($file.Name)
        $opDir = "./tmp/$resourceName"
        if (-Not (Test-Path -Path $opDir)) {
            New-Item -ItemType Directory -Path $opDir
        }

        $destinationPath = ".\dist\$resourceName\$($dir.Name).dll"
        Copy-Item -Path $file -Destination $destinationPath

        Write-Output "Copy resource DLL $($file.FullName) -> $destinationPath"
    }
}

Remove-Item -Recurse -Force "./tmp"