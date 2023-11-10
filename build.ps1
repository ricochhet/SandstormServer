param(
    [string]$build,
    [string]$config
)

$projectPath = "project"

Set-Location -Path $projectPath

if ($build -eq "proxy" -or $build -eq "api") {
    $projectName = "Sandstorm.${build}.Client"

    if ($config -eq "Release" -or $config -eq "Debug") {
        dotnet build "$projectName" --configuration $config
    } elseif ($config -eq "Release-Publish" -or $config -eq "Debug-Publish") {
        dotnet publish "$projectName" --configuration ($config -replace "-Publish", "")
    } else {
        Write-Host "Invalid configuration: $config"
    }
} else {
    Write-Host "Specify either 'proxy' or 'api'"
}

Set-Location -Path "..\"
