FROM ubuntu:22.04
ARG DEBIAN_FRONTEND=noninteractive

# Install basic packages
RUN apt-get update && apt-get install -y \
    apt-transport-https \
    ca-certificates \
    curl \
    ssh

# Install Java
RUN apt-get install -y openjdk-11-jdk-headless

# Install Node
RUN curl -fsSL https://deb.nodesource.com/setup_16.x | bash \
    && apt-get install -y nodejs \
    && corepack enable 

# Install .NET 6
RUN apt-get install -y dotnet6

# Install Android workload
RUN dotnet workload install android

# Setup work user
RUN apt-get install -y sudo \
    && useradd -MN -s /bin/bash unicorns \
    && echo "unicorns ALL=(ALL) NOPASSWD:ALL" | tee /etc/sudoers
USER unicorns
WORKDIR /var/tmp/bmbf

# Install Android dependencies
COPY nuget.config ./nuget.config
COPY BMBF/BMBF.csproj ./BMBF/BMBF.csproj
COPY BMBF/AndroidManifest.xml ./BMBF/AndroidManifest.xml
COPY BMBF.Backend/BMBF.Backend.csproj ./BMBF.Backend/BMBF.Backend.csproj
COPY BMBF.ModManagement/BMBF.ModManagement.csproj ./BMBF.ModManagement/BMBF.ModManagement.csproj
COPY BMBF.Patching/BMBF.Patching.csproj ./BMBF.Patching/BMBF.Patching.csproj
COPY BMBF.QMod/BMBF.QMod.csproj ./BMBF.QMod/BMBF.QMod.csproj
COPY BMBF.Resources/BMBF.Resources.csproj ./BMBF.Resources/BMBF.Resources.csproj
COPY BMBF.WebServer/BMBF.WebServer.csproj ./BMBF.WebServer/BMBF.WebServer.csproj
RUN sudo mkdir /opt/android-sdk \
    && sudo dotnet restore ./BMBF/BMBF.csproj \
    && sudo dotnet msbuild ./BMBF/BMBF.csproj -t:InstallAndroidDependencies -p:AndroidSdkDirectory=/opt/android-sdk -p:AcceptAndroidSDKLicenses=true \
    && cd .. && sudo rm -rf bmbf

USER root
WORKDIR /bmbf
