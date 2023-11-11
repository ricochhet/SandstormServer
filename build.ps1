param(
    [string]$build,
    [string]$config
)

$projectPath = "project"

Set-Location -Path $projectPath

if ($build -eq "proxy") {
    $projectName = "Sandstorm.${build}"

    if ($config -eq "Release" -or $config -eq "Debug") {
        dotnet build "$projectName" --configuration $config
    } elseif ($config -eq "Release-Publish" -or $config -eq "Debug-Publish") {
        dotnet publish "$projectName" --configuration ($config -replace "-Publish", "")
    } else {
        Write-Host "Invalid configuration: $config"
    }
} else {
    Write-Host "Invalid project: $build"
}

Set-Location -Path "..\"
