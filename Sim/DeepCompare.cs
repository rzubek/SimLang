// Copyright (C) SomaSim LLC and Robert Zubek

using System;
using System.Collections;
using System.Reflection;

namespace SomaSim.SimLang
{
    /// <summary>
    /// Compares two objects for deep equality, ie. making sure all of their fields 
    /// are either the same primitive values, or if they are collections, that the 
    /// collections are of the same type, have same number of members, and that 
    /// their elements are deeply equal to each other.
    /// 
    /// Detailed rules for equality testing of any two objects:
    /// 
    /// - First, the two objects are checked for standard equality (using the == operator).
    ///   This will catch primitive types, value types, and two instances of the same reference type.
    ///   
    /// - If that fails, if they're instances of the same class (but not collections - see below), 
    ///   they will be reflected, and their members will be tested pairwise for deep equality.
    ///   
    ///   Note that only public member fields and properties will be compared, so that implementation 
    ///   details can remain hidden and not affect testing.
    ///   
    /// - Objects which are collections are checked as follows:
    ///   - Two collections must have the same number of elements and be of the exact same type.
    ///   - IDictionary: must have the same number of keys, and each key must produce a pair of 
    ///     values from both collections which are deep equal.
    ///     (Note that keys are not tested for deep equality, only standard equality.)
    ///   - Arrays, IList, and any other IEnumerable: they will be iterated in order, 
    ///     and each pair of elements produced by the iterators must be deep equal.
    ///     
    /// </summary>
    public static class DeepCompare
    {
        public static bool DeepEquals<T> (T a, T b) where T : class
            => DeepEquals((object) a, b);

        public static bool DeepEquals (object a, object b) {
            // if one is null, see if both are
            if (a == null || b == null) { return a == b; }

            var at = a.GetType();
            var bt = b.GetType();

            // see if they're different types, if so they're never equal.
            // (we don't care if they're interconvertible)
            if (at != bt) { return false; }

            if (at.IsValueType) {
                return ((ValueType) a).Equals(b);

            } else if (a is string str) {
                return str.Equals(b);

            } else if (IsCollection(a)) {
                return EqualsCollections(a, b);

            } else if (IsReferenceType(a)) {
                return EqualsReflected(a, b);

            } else {
                return false;
            }
        }

        private static bool IsCollection (object a) => (a is IEnumerable) || a.GetType().IsArray;

        private static bool IsReferenceType (object a) => a.GetType().IsClass;

        private static bool EqualsReflected (object a, object b) {

            Type t = a.GetType();
            foreach (var member in t.GetMembers(BindingFlags.Public | BindingFlags.Instance)) {
                bool result = true;

                switch (member.MemberType) {
                    case MemberTypes.Field:
                        result = CompareFields((FieldInfo) member, a, b);
                        break;
                    case MemberTypes.Property:
                        result = CompareProperties((PropertyInfo) member, a, b);
                        break;
                }

                if (!result) {
                    return false;
                }
            }

            return true;
        }

        private static bool CompareFields (FieldInfo field, object a, object b) {
            var aval = field.GetValue(a);
            var bval = field.GetValue(b);
            return DeepEquals(aval, bval);
        }

        private static bool CompareProperties (PropertyInfo prop, object a, object b) {
            var aval = prop.GetValue(a, null);
            var bval = prop.GetValue(b, null);
            return DeepEquals(aval, bval);
        }

        private static bool EqualsCollections (object a, object b) {
            if (a is IDictionary dictionary) {
                return CompareDictionaries(dictionary, (IDictionary) b);
            } else if (a.GetType().IsArray) {
                return CompareArrays(a, b);
            } else {
                return CompareIEnumerables((IEnumerable) a, (IEnumerable) b);
            }
        }

        private static bool CompareDictionaries (IDictionary a, IDictionary b) {
            if (a.Count != b.Count) {
                return false;
            }

            foreach (var key in a.Keys) {
                if (!DeepEquals(a[key], b[key])) {
                    return false;
                }
            }

            return true;
        }

        private static bool CompareArrays (object a, object b) {
            Array arra = (Array) a;
            Array arrb = (Array) b;
            return CompareIEnumerables(arra, arrb);
        }

        private static bool CompareIEnumerables (IEnumerable a, IEnumerable b) {
            var ea = a.GetEnumerator();
            var eb = b.GetEnumerator();

            bool hasa, hasb;
            while (true) {
                hasa = ea.MoveNext();
                hasb = eb.MoveNext();
                if (!hasa || !hasb) { break; }
                if (!DeepEquals(ea.Current, eb.Current)) { return false; }
            }

            return (!hasa && !hasb);
        }
    }
}
