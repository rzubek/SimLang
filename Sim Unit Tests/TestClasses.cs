// Copyright (C) SomaSim LLC and Robert Zubek

using System.Collections;
using System.Collections.Generic;

namespace SomaSim.SimLang
{
    public static class TestClasses
    {
        public enum TestEnum
        {
            Zero = 0,
            One = 1,
            FortyTwo = 42
        }

#pragma warning disable CS0414 // The field is assigned but its value is never used
#pragma warning disable IDE0051 // Remove unused private members

        public class TestClassOne
        {
            // these public fields will be serialized
            public int PublicFieldInt = 0;
            public string PublicFieldString = "";

            // these public get/set properties will be serialized
            public int PublicProperty { get; set; }
            public int PublicGetterSetter { get => _PrivateField; set => _PrivateField = value; }

            // these public properties without a public getter or setter will NOT get serialized
            public int MissingSetter => _PrivateField;
            public int PrivateSetter { get; private set; } = 0;
            public int PrivateGetter { private get; set; } = 0;

            // these private properties will NOT be serialized
            private int _PrivateField = 0;
            protected int _ProtectedField = 0;
            int _UnusedField = 0;
        }

#pragma warning restore IDE0051 // Remove unused private members
#pragma warning restore CS0414 // The field is assigned but its value is never used

        public struct TestStruct
        {
            public int x;
            public int y;
            public string id;
        }

        public interface IClass { }

        public abstract class AbstractClass : IClass { }

        public class ClassA : AbstractClass
        {
            public int fielda;
        }

        public class ClassB : AbstractClass
        {
            public string fieldb;
        }

        public class TestClassGenerics
        {
            public Dictionary<int, string> IntToString;
            public Dictionary<TestEnum, string> EnumToString;
            public Dictionary<string, TestStruct> StringToStruct;
            public Dictionary<int, IClass> IntToInterface;

            public List<TestStruct> ListOfStructs;
            public List<IClass> ListOfInterfaces;
            public List<AbstractClass> ListOfAbstracts;
            public List<ClassA> ListOfConcretes;
            public List<string> ListOfPrimitives;

            public HashSet<int> HashSetOfInts;
            public HashSet<ClassA> HasSetOfReferences;
        }

        public class TestClassNonGenerics
        {
            public int[] ArrayOfInts;
            public TestStruct[] ArrayOfStructs;
            public Hashtable Hashtable;
            public ArrayList ArrayList;
        }

        public struct TestLabel
        {
            public string Value;
            public int Hash;

            public TestLabel (string value) {
                Value = value;
                Hash = value.GetHashCode();
            }

            public static object Serialize (TestLabel label, Serializer _) =>
                label.Value;

            public static TestLabel Deserialize (object value, Serializer _) =>
                value is string s ? new TestLabel(s) : new TestLabel();
        }
    }
}
