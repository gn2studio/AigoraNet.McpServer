using AigoraNet.Common.Filters;
using System.Reflection;

namespace AigoraNet.Common.Extends;

public static class ExtendObject
{
    public static string GetStringValue<T>(this T? enumValue) where T : struct
    {
        string result = String.Empty;

        if (enumValue == null) return result;

        try
        {
            Type type = enumValue.GetType();
            if (type.IsEnum)
            {
                FieldInfo? fi = type.GetField(enumValue.ToString() ?? "");
                if (fi == null) return result;
                StringValueAttribute[]? attrs = fi.GetCustomAttributes(typeof(StringValueAttribute), false) as StringValueAttribute[];
                if (attrs == null) return result;
                if (attrs?.Length > 0)
                {
                    result = attrs[0].Value;
                }
            }
        }
        catch
        {
            result = "";
        }

        return result;
    }

    public static T? GetFromString<T>(this string stringValue) where T : struct, Enum
    {
        if (string.IsNullOrEmpty(stringValue))
            return null;

        try
        {
            var type = typeof(T);
            foreach (var field in type.GetFields())
            {
                var attribute = Attribute.GetCustomAttribute(field, typeof(StringValueAttribute)) as StringValueAttribute;
                if (attribute != null && attribute.Value == stringValue)
                {
                    if (Enum.TryParse<T>(field.Name, out T result))
                    {
                        return result;
                    }
                }
            }
        }
        catch
        {
            return null;
        }

        return null; // 찾지 못하면 null 리턴
    }

    public static List<Tuple<T, T>> toDualList<T>(this IEnumerable<T> array) where T : new()
    {
        var result = new List<Tuple<T, T>>();

        if (array != null && array.Any())
        {
            using (var enumerator = array.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    T first = enumerator.Current;

                    if (enumerator.MoveNext())
                    {
                        T second = enumerator.Current;
                        result.Add(Tuple.Create(first, second));
                    }
                    else
                    {
                        result.Add(Tuple.Create(first, new T()));
                    }
                }
            }
        }

        return result;
    }

    public static List<K> ConvertList<T, K>(this List<T> list) where T : class where K : class, new()
    {
        var result = new List<K>();
        if (list != null && list.Any())
        {
            foreach (var item in list)
            {
                if (item != null)
                {
                    K newItem = new K();
                    foreach (PropertyInfo prop in typeof(T).GetProperties())
                    {
                        PropertyInfo? targetProp = typeof(K).GetProperty(prop.Name);
                        if (targetProp != null && targetProp.CanWrite)
                        {
                            targetProp.SetValue(newItem, prop.GetValue(item));
                        }
                    }
                    result.Add(newItem);
                }
            }
        }
        return result;
    }

    public static K ConvertObject<T, K>(this T data) where T : class where K : class, new()
    {
        var result = new K();
        if (data != null)
        {
            foreach (PropertyInfo prop in typeof(T).GetProperties())
            {
                PropertyInfo? targetProp = typeof(K).GetProperty(prop.Name);
                if (targetProp != null && targetProp.CanWrite)
                {
                    targetProp.SetValue(result, prop.GetValue(data));
                }
            }
        }
        return result;
    }
}
