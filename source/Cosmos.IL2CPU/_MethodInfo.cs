using System;
using System.Linq;
using System.Reflection;
using IL2CPU.API;
using IL2CPU.API.Attribs;

namespace Cosmos.IL2CPU
{
    public class _MethodInfo
    {
        public enum TypeEnum { Normal, Plug, NeedsPlug };

        public MethodBase MethodBase { get; }
        public TypeEnum Type { get; }
        //TODO: Figure out if we really need three different ids
        public uint UID { get; }
        public long DebugMethodUID { get; set; }
        public long DebugMethodLabelUID { get; set; }
        public long EndMethodID { get; set; }
        public string MethodLabel { get; private set; }
        /// <summary>
        /// The method info for the method which plugs this one
        /// </summary>
        public _MethodInfo PlugMethod { get; }
        public Type MethodAssembler { get; }
        public bool IsInlineAssembler { get; }
        public bool DebugStubOff { get; }

        private _MethodInfo _PluggedMethod;
        /// <summary>
        /// Method which is plugged by this method
        /// </summary>
        public _MethodInfo PluggedMethod
        {
            get => _PluggedMethod; set
            {
                _PluggedMethod = value;
                if (PluggedMethod != null)
                {
                    MethodLabel = "PLUG_FOR___" + LabelName.Get(PluggedMethod.MethodBase);
                }
                else
                {
                    MethodLabel = LabelName.Get(MethodBase);
                }
            }
        }
        public uint LocalVariablesSize { get; set; }

        public bool IsWildcard { get; set; }

        public _MethodInfo(MethodBase aMethodBase, uint aUID, TypeEnum aType, _MethodInfo aPlugMethod, Type aMethodAssembler) : this(aMethodBase, aUID, aType, aPlugMethod, false)
        {
            MethodAssembler = aMethodAssembler;
        }


        public _MethodInfo(MethodBase aMethodBase, uint aUID, TypeEnum aType, _MethodInfo aPlugMethod)
            : this(aMethodBase, aUID, aType, aPlugMethod, false)
        {
        }

        public _MethodInfo(MethodBase aMethodBase, uint aUID, TypeEnum aType, _MethodInfo aPlugMethod, bool isInlineAssembler)
        {
            MethodBase = aMethodBase;
            UID = aUID;
            Type = aType;
            PlugMethod = aPlugMethod;
            IsInlineAssembler = isInlineAssembler;

            var attribs = aMethodBase.GetCustomAttributes<DebugStub>(false).ToList();
            if (attribs.Count != 0)
            {
                var attrib = new DebugStub
                {
                    Off = attribs[0].Off,
                };
                DebugStubOff = attrib.Off;
            }

            MethodLabel = LabelName.Get(MethodBase);
        }
    }
}
