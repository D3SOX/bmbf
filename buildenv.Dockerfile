FROM ubuntu:latest

# Install basic packages
RUN apt-get update && apt-get install -y \
    apt-transport-https \
    ca-certificates \
    curl \
    gnupg \
    lsb-release

# Install .NET
RUN curl -fsSL "https://packages.microsoft.com/config/ubuntu/$(lsb_release -rs)/packages-microsoft-prod.deb" -o packages-microsoft-prod.deb \
    && dpkg -i packages-microsoft-prod.deb \
    && rm packages-microsoft-prod.deb \
    && apt-get update \
    && apt-get install -y dotnet-sdk-6.0

# Install Java
RUN apt-get install -y openjdk-11-jdk-headless

# Install Node
RUN curl -fsSL https://deb.nodesource.com/setup_16.x | bash \
    && apt-get install -y nodejs

# Install Mono
RUN apt-key adv --keyserver hkp://keyserver.ubuntu.com:80 --recv-keys 3FA7E0328081BFF6A14DA29AA6A19B38D3D831EF \
    && echo "deb https://download.mono-project.com/repo/ubuntu stable-$(lsb_release -cs) main" | tee /etc/apt/sources.list.d/mono-official-stable.list \
    && apt-get update \
    && apt-get install -y mono-complete

# Install Nuget
RUN curl -fsSL https://dist.nuget.org/win-x86-commandline/v6.0.0/nuget.exe -o /usr/local/bin/nuget.exe \
    && echo '#!/bin/bash\nmono /usr/local/bin/nuget.exe "$@"' > /usr/local/bin/nuget \
    && chmod +x /usr/local/bin/nuget

# Install Android workload
# Blocked on https://github.com/xamarin/xamarin-android/pull/6613
# RUN dotnet workload install android
