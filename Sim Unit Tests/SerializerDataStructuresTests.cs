// Copyright (C) SomaSim LLC and Robert Zubek

using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;

using static SomaSim.SimLang.TestClasses;

namespace SomaSim.SimLang
{
    public class SerializerDataStructureTests
    {
        #region Test Instances

        TestClassOne test1 = new() {
            PublicFieldInt = 42,
            PublicFieldString = "hello, \"world\" ",
            PublicProperty = 1,
            PublicGetterSetter = 2
        };

        TestClassOne test2 = new() {
            PublicFieldString = "hello", // other fields left as defaults
        };

        TestClassGenerics testGenerics = new() {
            ListOfStructs = new List<TestStruct>() {
                new TestStruct() { id = "one", x = 1, y = 1 },
                new TestStruct() { id = "two", x = 2, y = 2 },
            },
            ListOfInterfaces = new List<IClass>() {
                new ClassA() { fielda = 1 },
                new ClassB() { fieldb = "two" }
            },
            ListOfAbstracts = new List<AbstractClass>() {
                new ClassA() { fielda = 1 },
                new ClassB() { fieldb = "two" }
            },
            ListOfConcretes = new List<ClassA>() {
                new ClassA() { fielda = 1 }
            },
            ListOfPrimitives = new List<string>() {
                "foo", "bar", "this is \"cool\" isn't it"
            },
            HashSetOfInts = new HashSet<int>() {
                1, 2, 3, 4
            },
            HasSetOfReferences = new HashSet<ClassA>() {
                new ClassA() { fielda = 1 },
                new ClassA() { fielda = 2 }
            },
            EnumToString = new Dictionary<TestEnum, string>() {
                { TestEnum.Zero, "Zero" },
                { TestEnum.One, "One" },
                { TestEnum.FortyTwo, "FortyTwo" }
            },
            IntToString = new Dictionary<int, string>() {
                { 0, "Zero" }, { 1, "One" } },
            IntToInterface = new Dictionary<int, IClass>() {
                { 1, new ClassA() { fielda = 1 } },
                { 2, new ClassB() { fieldb = "two" } }
            },
            StringToStruct = new Dictionary<string, TestStruct>() {
                { "one", new TestStruct() { id = "one", x = 1, y = 1 } },
                { "two", new TestStruct() { id = "two", x = 2, y = 2 } },
            }
        };

        #endregion

        private static void TestPrintAndParseBack<T> (T value, Serializer s) {
            var serialized = s.Serialize(value);
            var stringified = SimFile.Print(serialized);
            object parsed = SimFile.Parse(stringified);
            T deserialized = s.Deserialize<T>(parsed);

            Assert.IsTrue(DeepCompare.DeepEquals(deserialized, value));
        }

        [Test]
        public void TestIntegrationAndSkippingDefaults () {
            var s = new Serializer();

            TestPrintAndParseBack(test1, s);
            TestPrintAndParseBack(test2, s);

            s.Options.SkipDefaultValuesDuringSerialization = false;
            TestPrintAndParseBack(test2, s);
            s.Options.SkipDefaultValuesDuringSerialization = true;

            TestPrintAndParseBack(testGenerics, s);
        }

        [Test]
        public void TestIntegrationInjectingSpuriousData () {
            string spuriousKey = null;
            var s = new Serializer();
            s.Options.OnSpuriousDataCallback = (key, type) => { spuriousKey = key; };

            Hashtable serialized = s.Serialize(test1) as Hashtable;
            serialized["SomeSpuriousKey"] = "SpuriousValue";
            var stringified = SimFile.Print(serialized);
            object parsed = SimFile.Parse(stringified);
            var deserialized = s.Deserialize<TestClassOne>(parsed);

            Assert.IsTrue(DeepCompare.DeepEquals(deserialized, test1));
            Assert.IsTrue(spuriousKey == "SomeSpuriousKey");
        }

        [Test]
        public void TestIntegrationInvalidType () {
            string unknownType = null;
            var s = new Serializer();
            s.Options.OnUnknownTypeCallback = (type) => { unknownType = type; };

            Hashtable serialized = s.Serialize(test1) as Hashtable;
            serialized[s.Options.SpecialTypeToken] = "$$$blah";
            var stringified = SimFile.Print(serialized);
            object parsed = SimFile.Parse(stringified);
            var deserialized = s.Deserialize(parsed);

            Assert.IsTrue(deserialized is Hashtable);
            Assert.IsTrue(unknownType == "$$$blah");
        }

        [Test]
        public void TestClone () {
            var s = new Serializer();
            Assert.IsTrue(DeepCompare.DeepEquals(s.Clone(test1), test1));
            Assert.IsTrue(DeepCompare.DeepEquals(s.Clone(test2), test2));
            Assert.IsTrue(DeepCompare.DeepEquals(s.Clone(testGenerics), testGenerics));
        }
    }

}
