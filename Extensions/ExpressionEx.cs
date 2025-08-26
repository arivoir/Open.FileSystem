using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Open.FileSystemAsync
{
    public static class ExpressionEx
    {

        public static string GetPropertyPath<T, P>(Expression<Func<T, P>> expression)
        {
            // Working outside in e.g. given p.Spouse.Name - the first node will be Name, then Spouse, then p
            IList<string> propertyNames = new List<string>();
            var currentNode = expression.Body;
            while (currentNode.NodeType != ExpressionType.Parameter)
            {
                switch (currentNode.NodeType)
                {
                    case ExpressionType.MemberAccess:
                    case ExpressionType.Convert:
                        MemberExpression memberExpression;
                        memberExpression = (currentNode.NodeType == ExpressionType.MemberAccess ? (MemberExpression)currentNode : (MemberExpression)((UnaryExpression)currentNode).Operand);
                        if (!(memberExpression.Member is PropertyInfo ||
                             memberExpression.Member is FieldInfo))
                        {
                            throw new InvalidOperationException("The member '" + memberExpression.Member.Name + "' is not a field or property");
                        }
                        propertyNames.Add(memberExpression.Member.Name);
                        currentNode = memberExpression.Expression;
                        break;
                    case ExpressionType.Call:
                        MethodCallExpression methodCallExpression = (MethodCallExpression)currentNode;
                        if (methodCallExpression.Method.Name == "get_Item")
                        {
                            propertyNames.Add("[" + methodCallExpression.Arguments.First().ToString() + "]");
                            currentNode = methodCallExpression.Object;
                        }
                        else
                        {
                            throw new InvalidOperationException("The member '" + methodCallExpression.Method.Name + "' is a method call but a Property or Field was expected.");
                        }
                        break;

                    // To include method calls, remove the exception and uncomment the following three lines:
                    //propertyNames.Add(methodCallExpression.Method.Name);
                    //currentExpression = methodCallExpression.Object;
                    //break;
                    default:
                        throw new InvalidOperationException("The expression NodeType '" + currentNode.NodeType.ToString() + "' is not supported, expected MemberAccess, Convert, or Call.");
                }
            }
            return string.Join(".", propertyNames.Reverse().ToArray());
        }

        public static Expression GetPropertyPathExpression(Expression param, string propertyPath)
        {
            if (string.IsNullOrWhiteSpace(propertyPath))
                return param;

            Expression propertyAccess = param;
            var groups = ObjectEx.PropertyPathRegEx.Matches(propertyPath);
            if (groups.Count > 1)
            {
                var firstGroup = groups[0];
                var firstProp = firstGroup.Value;
                var restExp = propertyPath.Substring(firstGroup.Index + firstGroup.Length);
                if (restExp.StartsWith("."))
                    restExp = restExp.Substring(1);
                propertyAccess = GetPropertyPathExpression(GetPropertyPathExpression(propertyAccess, firstProp), restExp);
            }
            else if (propertyPath.Contains("["))
            {
                // get property name
                string propertyName = propertyPath.Substring(0, propertyPath.IndexOf('['));
                // get index value
                object indexValue = propertyPath.Substring(propertyPath.IndexOf('[') + 1, propertyPath.IndexOf(']') - propertyPath.IndexOf('[') - 1);
                // we try numeric indexes first
                var pInfo = ObjectEx.GetIndexerProperty(param.Type, propertyName, ref indexValue);
                if (pInfo != null)
                {
                    propertyAccess = Expression.Call(propertyAccess, pInfo.GetMethod, Expression.Constant(indexValue));
                }
                else
                {
                    propertyAccess = GetPropertyPathExpression(Expression.Property(propertyAccess, propertyName), propertyPath.Substring(propertyPath.IndexOf('['), propertyPath.IndexOf(']') - propertyPath.IndexOf('[') + 1));
                }
            }
            else
            {
                propertyAccess = Expression.Property(propertyAccess, propertyPath);
            }
            return propertyAccess;
        }

        public static Expression Convert(Expression exp, Type type)
        {
            Expression safeExp = exp;
            if (exp.Type != type)
            {
                try
                {
                    if (type.IsNullableType() && type.GetNonNullableType() == exp.Type)
                    {
                        safeExp = Expression.Convert(safeExp, type);
                    }
                    else
                    {
                        // do the conversion
                        MethodInfo convertMethod = typeof(Convert).GetMethod("ChangeType", new Type[] { typeof(object), typeof(Type), typeof(IFormatProvider) });
                        safeExp = Expression.Call(convertMethod, new Expression[] { exp, Expression.Constant(type, typeof(Type)), Expression.Constant(null, typeof(IFormatProvider)) });
                        safeExp = Expression.Convert(safeExp, type);
                    }
                }
                catch { }
            }

            return safeExp;
        }

        public static bool CanAssign(this Expression exp)
        {
            var member = exp as System.Linq.Expressions.MemberExpression;
            if (member != null)
            {
                var prop = member.Member as PropertyInfo;
                if (prop != null)
                {
                    return prop.CanWrite;
                }
            }
            return false;
        }
    }
}
