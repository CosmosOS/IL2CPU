#!/bin/sh
docker build . -t cosmoslinbld
mkdir out
mkdir out/VSIX
mkdir out/packages
mkdir out/Build
mkdir out/Build/IL2CPU
mkdir out/Build/HyperV
mkdir out/Build/VMware
mkdir out/Build/VMware/Workstation
mkdir out/Build/ISO
mkdir out/Build/PXE
mkdir out/Build/PXE/pxelinux.cfg
mkdir out/Build/Tools
mkdir out/Build/Tools/NAsm
mkdir out/Build/Tools/cygwin
mkdir out/Build/Tools/mkisofs
mkdir out/Build/USB
mkdir out/Build/VSIP
mkdir out/Kernel
mkdir out/XSharp
mkdir out/XSharp/DebugStub
docker run -v ./out:/out -it --rm cosmoslinbld
