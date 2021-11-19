using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Cosmos.IL2CPU.Extensions;

using IL2CPU.API;
using IL2CPU.API.Attribs;

using XSharp.Assembler;

namespace Cosmos.IL2CPU
{
    internal class PlugManager
    {
        public bool ThrowExceptions = true;

        public Action<Exception> LogException = null;

        ////public delegate void ScanMethodDelegate(MethodBase aMethod, bool aIsPlug, string sourceItem);
        //public ScanMethodDelegate ScanMethod = null;
        //public delegate void QueueDelegate(_MemberInfo aItem, object aSrc, string aSrcType, string sourceItem = null);
        //public QueueDelegate Queue = null;

        // Contains a list of plug implementor classes
        // Key = Target Class
        // Value = List of Implementors. There may be more than one
        protected Dictionary<Type, List<Type>> mPlugImpls = new Dictionary<Type, List<Type>>();
        // List of inheritable plugs. Plugs that start at an ancestor and plug all
        // descendants. For example, delegates
        protected Dictionary<Type, List<Type>> mPlugImplsInhrt = new Dictionary<Type, List<Type>>();

        // same as above 2 fields, except for generic plugs
        protected Dictionary<Type, List<Type>> mGenericPlugImpls = new Dictionary<Type, List<Type>>();
        protected Dictionary<Type, List<Type>> mGenericPlugImplsInhrt = new Dictionary<Type, List<Type>>();

        // list of field plugs
        protected IDictionary<Type, IDictionary<string, PlugField>> mPlugFields = new Dictionary<Type, IDictionary<string, PlugField>>();

        public Dictionary<Type, List<Type>> PlugImpls => mPlugImpls;
        public Dictionary<Type, List<Type>> PlugImplsInhrt => mPlugImplsInhrt;
        public IDictionary<Type, IDictionary<string, PlugField>> PlugFields => mPlugFields;

        private TypeResolver _typeResolver;

        private Dictionary<string, MethodBase> ResolvedPlugs = new Dictionary<string, MethodBase>();

        private static string BuildMethodKeyName(MethodBase m)
        {
            return LabelName.GetFullName(m);
        }

        public PlugManager(Action<Exception> aLogException, Action<string> aLogWarning, TypeResolver typeResolver)
        {
            LogException = aLogException;
            LogWarning = aLogWarning;

            _typeResolver = typeResolver;
        }

        public void FindPlugImpls(IEnumerable<Assembly> assemblies)
        {
            // TODO: Cache method list with info - so we dont have to keep
            // scanning attributes for enabled etc repeatedly
            // TODO: New plug system, common plug base which all descend from
            // It can have a "this" member and then we
            // can separate static from instance by the static keyword
            // and ctors can be static "ctor" by name
            // Will still need plug attrib though to specify target
            // Also need to handle asm plugs, but those will be different anyways
            // TODO: Allow whole class plugs? ie, a class that completely replaces another class
            // and is substituted on the fly? Plug scanner would direct all access to that
            // class and throw an exception if any method, field, member etc is missing.

            foreach (var xAsm in assemblies)
            {
                LogWarning("Loading plugs from assembly: " + xAsm.FullName);
                // Find all classes marked as a Plug
                foreach (var xPlugType in xAsm.GetTypes())
                {
                    // Foreach, it is possible there could be one plug class with mult plug targets
                    foreach (var xAttrib in xPlugType.GetCustomAttributes<Plug>(false))
                    {
                        var xTargetType = xAttrib.Target;
                        // If no type is specified, try to find by a specified name.
                        // This is needed in cross assembly references where the
                        // plug cannot reference the assembly of the target type
                        if (xTargetType == null)
                        {
                            try
                            {
                                xTargetType = _typeResolver.ResolveType(xAttrib.TargetName, true, false);
                            }
                            catch (Exception ex)
                            {
                                if (!xAttrib.IsOptional)
                                {
                                    throw new Exception("Error", ex);
                                }
                                continue;
                            }
                        }

                        Dictionary<Type, List<Type>> mPlugs;
                        if (xTargetType.ContainsGenericParameters)
                        {
                            mPlugs = xAttrib.Inheritable ? mGenericPlugImplsInhrt : mGenericPlugImpls;
                        }
                        else
                        {
                            mPlugs = xAttrib.Inheritable ? mPlugImplsInhrt : mPlugImpls;
                        }
                        if (mPlugs.TryGetValue(xTargetType, out var xImpls))
                        {
                            xImpls.Add(xPlugType);
                        }
                        else
                        {
                            mPlugs.Add(xTargetType, new List<Type>() { xPlugType });
                        }
                    }
                }
            }
        }

        public void ScanFoundPlugs()
        {
            ScanPlugs(mPlugImpls);
            ScanPlugs(mPlugImplsInhrt);
        }

        public void ScanPlugs(Dictionary<Type, List<Type>> aPlugs)
        {
            foreach (var xPlug in aPlugs)
            {
                var xImpls = xPlug.Value;
                foreach (var xImpl in xImpls)
                {
                    #region PlugMethods scan

                    foreach (var xMethod in xImpl.GetMethods(BindingFlags.Public | BindingFlags.Static))
                    {
                        PlugMethod xAttrib = null;
                        foreach (PlugMethod x in xMethod.GetCustomAttributes(typeof(PlugMethod), false))
                        {
                            xAttrib = x;
                        }
                        if (xAttrib == null)
                        {
                            //At this point we need to check the plug method actually
                            //matches a method that might need plugging.
                            // x08 bug
                            // We must check for a number of cases:
                            //   - Public, static and private/internal methods that need plugging
                            //   - Ctor or Cctor

                            bool OK = false;
                            if (String.Equals(xMethod.Name, "ctor", StringComparison.OrdinalIgnoreCase)
                                || String.Equals(xMethod.Name, "cctor", StringComparison.OrdinalIgnoreCase))
                            {
                                OK = true;
                            }
                            else
                            {
                                // Skip checking methods related to fields because it's just too messy...
                                // We also skip methods which do method access.
                                if (xMethod.GetParameters().Where(x =>
                                {
                                    return x.GetCustomAttributes(typeof(FieldAccess)).Any()
                                           || x.GetCustomAttributes(typeof(ObjectPointerAccess)).Any();
                                }).Any())
                                {
                                    OK = true;
                                }
                                else
                                {
                                    var xParamTypes = xMethod.GetParameters().Select(delegate (ParameterInfo x)
                                    {
                                        var result = x.ParameterType;
                                        if (result.IsByRef)
                                        {
                                            result = result.GetElementType();
                                        }
                                        else if (result.IsPointer)
                                        {
                                            result = null;
                                        }
                                        return result;
                                    }).ToArray();

                                    var posMethods = xPlug.Key.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
                                                                            .Where(x => x.Name == xMethod.Name);
                                    foreach (MethodInfo posInf in posMethods)
                                    {
                                        // If static, no this param
                                        // Otherwise, take into account first param is this param
                                        //This param is either of declaring type, or ref to declaring type or pointer
                                        var posMethParamTypes = posInf.GetParameters().Select(delegate (ParameterInfo x)
                                        {
                                            var result = x.ParameterType;
                                            if (result.IsByRef)
                                            {
                                                result = result.GetElementType();
                                            }
                                            else if (result.IsPointer)
                                            {
                                                result = null;
                                            }
                                            return result;
                                        }).ToArray();

                                        if (posInf.IsStatic)
                                        {
                                            if (posMethParamTypes.Length != xParamTypes.Length)
                                            {
                                                continue;
                                            }

                                            OK = true;
                                            // Exact params match incl. pointers - which shuld be "null" types for statics since some could be pointers
                                            for (int i = 0; i < posMethParamTypes.Length; i++)
                                            {
                                                if ((posMethParamTypes[i] == null && xParamTypes[i] != null) ||
                                                    (posMethParamTypes[i] != null && !posMethParamTypes[i].Equals(xParamTypes[i])))
                                                {
                                                    OK = false;
                                                    break;
                                                }
                                            }

                                            if (!OK)
                                            {
                                                continue;
                                            }
                                            else
                                            {
                                                break;
                                            }
                                        }
                                        else
                                        {
                                            // Exact match except possibly 1st param
                                            if (posMethParamTypes.Length != xParamTypes.Length &&
                                                posMethParamTypes.Length != xParamTypes.Length - 1)
                                            {
                                                continue;
                                            }
                                            int offset = 0;

                                            OK = true;
                                            // Exact match except if first param doesn't match, we skip 1st param and restart matching
                                            for (int i = 0; i < posMethParamTypes.Length && (i + offset) < xParamTypes.Length; i++)
                                            {
                                                //Continue if current type is null i.e. was a pointer as that could be any type originally.
                                                if (xParamTypes[i + offset] != null && !xParamTypes[i + offset].Equals(posMethParamTypes[i]))
                                                {
                                                    if (offset == 0)
                                                    {
                                                        offset = 1;
                                                        i = -1;
                                                    }
                                                    else
                                                    {
                                                        OK = false;
                                                        break;
                                                    }
                                                }
                                            }
                                            if (posMethParamTypes.Length == 0 && xParamTypes.Length > 0)
                                            {
                                                //We use IsAssignableFrom here because _some_ plugs decide to use more generic types for the
                                                //this parameter
                                                OK = xParamTypes[0] == null || xParamTypes[0].IsAssignableFrom(posInf.DeclaringType);
                                            }

                                            if (!OK)
                                            {
                                                continue;
                                            }
                                            else
                                            {
                                                break;
                                            }
                                        }
                                    }
                                }
                            }

                            if (!OK)
                            {
                                if (xAttrib == null
                                    || xAttrib.IsOptional)
                                {
                                    if (LogWarning != null)
                                    {
                                        LogWarning("Invalid plug method! Target method not found. : " + xMethod.GetFullName());
                                    }
                                }
                            }
                        }
                        else
                        {
                            if (xAttrib.IsWildcard
                                && xAttrib.Assembler == null)
                            {
                                if (LogWarning != null)
                                {
                                    LogWarning("Wildcard PlugMethods need to use an assembler for now.");
                                }
                            }
                        }
                    }

                    #endregion

                    #region PlugFields scan

                    foreach (var xField in xImpl.GetCustomAttributes(typeof(PlugField), true).Cast<PlugField>())
                    {
                        if (!mPlugFields.TryGetValue(xPlug.Key, out var xFields))
                        {
                            xFields = new Dictionary<string, PlugField>();
                            mPlugFields.Add(xPlug.Key, xFields);
                        }
                        if (xFields.ContainsKey(xField.FieldId))
                        {
                            throw new Exception("Duplicate PlugField found for field '" + xField.FieldId + "'!");
                        }
                        xFields.Add(xField.FieldId, xField);
                    }

                    #endregion
                }
            }
        }

        public Action<string> LogWarning;

        private MethodBase ResolvePlug(Type aTargetType, List<Type> aImpls, MethodBase aMethod, Type[] aParamTypes)
        {
            //TODO: This method is "reversed" from old - remember that when porting
            MethodBase xResult = null;

            // Setup param types for search
            Type[] xParamTypes;
            if (aMethod.IsStatic)
            {
                xParamTypes = aParamTypes;
            }
            else
            {
                // If its an instance method, we have to add this to the ParamTypes to search
                xParamTypes = new Type[aParamTypes.Length + 1];
                if (aParamTypes.Length > 0)
                {
                    aParamTypes.CopyTo(xParamTypes, 1);
                }
                xParamTypes[0] = aTargetType;
            }

            PlugMethod xAttrib = null;
            foreach (var xImpl in aImpls)
            {
                // TODO: cleanup this loop, next statement shouldnt be neccessary
                if (xResult != null)
                {
                    break;
                }
                // Plugs methods must be static, and public
                // Search for non signature matches first since signature searches are slower
                xResult = xImpl.GetMethod(aMethod.Name, BindingFlags.Static | BindingFlags.Public,
                    null, xParamTypes, null);

                if (xResult == null && aMethod.Name == ".ctor")
                {
                    xResult = xImpl.GetMethod("Ctor", BindingFlags.Static | BindingFlags.Public,
                        null, xParamTypes, null);
                }
                if (xResult == null && aMethod.Name == ".cctor")
                {
                    xResult = xImpl.GetMethod("CCtor", BindingFlags.Static | BindingFlags.Public,
                        null, xParamTypes, null);
                }

                if (xResult == null)
                {
                    // Search by signature
                    foreach (var xSigMethod in xImpl.GetMethods(BindingFlags.Static | BindingFlags.Public))
                    {
                        // TODO: Only allow one, but this code for now takes the last one
                        // if there is more than one
                        xAttrib = null;
                        foreach (PlugMethod x in xSigMethod.GetCustomAttributes(typeof(PlugMethod), false))
                        {
                            xAttrib = x;
                        }

                        if (xAttrib != null && (xAttrib.IsWildcard && !xAttrib.WildcardMatchParameters))
                        {
                            MethodBase xTargetMethod = null;
                            if (String.Equals(xSigMethod.Name, "Ctor", StringComparison.OrdinalIgnoreCase)
                                || String.Equals(xSigMethod.Name, "Cctor", StringComparison.OrdinalIgnoreCase))
                            {
                                xTargetMethod = aTargetType.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance).SingleOrDefault();
                            }
                            else
                            {
                                xTargetMethod = (from item in aTargetType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance)
                                                 where item.Name == xSigMethod.Name
                                                 select item).SingleOrDefault();
                            }
                            if (xTargetMethod == aMethod)
                            {
                                xResult = xSigMethod;
                            }
                        }
                        else
                        {

                            var xParams = xSigMethod.GetParameters();
                            //TODO: Static method plugs dont seem to be separated
                            // from instance ones, so the only way seems to be to try
                            // to match instance first, and if no match try static.
                            // I really don't like this and feel we need to find
                            // an explicit way to determine or mark the method
                            // implementations.
                            //
                            // Plug implementations take "this" as first argument
                            // so when matching we don't include it in the search
                            Type[] xTypesInst = null;
                            var xActualParamCount = xParams.Length;
                            foreach (var xParam in xParams)
                            {
                                if (xParam.GetCustomAttributes(typeof(FieldAccess), false).Length != 0)
                                {
                                    xActualParamCount--;
                                }
                            }
                            var xTypesStatic = new Type[xActualParamCount];
                            // If 0 params, has to be a static plug so we skip
                            // any copying and leave xTypesInst = null
                            // If 1 params, xTypesInst must be converted to Type[0]
                            if (xActualParamCount == 1)
                            {
                                xTypesInst = Array.Empty<Type>();

                                var xReplaceType = xParams[0].GetCustomAttributes(typeof(FieldType), false).ToList();
                                if (xReplaceType.Count != 0)
                                {
                                    xTypesStatic[0] = _typeResolver.ResolveType(((FieldType)xReplaceType[0]).Name, true);
                                }
                                else
                                {
                                    xTypesStatic[0] = xParams[0].ParameterType;
                                }
                            }
                            else if (xActualParamCount > 1)
                            {
                                xTypesInst = new Type[xActualParamCount - 1];
                                var xCurIdx = 0;
                                foreach (var xParam in xParams.Skip(1))
                                {
                                    if (xParam.GetCustomAttributes(typeof(FieldAccess), false).Length != 0)
                                    {
                                        continue;
                                    }

                                    var xReplaceType = xParam.GetCustomAttributes(typeof(FieldType), false).ToList();
                                    if (xReplaceType.Count!=0)
                                    {
                                        xTypesInst[xCurIdx] = _typeResolver.ResolveType(((FieldType)xReplaceType[0]).Name, true);
                                    }
                                    else
                                    {
                                        xTypesInst[xCurIdx] = xParam.ParameterType;
                                    }

                                    xCurIdx++;
                                }
                                xCurIdx = 0;
                                foreach (var xParam in xParams)
                                {
                                    if (xParam.GetCustomAttributes(typeof(FieldAccess), false).Length != 0)
                                    {
                                        xCurIdx++;
                                        continue;
                                    }
                                    if (xCurIdx >= xTypesStatic.Length)
                                    {
                                        break;
                                    }
                                    xTypesStatic[xCurIdx] = xParam.ParameterType;
                                    xCurIdx++;
                                }
                            }
                            MethodBase xTargetMethod = null;
                            // TODO: In future make rule that all ctor plugs are called
                            // ctor by name, or use a new attrib
                            //TODO: Document all the plug stuff in a document on website
                            //TODO: To make inclusion of plugs easy, we can make a plugs master
                            // that references the other default plugs so user exes only
                            // need to reference that one.
                            // TODO: Skip FieldAccessAttribute if in impl
                            if (xTypesInst != null)
                            {
                                if (String.Equals(xSigMethod.Name, "ctor", StringComparison.OrdinalIgnoreCase))
                                {
                                    xTargetMethod = aTargetType.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, CallingConventions.Any, xTypesInst, null);
                                }
                                else
                                {
                                    xTargetMethod = aTargetType.GetMethod(xSigMethod.Name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, CallingConventions.Any, xTypesInst, null);
                                }
                            }
                            // Not an instance method, try static
                            if (xTargetMethod == null)
                            {
                                if (String.Equals(xSigMethod.Name, "cctor", StringComparison.OrdinalIgnoreCase)
                                    || String.Equals(xSigMethod.Name, "ctor", StringComparison.OrdinalIgnoreCase))
                                {
                                    xTargetMethod = aTargetType.GetConstructor(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, CallingConventions.Any, xTypesStatic, null);
                                }
                                else
                                {

                                    xTargetMethod = aTargetType.GetMethod(xSigMethod.Name, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, CallingConventions.Any, xTypesStatic, null);
                                }
                            }
                            if (xTargetMethod == aMethod)
                            {
                                xResult = xSigMethod;
                                break;
                            }
                            if (xAttrib?.Signature != null)
                            {
                                var xName = DataMember.FilterStringForIncorrectChars(LabelName.GetFullName(aMethod));
                                if (String.Equals(xName.Replace("_", ""), xAttrib.Signature.Replace("_", ""), StringComparison.OrdinalIgnoreCase))
                                {
                                    xResult = xSigMethod;
                                    break;
                                }
                            }
                            xAttrib = null;
                        }
                    }
                }
                else
                {
                    // check if signatur is equal
                    var xResPara = xResult.GetParameters();
                    var xAMethodPara = aMethod.GetParameters();
                    if (aMethod.IsStatic)
                    {
                        if (xResPara.Length != xAMethodPara.Length)
                        {
                            return null;
                        }
                    }
                    else
                    {
                        if (xResPara.Length - 1 != xAMethodPara.Length)
                        {
                            return null;
                        }
                    }
                    for (int i = 0; i < xAMethodPara.Length; i++)
                    {
                        int correctIndex = aMethod.IsStatic ? i : i + 1;
                        if (xResPara[correctIndex].ParameterType != xAMethodPara[i].ParameterType && xResPara[correctIndex].ParameterType.Name != "Object") // to cheat if we cant access the actual type
                        {
                            // Allow explicit overwriting of types by signature in case we have to hide internal enum behind uint etc
                            if(xResult.GetCustomAttribute<PlugMethod>()?.Signature.Replace("_","") == DataMember.FilterStringForIncorrectChars(LabelName.GetFullName(aMethod)).Replace("_", ""))
                            {

                            }
                            else
                            {
                                return null;
                            }
                        }
                    }
                    if (xResult.Name == "Ctor" && aMethod.Name == ".ctor")
                    {
                    }
                    else if (xResult.Name == "CCtor" && aMethod.Name == ".cctor")
                    {
                    }
                    else if (xResult.Name != aMethod.Name)
                    {
                        return null;
                    }
                }
            }
            if (xResult == null)
            {
                return null;
            }

            // If we found a matching method, check for attributes
            // that might disable it.
            //TODO: For signature ones, we could cache the attrib. Thats
            // why we check for null here
            if (xAttrib == null)
            {
                // TODO: Only allow one, but this code for now takes the last one
                // if there is more than one
                foreach (PlugMethod x in xResult.GetCustomAttributes(typeof(PlugMethod), false))
                {
                    xAttrib = x;
                }
            }

            // See if we need to disable this plug
            if (xAttrib != null)
            {
                if (!xAttrib.Enabled)
                {
                    //xResult = null;
                    return null;
                }

                //else if (xAttrib.Signature != null) {
                //  var xName = DataMember.FilterStringForIncorrectChars(MethodInfoLabelGenerator.GetFullName(xResult));
                //  if (string.Compare(xName, xAttrib.Signature, true) != 0) {
                //    xResult = null;
                //  }
                //}
            }

            //if (xAttrib != null && xAttrib.Signature != null)
            //{
            //    var xTargetMethods = aTargetType.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            //    //System_Void__Indy_IL2CPU_Assembler_Assembler__cctor__
            //    //If signature exists, the search is slow. Signatures
            //    //are infrequent though, so for now we just go slow method
            //    //and have not optimized or cached this info. When we
            //    //redo the plugs, we can fix this.
            //    bool xEnabled=true;
            //    foreach (var xTargetMethod in xTargetMethods)
            //    {
            //        string sName = DataMember.FilterStringForIncorrectChars(MethodInfoLabelGenerator.GetFullName(xTargetMethod));
            //        if (string.Compare(sName, xAttrib.Signature, true) == 0)
            //        {
            //            //uint xUID = QueueMethod(xPlugImpl.Plug, "Plug", xMethod, true);
            //            //mMethodPlugs.Add(xTargetMethod, new PlugInfo(xUID, xAttrib.Assembler));
            //            // Mark as disabled, because we already handled it
            //            xEnabled = false;
            //            break;
            //        }
            //    }
            //    // if still enabled, we didn't find our method
            //    if (xEnabled)
            //    {
            //        // todo: more precise error: imagine having a 100K line project, and this error happens...
            //        throw new Exception("Plug target method not found.");
            //    }
            //}
            return xResult;
        }

        public MethodBase ResolvePlug(MethodBase aMethod, Type[] aParamTypes)
        {
            var xMethodKey = BuildMethodKeyName(aMethod);
            if (ResolvedPlugs.TryGetValue(xMethodKey, out var xResult))
            {
                return xResult;
            }
            else
            {
                // Check for exact type plugs first, they have precedence
                if (mPlugImpls.TryGetValue(aMethod.DeclaringType, out var xImpls))
                {
                    xResult = ResolvePlug(aMethod.DeclaringType, xImpls, aMethod, aParamTypes);
                }

                // Check for inheritable plugs second.
                // We also need to fall through at method level, not just type.
                // That is a exact type plug could exist, but not method match.
                // In such a case the Inheritable methods should still be searched
                // if there is a inheritable type match.
                if (xResult == null)
                {
                    foreach (var xInheritable in mPlugImplsInhrt)
                    {
                        if (aMethod.DeclaringType.IsSubclassOf(xInheritable.Key))
                        {
                            xResult = ResolvePlug(aMethod.DeclaringType /*xInheritable.Key*/, xInheritable.Value, aMethod, aParamTypes);
                            if (xResult != null)
                            {
                                // prevent key overriding.
                                break;
                            }
                        }
                    }
                }
                if (xResult == null)
                {
                    xImpls = null;
                    if (aMethod.DeclaringType.IsGenericType)
                    {
                        var xMethodDeclaringTypeDef = aMethod.DeclaringType.GetGenericTypeDefinition();
                        if (mGenericPlugImpls.TryGetValue(xMethodDeclaringTypeDef, out xImpls))
                        {
                            var xBindingFlagsToFindMethod = BindingFlags.Default;
                            if (aMethod.IsPublic)
                            {
                                xBindingFlagsToFindMethod = BindingFlags.Public;
                            }
                            else
                            {
                                // private
                                xBindingFlagsToFindMethod = BindingFlags.NonPublic;
                            }
                            if (aMethod.IsStatic)
                            {
                                xBindingFlagsToFindMethod |= BindingFlags.Static;
                            }
                            else
                            {
                                xBindingFlagsToFindMethod |= BindingFlags.Instance;
                            }
                            var xGenericMethod = (from item in xMethodDeclaringTypeDef.GetMethods(xBindingFlagsToFindMethod)
                                                  where item.Name == aMethod.Name && item.GetParameters().Length == aParamTypes.Length
                                                  select item).SingleOrDefault();
                            if (xGenericMethod != null)
                            {
                                var xTempResult = ResolvePlug(xMethodDeclaringTypeDef, xImpls, xGenericMethod, aParamTypes);

                                if (xTempResult != null)
                                {
                                    if (xTempResult.DeclaringType.IsGenericTypeDefinition)
                                    {
                                        var xConcreteTempResultType = xTempResult.DeclaringType.MakeGenericType(aMethod.DeclaringType.GetGenericArguments());
                                        xResult = (from item in xConcreteTempResultType.GetMethods(BindingFlags.Static | BindingFlags.Public)
                                                   where item.Name == aMethod.Name && item.GetParameters().Length == aParamTypes.Length
                                                   select item).SingleOrDefault();
                                    }
                                }
                            }
                        }
                    }
                }

                ResolvedPlugs[xMethodKey] = xResult;

                return xResult;
            }
        }
    }
}
