using System;
using System.Collections.Generic;
using System.Reflection;

using IL2CPU.API;

namespace Cosmos.IL2CPU
{
    internal class MemberInfoComparer : IEqualityComparer<MemberInfo>
    {
        public static MemberInfoComparer Instance { get; } = new MemberInfoComparer();

        public bool Equals(MemberInfo x, MemberInfo y)
        {
            if (x == null)
            {
                return y == null;
            }

            if (y == null)
            {
                return false;
            }

            if (x.GetType() == y.GetType())
            {
                if (x.MetadataToken == y.MetadataToken && x.Module == y.Module)
                {
                    if (x is MethodBase xMethod && y is MethodBase yMethod)
                    {
                        return LabelName.GetFullName(xMethod) == LabelName.GetFullName(yMethod);
                    }
                    else if (x is Type xType && y is Type yType)
                    {
                        return LabelName.GetFullName(xType) == LabelName.GetFullName(yType);
                    }
                    else
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public int GetHashCode(MemberInfo aItem)
        {
            return (aItem.ToString() + GetDeclareTypeString(aItem)).GetHashCode();
        }

        private static string GetDeclareTypeString(MemberInfo item)
        {
            var xName = item.DeclaringType;
            return xName == null ? String.Empty : xName.ToString();
        }
    }
}
