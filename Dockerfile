FROM mcr.microsoft.com/dotnet/sdk:5.0

RUN mkdir -p source/CosmosOS \
 && cd source/CosmosOS \
 && git clone https://github.com/CosmosOS/XSharp --depth=1 \
 && git clone https://github.com/CosmosOS/IL2CPU --depth=1 \
 && git clone https://github.com/CosmosOS/Common --depth=1 \
 && git clone https://github.com/CosmosOS/Cosmos --depth=1 -b netcoreapp2.1 \
 && ln -s /source/CosmosOS/Cosmos/Build /source/CosmosOS/Cosmos/build

RUN cd /source/CosmosOS/IL2CPU \
 && dotnet build \
 && dotnet pack

RUN cd /source/CosmosOS/Cosmos \
 && dotnet pack source/Cosmos.Common \
 && dotnet pack source/Cosmos.Debug.Kernel \
 && dotnet pack source/Cosmos.Debug.Kernel.Plugs.Asm \
 && dotnet pack source/Cosmos.Core \
 && dotnet pack source/Cosmos.Core_Asm \
 && dotnet pack source/Cosmos.Core_Plugs \
 && dotnet pack source/Cosmos.HAL2 \
 && dotnet pack source/Cosmos.System2 \
 && dotnet pack source/Cosmos.System2_Plugs

RUN cd /source/CosmosOS/IL2CPU \
 && dotnet publish source/IL2CPU -r linux-x64 --self-contained

RUN cd /source/CosmosOS/Cosmos \
 && dotnet publish source/Cosmos.Core_Plugs \
 && dotnet publish source/Cosmos.Debug.Kernel.Plugs.Asm \
 && dotnet publish source/Cosmos.HAL2 \
 && dotnet publish source/Cosmos.System2_Plugs

CMD mv /source/CosmosOS/IL2CPU/artifacts/Debug/nupkg/*.nupkg /out/packages \
 && mv /source/CosmosOS/Cosmos/artifacts/Debug/nupkg/*.nupkg /out/packages \
 && mv /source/CosmosOS/IL2CPU/source/Cosmos.Core.DebugStub/*.xs /out/XSharp/DebugStub \
 && mv /source/CosmosOS/Cosmos/Artwork/XSharp/XSharp.ico /out/XSharp \
 && mv /source/CosmosOS/Cosmos/Artwork/Cosmos.ico /out/ \
 && mv /source/CosmosOS/IL2CPU/source/IL2CPU/bin/Debug/net5.0/linux-x64/publish/* out/Build/IL2CPU \
 && mv /source/CosmosOS/Cosmos/source/Cosmos.Core_Plugs/bin/Debug/net5.0/publish/*.dll out/Kernel \
 && mv /source/CosmosOS/Cosmos/source/Cosmos.System2_Plugs/bin/Debug/net5.0/publish/*.dll out/Kernel \
 && mv /source/CosmosOS/Cosmos/source/Cosmos.HAL2/bin/Debug/net5.0/publish/*.dll out/Kernel \
 && mv /source/CosmosOS/Cosmos/source/Cosmos.Debug.Kernel.Plugs.Asm/bin/Debug/netstandard2.0/publish/*.dll out/Kernel \
 && mv /source/CosmosOS/Cosmos/build/HyperV/*.vhdx /out/Build/HyperV \
 && mv /source/CosmosOS/Cosmos/build/VMWare/Workstation/* /out/Build/VMware/Workstation \
 && mv /source/CosmosOS/Cosmos/build/syslinux/* /out/Build/ISO \
 && mv out/Build/ISO/pxe* out/Build/PXE \
 && echo Done.
