# IL2CPU

[![Build status](https://ci.appveyor.com/api/projects/status/budqdarf5cj67lp7/branch/master?svg=true)](https://ci.appveyor.com/project/CosmosOS/il2cpu/branch/master)

IL2CPU is a compiler for .NET IL code to compile to assembly language for direct booting. IL2CPU creates NASM style assembly ready to assemble with [NASM](http://www.nasm.us/).

# Steps

1) Create and change to folder:

cd ~/Git/CosmosRepo

2) Check it out:

git clone https://github.com/CosmosOS/Common.git --depth=1

git clone https://github.com/CosmosOS/Cosmos.git --depth=1

git clone https://github.com/xafero/IL2CPU.git --depth=1

3) Build the compiler:

cd IL2CPU

dotnet build

4) Run the tests:

cd tests/IL2CPU.Reflection.Tests

dotnet test

cd ../..

5) Run the compiler:

cd source/IL2CPU

dotnet run ResponseFile:./example.rsp


# More Info
Please refer to our website (http://www.il2cpu.net)

# Status
Currently IL2CPU is used by the [C# Open Source Managed Operating System (COSMOS)](http://www.goCosmos.org) and parts of it are bound to Cosmos. We are in the process of and nearly finished separating out IL2CPU to allow it to operate as a stand alone project to allow users to make their own custom creations using IL2CPU.
