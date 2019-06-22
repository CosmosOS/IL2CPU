using System;
using System.Linq;

using IL2CPU.API.Attribs;
using IL2CPU.Reflection;

namespace Cosmos.IL2CPU
{
    public class _MethodInfo
    {
        public enum TypeEnum { Normal, Plug, NeedsPlug };

        public MethodInfo MethodInfo { get; }
        public TypeEnum Type { get; }
        public uint UID { get; }
        public _MethodInfo PlugMethod { get; }
        public Type MethodAssembler { get; }
        public bool IsInlineAssembler { get; }
        public bool DebugStubOff { get; }
        public _MethodInfo PluggedMethod { get; set; }
        public uint LocalVariablesSize { get; set; }

        public bool IsWildcard { get; set; }

        public _MethodInfo(MethodInfo aMethodInfo, uint aUID, TypeEnum aType, _MethodInfo aPlugMethod, Type aMethodAssembler)
            : this(aMethodInfo, aUID, aType, aPlugMethod, false)
        {
            MethodAssembler = aMethodAssembler;
        }


        public _MethodInfo(MethodInfo aMethodInfo, uint aUID, TypeEnum aType, _MethodInfo aPlugMethod)
            : this(aMethodInfo, aUID, aType, aPlugMethod, false)
        {
            //MethodBase = aMethodBase;
            //UID = aUID;
            //Type = aType;
            //PlugMethod = aPlugMethod;
        }

        public _MethodInfo(MethodInfo aMethodInfo, uint aUID, TypeEnum aType, _MethodInfo aPlugMethod, bool isInlineAssembler)
        {
            MethodInfo = aMethodInfo;
            UID = aUID;
            Type = aType;
            PlugMethod = aPlugMethod;
            IsInlineAssembler = isInlineAssembler;

            var attribs = aMethodInfo.GetCustomAttributes<DebugStub>(false).ToList();
            if (attribs.Any())
            {
                var attrib = new DebugStub { Off = attribs[0].Off };
                DebugStubOff = attrib.Off;
            }
        }
    }
}
