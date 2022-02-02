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
ENV PATH="/opt/dotnet:${PATH}"

# Install Java
RUN apt-get install -y openjdk-11-jdk-headless

# Install Node
RUN curl -fsSL https://deb.nodesource.com/setup_16.x | bash \
    && apt-get install -y nodejs

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
RUN dotnet workload install android
