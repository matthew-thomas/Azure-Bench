Write-Host $env:PATH

# Install chocolately if it's not already installed
if (-not ($env:Path -ilike "*chocolatey*")) {
    Write-Host "Chocolatey not found in PATH environment variable, installing Chocolatey..."
    #iex ((new-object net.webclient).DownloadString('https://chocolatey.org/install.ps1'))
} else {
    Write-Host "CONTAINS!"
}
