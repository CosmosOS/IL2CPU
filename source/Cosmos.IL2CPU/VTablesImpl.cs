using System;
using System.Diagnostics.CodeAnalysis;

using Cosmos.Debug.Kernel;

namespace Cosmos.IL2CPU
{
    // todo: optimize this, probably using assembler
    [SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix")]
    [SuppressMessage("Usage", "CA2211:Non-constant fields should not be visible")]
    [SuppressMessage("Style", "IDE0011:Add braces")]
    public static partial class VTablesImpl
    {
        // this field seems to be always empty, but the VTablesImpl class is embedded in the final exe.
        public static VTable[] mTypes;

        static VTablesImpl()
        {

        }

        public static bool IsInstance(uint aObjectType, uint aDesiredObjectType, bool aIsInterface)
        {
            if (aObjectType == 0)
            {
                return true;
            }

            if (aIsInterface)
            {
                var xType = mTypes[aObjectType];

                for (int i = 0; i < xType.InterfaceCount; i++)
                {
                    if (xType.InterfaceIndexes[i] == aDesiredObjectType)
                    {
                        return true;
                    }
                }

                return false;
            }

            var xCurrentType = aObjectType;

            do
            {
                if (xCurrentType == aDesiredObjectType)
                {
                    return true;
                }

                if (xCurrentType == mTypes[xCurrentType].BaseTypeIdentifier)
                {
                    Debug("IsInstance failed (1):");
                    DebugHex("aObjectType: ", aObjectType);
                    DebugHex("aDesiredObjectType: ", aDesiredObjectType);

                    return false;
                }

                xCurrentType = mTypes[xCurrentType].BaseTypeIdentifier;
            }
            while (xCurrentType != 0);

            Debug("IsInstance failed (2):");
            DebugHex("aObjectType: ", aObjectType);
            DebugHex("aDesiredObjectType: ", aDesiredObjectType);

            return false;
        }

        public static void SetTypeInfo(
          int aType, uint aBaseType, uint aInterfaceCount, uint[] aInterfaceIndexes,
          uint aMethodCount, uint[] aMethodIndexes, uint[] aMethodAddresses,
          uint aInterfaceMethodCount, uint[] aInterfaceMethodIndexes, uint[] aTargetMethodIndexes)
        {
            //DebugHex("SetTypeInfo - Type", (uint)aType);
            //DebugHex("SetTypeInfo - BaseType", aBaseType);
            //DebugHex("SetTypeInfo - InterfaceCount", aInterfaceCount);
            //foreach (uint t in aInterfaceIndexes)
            //{
            //  DebugHex("SetTypeInfo - Interface Indexes", t);
            //}
            //DebugHex("SetTypeInfo - MethodCount", aMethodCount);
            //foreach (uint t in aMethodIndexes)
            //{
            //  DebugHex("SetTypeInfo - Method Indexes", t);
            //}
            //foreach (uint t in aMethodAddresses)
            //{
            //  DebugHex("SetTypeInfo - Method Addresses", t);
            //}
            //DebugHex("SetTypeInfo - Interface Method Count", aInterfaceMethodCount);
            //foreach (uint t in aMethodIndexes)
            //{
            //  DebugHex("SetTypeInfo - Interface Method IDs", t);
            //}
            //foreach (uint t in aMethodAddresses)
            //{
            //  DebugHex("SetTypeInfo - Target Method IDs", t);
            //}

            mTypes[aType].BaseTypeIdentifier = aBaseType;
            mTypes[aType].InterfaceCount = aInterfaceCount;
            mTypes[aType].InterfaceIndexes = aInterfaceIndexes;
            mTypes[aType].MethodCount = aMethodCount;
            mTypes[aType].MethodIndexes = aMethodIndexes;
            mTypes[aType].MethodAddresses = aMethodAddresses;
            mTypes[aType].InterfaceMethodCount = aInterfaceMethodCount;
            mTypes[aType].InterfaceMethodIndexes = aInterfaceMethodIndexes;
            mTypes[aType].TargetMethodIndexes = aTargetMethodIndexes;
        }

        public static void SetInterfaceInfo(int aType, int aInterfaceIndex, uint aInterfaceIdentifier)
        {
            //DebugHex("SetInterfaceInfo - Type", (uint)aType);
            //DebugHex("SetInterfaceInfo - InterfaceIndex", (uint)aInterfaceIndex);
            //DebugHex("SetInterfaceInfo - InterfaceIdentifier", aInterfaceIdentifier);

            mTypes[aType].InterfaceIndexes[aInterfaceIndex] = aInterfaceIdentifier;

            if (mTypes[aType].InterfaceIndexes[aInterfaceIndex] != aInterfaceIdentifier)
            {
                DebugAndHalt("Setting interface info failed!");
            }
        }

        public static void SetMethodInfo(int aType, int aMethodIndex, uint aMethodIdentifier, uint aMethodAddress)
        {
            //DebugHex("SetMethodInfo - Type", (uint)aType);
            //DebugHex("SetMethodInfo - MethodIndex", (uint)aMethodIndex);
            //DebugHex("SetMethodInfo - MethodId", aMethodIdentifier);
            //DebugHex("SetMethodInfo - MethodAddress", aMethodAddress);

            mTypes[aType].MethodIndexes[aMethodIndex] = aMethodIdentifier;
            mTypes[aType].MethodAddresses[aMethodIndex] = aMethodAddress;

            if (mTypes[aType].MethodIndexes[aMethodIndex] != aMethodIdentifier)
            {
                DebugAndHalt("Setting method info failed! (1)");
            }
        }

        public static void SetInterfaceMethodInfo(int aType, int aMethodIndex, uint aInterfaceMethodId, uint aTargetMethodId)
        {
            //DebugHex("SetInterfaceMethodInfo - Type", (uint)aType);
            //DebugHex("SetInterfaceMethodInfo - MethodIndex", (uint)aMethodIndex);
            //DebugHex("SetInterfaceMethodInfo - InterfaceMethodId", aInterfaceMethodId);
            //DebugHex("SetInterfaceMethodInfo - TargetMethodId", aTargetMethodId);

            mTypes[aType].InterfaceMethodIndexes[aMethodIndex] = aInterfaceMethodId;
            mTypes[aType].TargetMethodIndexes[aMethodIndex] = aTargetMethodId;
        }

        public static uint GetMethodAddressForType(uint aType, uint aMethodId)
        {
            if (aType > 0xFFFF)
            {
                EnableDebug = true;
                DebugHex("Type", aType);
                DebugHex("MethodId", aMethodId);
                Debugger.DoBochsBreak();
                Debugger.SendKernelPanic(KernelPanics.VMT_TypeIdInvalid);
                while (true) ;
            }
            var xCurrentType = aType;
            do
            {
                DebugHex("Now checking type", xCurrentType);
                var xCurrentTypeInfo = mTypes[xCurrentType];
                DebugHex("It's basetype is", xCurrentTypeInfo.BaseTypeIdentifier);

                if (xCurrentTypeInfo.MethodIndexes == null)
                {
                    EnableDebug = true;
                    DebugHex("MethodIndexes is null for type", aType);
                    Debugger.DoBochsBreak();
                    Debugger.SendKernelPanic(KernelPanics.VMT_MethodIndexesNull);
                    while (true) ;
                }
                if (xCurrentTypeInfo.MethodAddresses == null)
                {
                    EnableDebug = true;
                    DebugHex("MethodAddresses is null for type", aType);
                    Debugger.DoBochsBreak();
                    Debugger.SendKernelPanic(KernelPanics.VMT_MethodAddressesNull);
                    while (true) ;
                }

                for (int i = 0; i < xCurrentTypeInfo.MethodIndexes.Length; i++)
                {
                    if (xCurrentTypeInfo.MethodIndexes[i] == aMethodId)
                    {
                        var xResult = xCurrentTypeInfo.MethodAddresses[i];
                        if (xResult < 1048576) // if pointer is under 1MB, some issue exists!
                        {
                            EnableDebug = true;
                            DebugHex("Type", xCurrentType);
                            DebugHex("MethodId", aMethodId);
                            DebugHex("Result", xResult);
                            DebugHex("i", (uint)i);
                            DebugHex("MethodCount", xCurrentTypeInfo.MethodCount);
                            DebugHex("MethodAddresses.Length", (uint)xCurrentTypeInfo.MethodAddresses.Length);
                            Debug("Method found, but address is invalid!");
                            Debugger.DoBochsBreak();
                            Debugger.SendKernelPanic(KernelPanics.VMT_MethodFoundButAddressInvalid);
                            while (true)
                                ;
                        }
                        Debug("Found.");
                        return xResult;
                    }
                }
                if (xCurrentType == xCurrentTypeInfo.BaseTypeIdentifier)
                {
                    Debug("Ultimate base type already found!");
                    break;
                }
                xCurrentType = xCurrentTypeInfo.BaseTypeIdentifier;
            }
            while (true);

            EnableDebug = true;
            DebugHex("Type", aType);
            DebugHex("MethodId", aMethodId);
            Debug("Not FOUND!");
            Debugger.DoBochsBreak();
            Debugger.SendKernelPanic(KernelPanics.VMT_MethodNotFound);
            while (true) ;
            throw new Exception("Cannot find virtual method!");
        }

        public static uint GetMethodAddressForInterfaceType(uint aType, uint aInterfaceMethodId)
        {
            if (aType > 0xFFFF)
            {
                EnableDebug = true;
                DebugHex("Type", aType);
                DebugHex("InterfaceMethodId", aInterfaceMethodId);
                Debugger.SendKernelPanic(KernelPanics.VMT_TypeIdInvalid);
                while (true) ;
            }

            var xTypeInfo = mTypes[aType];

            if (xTypeInfo.InterfaceMethodIndexes == null)
            {
                EnableDebug = true;
                DebugHex("InterfaceMethodIndexes is null for type", aType);
                Debugger.SendKernelPanic(KernelPanics.VMT_MethodIndexesNull);
                while (true) ;
            }

            if (xTypeInfo.TargetMethodIndexes == null)
            {
                EnableDebug = true;
                DebugHex("TargetMethodIndexes is null for type", aType);
                Debugger.SendKernelPanic(KernelPanics.VMT_MethodAddressesNull);
                while (true) ;
            }

            for (int i = 0; i < xTypeInfo.InterfaceMethodIndexes.Length; i++)
            {
                if (xTypeInfo.InterfaceMethodIndexes[i] == aInterfaceMethodId)
                {
                    var xTargetMethodId = xTypeInfo.TargetMethodIndexes[i];
                    return GetMethodAddressForType(aType, xTargetMethodId);
                }
            }

            EnableDebug = true;
            DebugHex("Type", aType);
            DebugHex("InterfaceMethodId", aInterfaceMethodId);
            Debug("Not FOUND!");

            Debugger.SendKernelPanic(KernelPanics.VMT_MethodNotFound);
            while (true) ;
        }
    }

    [SuppressMessage("Design", "CA1051:Do not declare visible instance fields")]
    public struct VTable
    {
        public uint BaseTypeIdentifier;

        public uint InterfaceCount;
        public uint[] InterfaceIndexes;

        public uint MethodCount;
        public uint[] MethodIndexes;
        public uint[] MethodAddresses;

        public uint InterfaceMethodCount;
        public uint[] InterfaceMethodIndexes;
        public uint[] TargetMethodIndexes;
    }
}
