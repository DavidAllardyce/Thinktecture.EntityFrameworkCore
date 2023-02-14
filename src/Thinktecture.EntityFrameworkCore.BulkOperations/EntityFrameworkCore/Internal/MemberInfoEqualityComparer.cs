using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Thinktecture.EntityFrameworkCore.Internal;
internal class MemberInfoEqualityComparer : EqualityComparer<MemberInfo>
{
    public override bool Equals(MemberInfo? member, MemberInfo? other)
    {
        if (ReferenceEquals(member, other)) return true;
        if (member is null) return false;
        if (other is null) return false;
        if (member.GetType() != other.GetType()) return false;

        return member.MetadataToken == other.MetadataToken &&
               member.Module.Equals(other.Module) &&
               Equals(member.DeclaringType, other.DeclaringType);
    }

    public override int GetHashCode([DisallowNull] MemberInfo member)
    {
        return HashCode.Combine(member.MetadataToken, member.Module, member.DeclaringType);
    }
}
