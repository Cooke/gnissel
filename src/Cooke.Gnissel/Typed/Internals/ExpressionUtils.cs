using System.Linq.Expressions;
using System.Reflection;

namespace Cooke.Gnissel.Typed.Internals;

public static class ExpressionUtils
{
    public static IReadOnlyCollection<MemberInfo> GetMemberChain(Expression outerMemberExpression)
    {
        return GetMemberInfoChain(outerMemberExpression).ToArray();

        IEnumerable<MemberInfo> GetMemberInfoChain(Expression expression)
        {
            switch (expression)
            {
                case MemberExpression memberExpression:
                    foreach (var member in GetMemberInfoChain(memberExpression.Expression!))
                    {
                        yield return member;
                    }
                    yield return memberExpression.Member;
                    break;
                case ParameterExpression _:
                    break;
                default:
                    throw new NotSupportedException();
            }
        }
    }
}
