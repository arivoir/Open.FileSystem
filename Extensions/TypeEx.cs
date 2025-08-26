using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Open.FileSystem
{
    /// <summary>
    /// The <see cref="TypeEx"/> class provides useful extension methods for the <see cref="System.Type"/> class.
    /// </summary>
    public static class TypeEx
    {
        public static IEnumerable<PropertyInfo> GetDefaultProperties(this Type type)
        {
            Type attribType = typeof(DefaultMemberAttribute);
            var typeInfo = type.GetTypeInfo();
            var customAtt = typeInfo.CustomAttributes.FirstOrDefault(ca => ca.AttributeType == attribType);
            if (customAtt != null)
            {
                foreach (var ca in customAtt.ConstructorArguments)
                {
                    var prop = typeInfo.GetDeclaredProperty(ca.Value as string);
                    if (prop != null)
                        yield return prop;
                }
            }
        }
        public static IEnumerable<PropertyInfo> GetIndexerProperties(this Type type)
        {

            foreach (var prop in type.GetRuntimeProperties())
            {
                var parameters = prop.GetIndexParameters();
                if (parameters.Length == 1)
                {
                    yield return prop;
                }
            }
            foreach (var interfaceType in type.GetTypeInfo().ImplementedInterfaces)
            {
                foreach (var prop in interfaceType.GetIndexerProperties())
                {
                    yield return prop;
                }
            }
        }

        public static PropertyInfo GetProperty(this Type type, string name)
        {
            return type.GetRuntimeProperty(name);
        }

        public static MethodInfo GetMethod(this Type type, string name, Type[] parameters)
        {
            return type.GetRuntimeMethod(name, parameters);
        }

        // from Extensions class

        /// <summary>
        /// Creates a new instance of this type using the default constructor.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="type">The type.</param>
        /// <returns>A new instance of type T.</returns>
        public static T New<T>(this Type type)
        {
            ConstructorInfo ci = type.GetDefaultPublicCtor();
            if (ci == null) throw new InvalidOperationException(String.Format("Cannot find a default constructor for type {0}", type.FullName));

            T newInstance = (T)ci.Invoke(new object[] { });
            return newInstance;
        }

        /// <summary>
        /// Creates a new instance of this type using the default constructor.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>A new instance of type T.</returns>
        public static object New(this Type type)
        {
            ConstructorInfo ci = type.GetDefaultPublicCtor();
            return ci == null ? null : ci.Invoke(new object[] { });
        }

        /// <summary>
        /// Returns default public instance parameter-less constructor for the specified type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>The default constructor for the specified type if it exists; Null otherwise.</returns>
        public static ConstructorInfo GetDefaultPublicCtor(this Type type)
        {
            return type.GetTypeInfo().DeclaredConstructors.Where(ctr => ctr.GetParameters().Length == 0 && ctr.Name != ".cctor").FirstOrDefault();
        }

        /// <summary>
        /// Creates a new instance of this type using the default constructor.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="type">The type.</param>
        /// <param name="initializers">The initializers.</param>
        /// <returns>A new instance of type T</returns>
        public static T New<T>(this Type type, Action<T> initializers)
        {
            T newInstance = type.New<T>();
            if (initializers != null)
            {
                initializers(newInstance);
            }
            return newInstance;
        }

        /// <summary>
        /// Returns the underlying type of a nullable type:
        /// e.g. if Type = double?, then returns double.
        /// </summary>
        /// <param name="type">Type.</param>
        /// <returns>The underlying type.</returns>
        public static Type GetNonNullableType(this Type type)
        {
            if (type.IsNullableType())
            {
                return Nullable.GetUnderlyingType(type);
            }
            return type;
        }

        /// <summary>
        /// Returns true if the type is a nullable type.
        /// </summary>
        /// <param name="type">Type.</param>
        /// <returns>True if the type is nullable.</returns>
        public static bool IsNullableType(this Type type)
        {
            return (((type != null) && type.GetTypeInfo().IsGenericType) && (type.GetGenericTypeDefinition() == typeof(Nullable<>)));
        }

        /// <summary>
        /// Returns true if the type is any of the numeric data types:
        /// double, float, int, uint, long, ulong, short, ushort, sbyte, byte and decimal.
        /// </summary>
        /// <param name="type">Type.</param>
        /// <returns>True if the type is numeric.</returns>
        public static bool IsNumeric(this Type type)
        {
            return
                type == typeof(double) || type == typeof(float) ||
                type == typeof(int) || type == typeof(uint) ||
                type == typeof(long) || type == typeof(ulong) ||
                type == typeof(short) || type == typeof(ushort) ||
                type == typeof(sbyte) || type == typeof(byte) ||
                type == typeof(decimal);
        }

        /// <summary>
        /// Returns true if the type is any of the integral numeric data types:
        /// int, uint, long, ulong, short, ushort, sbyte and byte.
        /// </summary>
        /// <param name="type">Type.</param>
        /// <returns>True if the type is an integral numeric.</returns>
        public static bool IsNumericIntegral(this Type type)
        {
            return
                type == typeof(int) || type == typeof(uint) ||
                type == typeof(long) || type == typeof(ulong) ||
                type == typeof(short) || type == typeof(ushort) ||
                type == typeof(sbyte) || type == typeof(byte);
        }

        /// <summary>
        /// Returns true if the type is any of the integral signed numeric data types:
        /// int, long, short and sbyte.
        /// </summary>
        /// <param name="type">Type.</param>
        /// <returns>True if the type is a signed integral numeric.</returns>
        public static bool IsNumericIntegralSigned(this Type type)
        {
            return
                type == typeof(int) ||
                type == typeof(long) ||
                type == typeof(short) ||
                type == typeof(sbyte);
        }

        /// <summary>
        /// Returns true if the type is any of the integral unsigned numeric data types:
        /// uint, ulong, ushort and byte.
        /// </summary>
        /// <param name="type">Type.</param>
        /// <returns>True if the type is an unsigned integral numeric.</returns>
        public static bool IsNumericIntegralUnsigned(this Type type)
        {
            return
                type == typeof(uint) ||
                type == typeof(ulong) ||
                type == typeof(ushort) ||
                type == typeof(byte);
        }

        /// <summary>
        /// Returns true if the type is any of the numeric non-integral data types:
        /// double, float and decimal.
        /// </summary>
        /// <param name="type">Type.</param>
        /// <returns>True if the type is a non-integral numeric.</returns>
        public static bool IsNumericNonIntegral(this Type type)
        {
            return
                type == typeof(double) || type == typeof(float) ||
                type == typeof(decimal);
        }

        /// <summary>
        /// Returns the values of an Enum type (Type.IsEnum == true).
        /// </summary>
        /// <typeparam name="T">Enum type.</typeparam>
        /// <param name="enumType">Enum type.</param>
        /// <returns>The list of values for that enum type.</returns>
        public static IList<T> GetEnumValues<T>(this Type enumType)
        {
            if (enumType == null) throw new ArgumentNullException("enumType");
            if (!enumType.GetTypeInfo().IsEnum) throw new InvalidOperationException(String.Format("{0} should be a an EnumType", enumType.Name));

            List<T> values = new List<T>();
            foreach (FieldInfo fi in enumType.GetRuntimeFields())
            {
                if (!fi.IsSpecialName)
                {
                    T value = (T)fi.GetValue(enumType);
                    if (!values.Contains(value))
                    {
                        values.Add(value);
                    }
                }
            }
            return values;
        }

        public static Type GetItemType(this IEnumerable enumerable)
        {
            if (enumerable == null)
            {
                return typeof(object);
            }
            Type enumerableType = enumerable.GetType();
            Type nonNullableType = null;
            if (IsEnumerableType(enumerableType))
            {
                nonNullableType = GetItemType(enumerableType).GetNonNullableType();
            }
            if ((nonNullableType != null) && (nonNullableType != typeof(object)))
            {
                return nonNullableType;
            }
            var type = enumerable.Cast<object>().Select(delegate (object x)
            {
                return x.GetType();
            }).FirstOrDefault();
            return type ?? nonNullableType ?? typeof(object);
        }

        internal static bool IsEnumerableType(this Type enumerableType)
        {
            return (FindGenericType(typeof(IEnumerable<>), enumerableType) != null);
        }

        internal static bool IsReadOnlyCollection(object instance)
        {
            Type collectionType = FindGenericType(typeof(ICollection<>), instance.GetType());
            if (collectionType != null)
                return (bool)collectionType.GetProperty("IsReadOnly").GetValue(instance, null);
            return false;
        }

        private static Type GetItemType(this Type enumerableType)
        {
            Type type = FindGenericType(typeof(IEnumerable<>), enumerableType);
            if (type != null)
            {
                return type.GetTypeInfo().GenericTypeArguments[0];
            }
            return enumerableType;
        }

        public static Type FindGenericType(this Type definition, Type type)
        {
            while ((type != null) && (type != typeof(object)))
            {
                if (type.GetTypeInfo().IsGenericType && (type.GetGenericTypeDefinition() == definition))
                {
                    return type;
                }
                if (definition.GetTypeInfo().IsInterface)
                {
                    foreach (Type interfaceType in type.GetTypeInfo().ImplementedInterfaces)
                    {
                        Type genericType = FindGenericType(definition, interfaceType);
                        if (genericType != null)
                        {
                            return genericType;
                        }
                    }
                }
                type = type.GetTypeInfo().BaseType;
            }
            return null;
        }

        #region lists reflection
#if !UWP
        public static IEnumerable<PropertyInfo> GetProperties(this Type type)
        {
            if (type != null && !DataTypeIsPrimitive(type))
            {
                var dataProperties = type.GetTypeInfo().DeclaredProperties.Where(
                    p =>
                    {
                        try
                        {
                            MethodInfo mi = p.GetMethod;
                            return (mi != null && mi.Attributes.HasFlag(MethodAttributes.Static)) ? false : p.GetIndexParameters().Length == 0;
                        }
                        catch
                        {
                            return true;
                        }
                    }).ToArray();
                return dataProperties;
            }
            return new PropertyInfo[0];
        }
#endif
        public static bool DataTypeIsPrimitive(this Type dataType)
        {
            if (dataType == null)
            {
                return false;
            }
            if ((!dataType.GetTypeInfo().IsPrimitive && (dataType != typeof(string))) && (dataType != typeof(DateTime)))
            {
                return (dataType == typeof(decimal));
            }
            return true;
        }

        #endregion
    }
}
