param(
    [string]$build
)

Set-Location -Path "project"

if ($build -eq "proxy") {
    dotnet build "./Sandstorm.Proxy.Client"
}
elseif ($build -eq "api") {
    dotnet build "./Sandstorm.Api.Client"
}
else {
    Write-Host "Specify either 'proxy' or 'api'"
}

Set-Location -Path "../"