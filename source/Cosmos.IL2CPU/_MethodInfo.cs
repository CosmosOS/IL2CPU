using System;
using System.Linq;
using System.Reflection;
using IL2CPU.API.Attribs;

namespace Cosmos.IL2CPU
{
    public class _MethodInfo
    {
        public enum TypeEnum { Normal, Plug, NeedsPlug };

        public MethodBase MethodBase { get; }
        public TypeEnum Type { get; }
        public UInt32 UID { get; }
        public _MethodInfo PlugMethod { get; }
        public Type MethodAssembler { get; }
        public bool IsInlineAssembler { get; }
        public bool DebugStubOff { get; }
        public _MethodInfo PluggedMethod { get; set; }
        public uint LocalVariablesSize { get; set; }

        public bool IsWildcard { get; set; }

        public _MethodInfo(MethodBase aMethodBase, UInt32 aUID, TypeEnum aType, _MethodInfo aPlugMethod, Type aMethodAssembler) : this(aMethodBase, aUID, aType, aPlugMethod, false)
        {
            MethodAssembler = aMethodAssembler;
        }


        public _MethodInfo(MethodBase aMethodBase, UInt32 aUID, TypeEnum aType, _MethodInfo aPlugMethod)
            : this(aMethodBase, aUID, aType, aPlugMethod, false)
        {
            //MethodBase = aMethodBase;
            //UID = aUID;
            //Type = aType;
            //PlugMethod = aPlugMethod;
        }

        public _MethodInfo(MethodBase aMethodBase, UInt32 aUID, TypeEnum aType, _MethodInfo aPlugMethod, bool isInlineAssembler)
        {
            MethodBase = aMethodBase;
            UID = aUID;
            Type = aType;
            PlugMethod = aPlugMethod;
            IsInlineAssembler = isInlineAssembler;

            var attribs = aMethodBase.GetCustomAttributes<DebugStub>(false).ToList();
            if (attribs.Any())
            {
                DebugStub attrib = new DebugStub
                                            {
                                                Off = attribs[0].Off,
                                            };
                DebugStubOff = attrib.Off;
            }
        }
    }
}
