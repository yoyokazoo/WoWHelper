using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace WoWHelper.Code.WorldState
{
    // Put on a config field to keep it out of its class's ALL_ list (e.g. to pull a route out of
    // auto-selection/pathfinding without deleting it).
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class ExcludeFromAllAttribute : Attribute { }

    // Builds the ALL_ lists in Config/Data by reflecting over a class's public static fields of
    // type T, in declaration order, instead of hand-maintaining each list.
    public static class StaticConfigCollector
    {
        // Lazy, so it runs after the owning class's static initializers have all finished.
        // Collecting from inside a static field initializer would read later fields as null.
        public static Lazy<IReadOnlyList<T>> LazyCollect<T>(Type owner) where T : class
        {
            return new Lazy<IReadOnlyList<T>>(() => Collect<T>(owner));
        }

        public static IReadOnlyList<T> Collect<T>(Type owner) where T : class
        {
            return owner.GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(field => field.FieldType == typeof(T))
                .Where(field => !field.IsDefined(typeof(ExcludeFromAllAttribute), false))
                // GetFields' order isn't guaranteed; metadata token order is declaration order.
                .OrderBy(field => field.MetadataToken)
                .Select(field => (T)field.GetValue(null))
                .Where(value => value != null)
                .ToList()
                .AsReadOnly();
        }
    }
}
