using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Open.FileSystemAsync
{
    public static class ObjectEx
    {
        public static readonly string PropertyPathRegExPattern = @"\w*\[[-{}.\w\s]+\]|\[[-{}.\w\s]+\]|\w+";
        public static readonly Regex PropertyPathRegEx = new Regex(PropertyPathRegExPattern);

        public static T GetPropertyValue<T>(this object target, string path)
        {
            if (target == null)
                return default(T);
            else if (string.IsNullOrEmpty(path))
            {
                if (target is T)
                    return (T)target;
                else
                    return default(T);
            }

            object propertyValue = GetPropertyValue(target, path);


            if (propertyValue == null)
                return default(T);
            return (T)propertyValue;
        }

        /// <summary>
        /// Gets the value of a property or property path of the specified object.
        /// </summary>
        /// <remarks>
        /// This extension DON'T evaluate ICustomTypeDescriptor interface. 
        /// Use generic extension with the same name if you are evaluating the path 
        /// of a DataTable or any object that implements ICustomTypeDescriptor.
        /// </remarks>
        /// <param name="target">The target object.</param>
        /// <param name="path">The name of the property or the property path.</param>
        /// <returns>The value contained in the property</returns>
        private static object GetPropertyValue(this object target, string path)
        {
            if (target == null)
                return null;
            var targetType = target.GetType();
            if (string.IsNullOrEmpty(path))
            {
                var defaultProperty = targetType.GetDefaultProperties().FirstOrDefault();
                if (defaultProperty != null)
                {
                    return defaultProperty.GetValue(target);
                }
                return target;
            }

            var groups = PropertyPathRegEx.Matches(path);
            if (groups.Count > 1)
            {
                var firstGroup = groups[0];
                var firstProp = firstGroup.Value;
                var propValue = GetPropertyValue(target, firstProp);
                var restExp = path.Substring(firstGroup.Index + firstGroup.Length);
                if (restExp.StartsWith("."))
                    restExp = restExp.Substring(1);
                return GetPropertyValue(propValue, restExp);
            }
            else if (path.Contains("["))
            {
                // get property name
                string propertyName = path.Substring(0, path.IndexOf('['));
                // get index value
                object indexValue = path.Substring(path.IndexOf('[') + 1, path.IndexOf(']') - path.IndexOf('[') - 1);
                var pInfo = GetIndexerProperty(targetType, propertyName, ref indexValue);
                if (pInfo != null)
                {
                    return pInfo.GetValue(target, new object[] { indexValue });
                }
                else
                {
                    object propValue = GetPropertyValue(target, propertyName);
                    return GetPropertyValue(propValue, path.Substring(path.IndexOf('['), path.IndexOf(']') - path.IndexOf('[') + 1));
                }
            }
            else
            {
                PropertyInfo pInfo = targetType.GetProperty(path);
                if (pInfo == null)
                    return null;
                return pInfo.GetValue(target, null);
            }
        }

        /// <summary>
        /// Sets the value of a property or property path of the specified object.
        /// </summary>
        /// <typeparam name="T">Type of the property to set.</typeparam>
        /// <param name="target">Object that contains the property.</param>
        /// <param name="path">Name or path of the property that contains the value.</param>
        /// <param name="value">New value for the property.</param>
        internal static void SetPropertyValue<T>(this object target, string path, T value)
        {
            if (target == null)
                return;
            var targetType = target.GetType();
            if (string.IsNullOrEmpty(path))
            {
                var defaultProperty = targetType.GetDefaultProperties().FirstOrDefault();
                if (defaultProperty != null)
                {
                    SetValue(defaultProperty, target, value, null);
                }
                return;
            }
            var groups = PropertyPathRegEx.Matches(path);
            if (groups.Count > 1)
            {
                var firstGroup = groups[0];
                var firstProp = firstGroup.Value;
                var propValue = GetPropertyValue(target, firstProp);
                var restExp = path.Substring(firstGroup.Index + firstGroup.Length + 1);
                SetPropertyValue(propValue, restExp, value);
            }
            else if (path.Contains("["))
            {
                // get property name
                string propertyName = path.Substring(0, path.IndexOf('['));
                // get index value
                object indexValue = path.Substring(path.IndexOf('[') + 1, path.IndexOf(']') - path.IndexOf('[') - 1);
                var pInfo = GetIndexerProperty(targetType, propertyName, ref indexValue);
                if (pInfo != null)
                {
                    pInfo.SetValue(target, value, new object[] { indexValue });
                }
                else
                {
                    object propValue = GetPropertyValue(target, propertyName);
                    SetPropertyValue(propValue, path.Substring(path.IndexOf('['), path.IndexOf(']') - path.IndexOf('[') + 1), value);
                }
            }
            else
            {
                PropertyInfo pInfo = targetType.GetProperty(path);
                pInfo.SetValue(target, value, null);
            }
        }

        public static PropertyInfo GetIndexerProperty(Type targetType, string propertyName, ref object indexValue)
        {
            foreach (var defaultProperty in targetType.GetIndexerProperties())
            {
                if (!string.IsNullOrWhiteSpace(propertyName) &&
                    defaultProperty.Name != propertyName)
                    continue;

                var parameters = defaultProperty.GetIndexParameters();
                if (parameters.Length > 1)
                    continue;

                var parameterType = parameters[0].ParameterType;
                if (parameterType == typeof(int))
                {
                    //Looks for an int indexer
                    int numIndexValue;
                    if (int.TryParse(indexValue as string, out numIndexValue))
                    {
                        indexValue = numIndexValue;
                        return defaultProperty;
                    }
                }
                if (parameterType == typeof(Guid))
                {
                    //Looks for an Guid indexer
                    Guid guidIndexValue;
                    if (Guid.TryParse(indexValue as string, out guidIndexValue))
                    {
                        indexValue = guidIndexValue;
                        return defaultProperty;
                    }
                }
                //Looks for a string indexer
                if (parameterType == typeof(string))
                {
                    return defaultProperty;
                }
            }
            return null;
        }

        internal static void SetValue(this PropertyInfo propertyInfo, object target, object value, object[] index)
        {
            if (propertyInfo.PropertyType.GetTypeInfo().IsEnum && value is string)
            {
                try
                {
                    value = Enum.Parse(propertyInfo.PropertyType, (string)value, true);
                }
                catch { }
            }
            propertyInfo.SetValue(target, value, index);
        }
    }
}
