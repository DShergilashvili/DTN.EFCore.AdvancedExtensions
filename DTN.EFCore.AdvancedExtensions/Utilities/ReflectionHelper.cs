using System.Reflection;

namespace DTN.EFCore.AdvancedExtensions.Utilities
{
    public static class ReflectionHelper
    {
        public static PropertyInfo GetProperty(Type type, string propertyName)
        {
            return type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
        }

        public static object GetPropertyValue(object obj, string propertyName)
        {
            var property = GetProperty(obj.GetType(), propertyName);
            return property?.GetValue(obj);
        }

        public static void SetPropertyValue(object obj, string propertyName, object value)
        {
            var property = GetProperty(obj.GetType(), propertyName);
            property?.SetValue(obj, value);
        }

        public static MethodInfo GetMethod(Type type, string methodName, Type[] parameterTypes)
        {
            return type.GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance, null, parameterTypes, null);
        }

        public static object InvokeMethod(object obj, string methodName, params object[] parameters)
        {
            var method = GetMethod(obj.GetType(), methodName, parameters.Select(p => p.GetType()).ToArray());
            return method?.Invoke(obj, parameters);
        }

        public static Type GetGenericType(Type genericType, params Type[] typeArguments)
        {
            return genericType.MakeGenericType(typeArguments);
        }

        public static bool IsSubclassOfRawGeneric(Type generic, Type toCheck)
        {
            while (toCheck != null && toCheck != typeof(object))
            {
                var cur = toCheck.IsGenericType ? toCheck.GetGenericTypeDefinition() : toCheck;
                if (generic == cur)
                {
                    return true;
                }
                toCheck = toCheck.BaseType;
            }
            return false;
        }
    }
}
