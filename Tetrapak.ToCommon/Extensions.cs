using System;

namespace Tetrapak.ToCommon
{
    public static class Extensions
    {
        public static int Match(this string s, params string[] values)
        {
            if (string.IsNullOrEmpty(s))
                return -1;

            for (var i = 0; i < values.Length; i++)
            {
                if (s.Equals(values[i], StringComparison.OrdinalIgnoreCase))
                    return i;
            }
            return -1;
        }

        public static void AssignShallowFrom(this object target, object source)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            if (source == null) throw new ArgumentNullException(nameof(source));
            var targetType = target.GetType();
            var sourceType = source.GetType();
            var props = targetType.GetProperties();
            foreach (var targetProp in props)
            {
                if (!targetProp.CanWrite) continue;
                var sourceProp = sourceType.GetProperty(targetProp.Name);
                if (sourceProp == null || !targetProp.PropertyType.IsAssignableFrom(sourceProp.PropertyType)) continue;
                var value = sourceProp.GetValue(source);
                targetProp.SetValue(target, value);
            }
        }
    }
}