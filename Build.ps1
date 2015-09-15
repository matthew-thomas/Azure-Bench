# Install chocolately if it's not already installed
if (-not ($env:Path -ilike "*chocolatey*")) {
    Write-Host "Chocolatey not found in PATH environment variable, installing..."
    
    iex ((new-object net.webclient).DownloadString('https://chocolatey.org/install.ps1'))
}

# Make sure all other required dependencies are installed...
cinst nuget.commandline --version 2.8.6 --confirm

# Make sure all NuGet packages are installed...
nuget restore .\Source
