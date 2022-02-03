FROM ubuntu:20.04
ARG DEBIAN_FRONTEND=noninteractive

# Install basic packages
RUN apt-get update && apt-get install -y \
    apt-transport-https \
    ca-certificates \
    curl \
    gnupg \
    libc6 \
    libgcc1 \
    libgssapi-krb5-2 \
    libicu66 \
    libssl1.1 \
    libstdc++6 \
    zlib1g

# Install .NET
RUN curl -fsSL "https://dot.net/v1/dotnet-install.sh" | bash -s -- --channel 6.0.2xx --quality daily --install-dir /opt/dotnet --no-path \
    && /opt/dotnet/dotnet nuget add source "https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet6/nuget/v3/index.json" -n dotnet6

# Install Java
RUN apt-get install -y openjdk-11-jdk-headless

# Install Node
RUN curl -fsSL https://deb.nodesource.com/setup_16.x | bash \
    && apt-get install -y nodejs \
    && corepack enable

# Install Mono
RUN apt-key adv --keyserver hkp://keyserver.ubuntu.com:80 --recv-keys 3FA7E0328081BFF6A14DA29AA6A19B38D3D831EF \
    && echo "deb https://download.mono-project.com/repo/ubuntu stable-focal main" | tee /etc/apt/sources.list.d/mono-official-stable.list \
    && apt-get update \
    && apt-get install -y mono-complete

# Install Nuget
RUN curl -fsSL https://dist.nuget.org/win-x86-commandline/v6.0.0/nuget.exe -o /usr/local/bin/nuget.exe \
    && echo '#!/bin/bash\nmono /usr/local/bin/nuget.exe "$@"' > /usr/local/bin/nuget \
    && chmod +x /usr/local/bin/nuget

# Install Android workload
RUN /opt/dotnet/dotnet nuget add source "https://pkgs.dev.azure.com/dnceng/public/_packaging/darc-pub-dotnet-emsdk-e8ffccbd/nuget/v3/index.json" -n emsdk \
    && /opt/dotnet/dotnet workload install android

# Setup work user
RUN apt-get install -y sudo \
    && useradd -MN -s /bin/bash -G sudo unicorns \
    && echo "unicorns ALL=(ALL) NOPASSWD:ALL" | tee /etc/sudoers
USER unicorns
WORKDIR /var/tmp/bmbf

# Install Android dependencies
COPY BMBF/BMBF.csproj ./BMBF/BMBF.csproj
COPY BMBF/AndroidManifest.xml ./BMBF/AndroidManifest.xml
COPY BMBF/packages.config ./BMBF/packages.config
COPY BMBF.Patching/BMBF.Patching.csproj ./BMBF.Patching/BMBF.Patching.csproj
COPY BMBF.Resources/BMBF.Resources.csproj ./BMBF.Resources/BMBF.Resources.csproj
COPY BMBF.ModManagement/BMBF.ModManagement.csproj ./BMBF.ModManagement/BMBF.ModManagement.csproj
COPY BMBF.QMod/BMBF.QMod.csproj ./BMBF.QMod/BMBF.QMod.csproj
RUN sudo mkdir /opt/android-sdk \
    && sudo /opt/dotnet/dotnet restore ./BMBF/BMBF.csproj \
    && sudo /opt/dotnet/dotnet msbuild ./BMBF/BMBF.csproj -t:InstallAndroidDependencies -p:AndroidSdkDirectory=/opt/android-sdk -p:AcceptAndroidSDKLicenses=true \
    && cd .. && sudo rm -rf bmbf

USER root
WORKDIR /bmbf
ENV PATH="/opt/dotnet:${PATH}"
