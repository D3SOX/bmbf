Write-Output "Restoring BMBF:"

# packages.config contains the aspnetcore runtime packages, as dotnet restore is refusing to restore these automatically
nuget restore packages.config -PackagesDirectory $HOME/.nuget/packages

# Restore the actual BMBF project
nuget restore BMBF.sln