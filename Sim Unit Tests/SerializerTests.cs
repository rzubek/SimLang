// Copyright (C) SomaSim LLC and Robert Zubek

using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;

using static SomaSim.SimLang.TestClasses;

namespace SomaSim.SimLang
{
    public class SerializerTests
    {
        private static Serializer s;

        static SerializerTests () {
            s = new Serializer();
        }

        private static void CheckSerializedTypeAndValue (object source, Type expectedType, object expectedValue) {
            object serialized = s.Serialize(source);
            Assert.IsTrue(expectedValue == null ? serialized == null : expectedValue.Equals(serialized));
            Assert.IsTrue(expectedType == serialized.GetType());
        }

        [Test]
        public void TestSerializeNumbers () {
            // make sure all integers are upcast to 64-bit long or ulong
            CheckSerializedTypeAndValue((byte) 42, typeof(ulong), 42UL);
            CheckSerializedTypeAndValue((ushort) 42, typeof(ulong), 42UL);
            CheckSerializedTypeAndValue((uint) 42, typeof(ulong), 42UL);
            CheckSerializedTypeAndValue((ulong) 42, typeof(ulong), 42UL);
            CheckSerializedTypeAndValue((sbyte) 42, typeof(long), 42L);
            CheckSerializedTypeAndValue((short) 42, typeof(long), 42L);
            CheckSerializedTypeAndValue((int) 42, typeof(long), 42L);
            CheckSerializedTypeAndValue((long) 42, typeof(long), 42L);

            // make sure all floating points are upcast to double precision
            CheckSerializedTypeAndValue(1.0f, typeof(double), 1.0);
            CheckSerializedTypeAndValue(1.0, typeof(double), 1.0);
        }

        [Test]
        public void TestSerializeOtherPrimitives () {
            // null be null
            Assert.IsNull(s.Serialize(null));

            // booleans are serialized as is
            CheckSerializedTypeAndValue(true, typeof(bool), true);
            CheckSerializedTypeAndValue(false, typeof(bool), false);

            // characters and strings are serialized as strings
            CheckSerializedTypeAndValue("foo", typeof(string), "foo");
            CheckSerializedTypeAndValue('x', typeof(string), "x");

            // enums are serialized as uint64, or as a string, depending on settings
            var previousSetting = s.Options.EnumSerialization;

            s.Options.EnumSerialization = EnumSerializationOption.SerializeAsNumber;
            CheckSerializedTypeAndValue(TestEnum.Zero, typeof(long), 0L);
            CheckSerializedTypeAndValue(TestEnum.One, typeof(long), 1L);
            CheckSerializedTypeAndValue(TestEnum.FortyTwo, typeof(long), 42L);

            s.Options.EnumSerialization = EnumSerializationOption.SerializeAsSimpleName;
            CheckSerializedTypeAndValue(TestEnum.Zero, typeof(string), "zero");
            CheckSerializedTypeAndValue(TestEnum.One, typeof(string), "one");
            CheckSerializedTypeAndValue(TestEnum.FortyTwo, typeof(string), "fortytwo");

            s.Options.EnumSerialization = previousSetting;

            // date time are converted to their binary representation (specially encoded int64)
            var time = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            long binary = time.ToBinary();
            CheckSerializedTypeAndValue(time, typeof(long), binary);
        }

        [Test]
        public void TestSerializeClassInstances () {

            // try serialize everything

            bool previous = s.Options.SkipDefaultValuesDuringSerialization;
            s.Options.SkipDefaultValuesDuringSerialization = false;
            {
                object source = new TestClassOne() { };
                object expected = new Hashtable() {
                    { "#type", "SomaSim.SimLang.TestClasses+TestClassOne" },
                    { "PublicFieldInt", 0L },
                    { "PublicFieldString", "" },
                    { "PublicGetterSetter", 0L },
                    { "PublicProperty", 0L }
                };

                Assert.IsTrue(DeepCompare.DeepEquals(s.Serialize(source, true), expected));
                s.Options.SkipDefaultValuesDuringSerialization = previous;
            }

            // try serialize just non-default fields

            s.Options.SkipDefaultValuesDuringSerialization = true;
            {
                // all fields are set, all will be serialized 

                object source = new TestClassOne() {
                    PublicFieldInt = 42,
                    PublicFieldString = "fortytwo",
                    PublicGetterSetter = 42,
                    PublicProperty = 42
                };
                object expected = new Hashtable() {
                    { "#type", "SomaSim.SimLang.TestClasses+TestClassOne" },
                    { "PublicFieldInt", 42L }, { "PublicFieldString", "fortytwo" }, { "PublicGetterSetter", 42L }, { "PublicProperty", 42L }
                };

                Assert.IsTrue(DeepCompare.DeepEquals(s.Serialize(source, true), expected));
            }

            {
                // some fields are not set, they will be skipped

                object source = new TestClassOne() {
                    PublicGetterSetter = 42,
                    PublicProperty = 42
                };
                object expected = new Hashtable() {
                    { "#type", "SomaSim.SimLang.TestClasses+TestClassOne" },
                    { "PublicGetterSetter", 42L }, { "PublicProperty", 42L }
                };

                Assert.IsTrue(DeepCompare.DeepEquals(s.Serialize(source, true), expected));
            }

            s.Options.SkipDefaultValuesDuringSerialization = previous;
        }

        [Test]
        public void TestSerializeDictionaryOfPrimitives () {

            // test generic / typed

            {
                var source = new Dictionary<string, int>() {
                    { "foo", 1 },
                    { "bar", 2 } };
                var expected = new Hashtable() {
                    { "foo", 1L },
                    { "bar", 2L } }; // because serializer converts up to longs
                Assert.IsTrue(DeepCompare.DeepEquals(s.Serialize(source), expected));
            }

            {
                var source = new Dictionary<sbyte, List<uint>>() {
                    { 1, new List<uint>() { 1, 2 } },
                    { 3, new List<uint>() { 3, 4 } } };
                var expected = new Hashtable() {
                    { 1L, new ArrayList() { 1UL, 2UL } },
                    { 3L, new ArrayList() { 3UL, 4UL } } };
                Assert.IsTrue(DeepCompare.DeepEquals(s.Serialize(source), expected));
            }

            // test untyped

            {
                var source = new Hashtable() {
                    { "foo", new ArrayList() { 1, (sbyte)2 } },
                    { 'x', new ArrayList() { (uint)3, (ushort)4 } } };
                var expected = new Hashtable() {
                    { "foo", new ArrayList() { 1L, 2L } },
                    { "x", new ArrayList() { 3UL, 4UL } } };
                Assert.IsTrue(DeepCompare.DeepEquals(s.Serialize(source), expected));
            }

            {
                var source = new TestClassNonGenerics() {
                    Hashtable = new Hashtable() { { "one", 1 } }
                };
                var expected = new Hashtable() {
                    { "Hashtable", new Hashtable() { { "one", 1L } } } };
                Assert.IsTrue(DeepCompare.DeepEquals(s.Serialize(source), expected));
            }
        }

        [Test]
        public void TestSerializeDictionaryOfClassInstances () {
            {
                var source = new TestClassGenerics() {
                    IntToString = new Dictionary<int, string>() { { 1, "one" }, { 2, "two" } },
                    EnumToString = new Dictionary<TestEnum, string>() {
                        { TestEnum.FortyTwo, "fortytwo" },
                        { TestEnum.One, "one" } },
                    StringToStruct = new Dictionary<string, TestStruct>() {
                        { "testone", new TestStruct() { id = "one", x = 1, y = 1 } },
                        { "testtwo", new TestStruct() { id = "two", x = 2, y = 2 } }
                    },
                    IntToInterface = new Dictionary<int, IClass>() {
                        { 1, new ClassA() { fielda = 1 } },
                        { 2, new ClassB() { fieldb = "two" } }
                    }
                };

                var expected = new Hashtable() {
                    { "IntToString", new Hashtable() { { 1L, "one" }, { 2L, "two" } } },
                    { "EnumToString", new Hashtable() { { 42L, "fortytwo" }, { 1L, "one" } } },
                    { "StringToStruct", new Hashtable() {
                        { "testone", new Hashtable() { { "id", "one" }, { "x", 1L }, { "y", 1L } } },
                        { "testtwo", new Hashtable() { { "id", "two" }, { "x", 2L }, { "y", 2L } } }
                    } },
                    { "IntToInterface", new Hashtable() {
                        { 1L, new Hashtable() { { "#type", "SomaSim.SimLang.TestClasses+ClassA" }, { "fielda", 1L } } },
                        { 2L, new Hashtable() { { "#type", "SomaSim.SimLang.TestClasses+ClassB" }, { "fieldb", "two" } } }
                    } }
                };

                Assert.IsTrue(DeepCompare.DeepEquals(s.Serialize(source), expected));
            }
        }

        [Test]
        public void TestSerializeEnumerableOfPrimitives () {

            // test generic / typed

            {
                var source = new short[] { 1, 2, 3 };
                var expected = new ArrayList() { 1L, 2L, 3L };
                Assert.IsTrue(DeepCompare.DeepEquals(s.Serialize(source), expected));
            }

            {
                var source = new List<short>() { 1, 2, 3 };
                var expected = new ArrayList() { 1L, 2L, 3L };
                Assert.IsTrue(DeepCompare.DeepEquals(s.Serialize(source), expected));
            }

            {
                var source = new List<char[]>() { new char[] { 'f', 'o', 'o' } };
                var expected = new ArrayList() { new ArrayList() { "f", "o", "o" } };
                Assert.IsTrue(DeepCompare.DeepEquals(s.Serialize(source), expected));
            }

            // test untyped

            {
                var source = new ArrayList() { "foo", 'x', 123, (byte) 42, new List<bool>() { true, false } };
                var expected = new ArrayList() { "foo", "x", 123L, 42UL, new ArrayList() { true, false } };
                Assert.IsTrue(DeepCompare.DeepEquals(s.Serialize(source), expected));
            }

            {
                var source = new TestClassNonGenerics() {
                    ArrayList = new ArrayList() { "one", 1 },
                    Hashtable = new Hashtable() { { 1L, "one" } },
                    ArrayOfInts = new int[] { 1, 2 },
                    ArrayOfStructs = new TestStruct[] {
                        new TestStruct() { id = "one", x = 1, y = 1 },
                        new TestStruct() { id = "two", x = 2, y = 2 },
                    },
                };
                var expected = new Hashtable() {
                    { "ArrayList", new ArrayList() { "one", 1L } },
                    { "Hashtable", new Hashtable() { { 1L, "one" } } },
                    { "ArrayOfInts", new ArrayList() { 1L, 2L } },
                    { "ArrayOfStructs", new ArrayList() {
                        new Hashtable() { { "id", "one" }, { "x", 1L }, { "y", 1L } },
                        new Hashtable() { { "id", "two" }, { "x", 2L }, { "y", 2L } },
                    }
                    }
                };
                Assert.IsTrue(DeepCompare.DeepEquals(s.Serialize(source), expected));
            }
        }

        [Test]
        public void TestSerializeEnumerableOfClassInstances () {

            {
                var source = new TestClassGenerics() {
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
                        "foo", "bar"
                    }
                };

                var expected = new Hashtable() {
                    { "ListOfStructs", new ArrayList() {
                        new Hashtable() { { "id", "one" }, { "x", 1L }, { "y", 1L } },
                        new Hashtable() { { "id", "two" }, { "x", 2L }, { "y", 2L } }
                    } },
                    { "ListOfInterfaces", new ArrayList() {
                        new Hashtable() { { "#type", "SomaSim.SimLang.TestClasses+ClassA" }, { "fielda", 1L } },
                        new Hashtable() { { "#type", "SomaSim.SimLang.TestClasses+ClassB" }, { "fieldb", "two" } }
                    } },
                    { "ListOfAbstracts", new ArrayList() {
                        new Hashtable() { { "#type", "SomaSim.SimLang.TestClasses+ClassA" }, { "fielda", 1L } },
                        new Hashtable() { { "#type", "SomaSim.SimLang.TestClasses+ClassB" }, { "fieldb", "two" } }
                    } },
                    { "ListOfConcretes", new ArrayList() {
                        new Hashtable() { { "fielda", 1L } }
                    } },
                    { "ListOfPrimitives", new ArrayList() { "foo", "bar" } }
                };

                Assert.IsTrue(DeepCompare.DeepEquals(s.Serialize(source), expected));
            }
        }

        [Test]
        public void TestSerializeWithImplicitNamespaces () {

            var source = new TestClassGenerics() {
                ListOfInterfaces = new List<IClass>() {
                    new ClassA() { fielda = 1 },
                    new ClassB() { fieldb = "two" }
                }
            };

            var resultExplicit = new Hashtable() {
                { "ListOfInterfaces", new ArrayList() {
                    new Hashtable() { { "#type", "SomaSim.SimLang.TestClasses+ClassA" }, { "fielda", 1L } },
                    new Hashtable() { { "#type", "SomaSim.SimLang.TestClasses+ClassB" }, { "fieldb", "two" } }
                } } };

            var resultImplicit = new Hashtable() {
                { "ListOfInterfaces", new ArrayList() {
                    new Hashtable() { { "#type", "ClassA" }, { "fielda", 1L } },
                    new Hashtable() { { "#type", "ClassB" }, { "fieldb", "two" } }
                } } };

            Assert.IsTrue(DeepCompare.DeepEquals(s.Serialize(source), resultExplicit));

            s.AddImplicitNamespace("SomaSim.SimLang.TestClasses", false);
            Assert.IsTrue(DeepCompare.DeepEquals(s.Serialize(source), resultImplicit));

            s.RemoveImplicitNamespace("SomaSim.SimLang.TestClasses");
            Assert.IsTrue(DeepCompare.DeepEquals(s.Serialize(source), resultExplicit));
        }



        //
        // test deserialization

        private static void CheckDeserializedTypeAndValue (object source, Type desiredType, Type expectedType, object expectedValue) {
            object deserialized = s.Deserialize(source, desiredType);
            Assert.IsTrue(expectedValue == null ? deserialized == null : expectedValue.Equals(deserialized));
            Assert.IsTrue(expectedType == deserialized.GetType());
        }

        [Test]
        public void TestDeserializeNumbers () {
            // check deserialization of plain primitives with casting
            CheckDeserializedTypeAndValue(42UL, typeof(ulong), typeof(ulong), 42UL);
            CheckDeserializedTypeAndValue(42UL, typeof(uint), typeof(uint), 42U);
            CheckDeserializedTypeAndValue(42UL, typeof(ushort), typeof(ushort), (ushort) 42);
            CheckDeserializedTypeAndValue(42UL, typeof(byte), typeof(byte), (byte) 42);

            CheckDeserializedTypeAndValue(42L, typeof(long), typeof(long), 42L);
            CheckDeserializedTypeAndValue(42L, typeof(int), typeof(int), 42);
            CheckDeserializedTypeAndValue(42L, typeof(short), typeof(short), (short) 42);
            CheckDeserializedTypeAndValue(42L, typeof(sbyte), typeof(sbyte), (sbyte) 42);

            CheckDeserializedTypeAndValue(42.0, typeof(float), typeof(float), 42.0f);
            CheckDeserializedTypeAndValue(42.0, typeof(double), typeof(double), 42.0);
        }

        [Test]
        public void TestDeserializeOtherPrimitives () {
            // null is null
            Assert.IsNull(s.Deserialize(null, null));

            // make sure plain primitives come through as is when not forced into a type
            CheckDeserializedTypeAndValue((ulong) 42, null, typeof(ulong), 42UL);
            CheckDeserializedTypeAndValue((long) 42, null, typeof(long), 42L);
            CheckDeserializedTypeAndValue((double) 42, null, typeof(double), 42.0);
            CheckDeserializedTypeAndValue(true, null, typeof(bool), true);
            CheckDeserializedTypeAndValue("foo", null, typeof(string), "foo");

            // booleans are serialized as is
            CheckDeserializedTypeAndValue(true, typeof(bool), typeof(bool), true);
            CheckDeserializedTypeAndValue(false, typeof(bool), typeof(bool), false);

            // characters and strings as serialized as strings
            CheckDeserializedTypeAndValue("x", typeof(char), typeof(char), 'x');
            CheckDeserializedTypeAndValue("x", typeof(string), typeof(string), "x");
            CheckDeserializedTypeAndValue("x", null, typeof(string), "x");

            // enums should deserialize correctly from either number or string
            CheckDeserializedTypeAndValue(42L, typeof(TestEnum), typeof(TestEnum), TestEnum.FortyTwo);
            CheckDeserializedTypeAndValue(42UL, typeof(TestEnum), typeof(TestEnum), TestEnum.FortyTwo);
            CheckDeserializedTypeAndValue("fortytwo", typeof(TestEnum), typeof(TestEnum), TestEnum.FortyTwo);
            CheckDeserializedTypeAndValue("FortyTwo", typeof(TestEnum), typeof(TestEnum), TestEnum.FortyTwo);
            CheckDeserializedTypeAndValue("forty-two", typeof(TestEnum), typeof(TestEnum), TestEnum.FortyTwo);
            CheckDeserializedTypeAndValue("f-o-rty-t-w-o", typeof(TestEnum), typeof(TestEnum), TestEnum.FortyTwo);
        }


        [Test]
        public void TestDeserializeClassInstances () {

            {
                // try deserialize with type embedded in the source object

                object source = new Hashtable() {
                    { "#type", "SomaSim.SimLang.TestClasses+TestClassOne" },
                    { "PublicFieldInt", 42L },
                    { "PublicFieldString", "fortytwo" },
                    { "PublicGetterSetter", 42L },
                    { "PublicProperty", 42L }
                };

                object expected = new TestClassOne() {
                    PublicFieldInt = 42,
                    PublicFieldString = "fortytwo",
                    PublicGetterSetter = 42,
                    PublicProperty = 42
                };

                Assert.IsTrue(DeepCompare.DeepEquals(s.Deserialize(source), expected));
            }

            {
                // try deserialize with type specified explicitly

                object source = new Hashtable() {
                    { "PublicFieldInt", 42L },
                    { "PublicFieldString", "fortytwo" },
                    { "PublicGetterSetter", 42L },
                    { "PublicProperty", 42L }
                };

                object expected = new TestClassOne() {
                    PublicFieldInt = 42,
                    PublicFieldString = "fortytwo",
                    PublicGetterSetter = 42,
                    PublicProperty = 42
                };

                Assert.IsTrue(DeepCompare.DeepEquals(s.Deserialize<TestClassOne>(source), expected));
            }
        }


        [Test]
        public void TestDeserializeDictionaryOfPrimitives () {

            // test generic / typed

            {
                var source = new Hashtable() {
                    { "foo", 1L },
                    { "bar", 2L } };
                var expected = new Dictionary<string, int>() {
                    { "foo", 1 },
                    { "bar", 2 } };
                Assert.IsTrue(DeepCompare.DeepEquals(s.Deserialize(source, expected.GetType()), expected));
            }

            {
                var source = new Hashtable() {
                    { 1L, new ArrayList() { 1UL, 2UL } },
                    { 3L, new ArrayList() { 3UL, 4UL } } };
                var expected = new Dictionary<sbyte, List<uint>>() {
                    { 1, new List<uint>() { 1, 2 } },
                    { 3, new List<uint>() { 3, 4 } } };
                Assert.IsTrue(DeepCompare.DeepEquals(s.Deserialize(source, expected.GetType()), expected));
            }

            // test untyped

            {
                var source = new Hashtable() {
                    { "foo", new ArrayList() { 1L, 2L } },
                    { "x", new ArrayList() { 3UL, 4UL } } };
                var expected = new Hashtable() {
                    { "foo", new ArrayList() { 1L, 2L } },
                    { "x", new ArrayList() { 3UL, 4UL } } };
                Assert.IsTrue(DeepCompare.DeepEquals(s.Deserialize(source), expected)); // no expected type specified
            }

            {
                var source = new Hashtable() {
                    { "Hashtable", new Hashtable() { { "one", 1L } } } };
                var expected = new TestClassNonGenerics() {
                    Hashtable = new Hashtable() { { "one", 1L } }
                };
                Assert.IsTrue(DeepCompare.DeepEquals(s.Deserialize(source, expected.GetType()), expected));
            }
        }

        [Test]
        public void TestDeserializeDictionaryOfClassInstances () {

            {
                var source = new Hashtable() {
                    { "IntToString", new Hashtable() { { 1L, "one" }, { 2L, "two" } } },
                    { "EnumToString", new Hashtable() { { 42L, "fortytwo" }, { 1L, "one" } } },
                    { "StringToStruct", new Hashtable() {
                        { "testone", new Hashtable() { { "id", "one" }, { "x", 1L }, { "y", 1L } } },
                        { "testtwo", new Hashtable() { { "id", "two" }, { "x", 2L }, { "y", 2L } } }
                    } }
                };

                var expected = new TestClassGenerics() {
                    IntToString = new Dictionary<int, string>() {
                        { 1, "one" },
                        { 2, "two" } },
                    EnumToString = new Dictionary<TestEnum, string>() {
                        { TestEnum.FortyTwo, "fortytwo" },
                        { TestEnum.One, "one" } },
                    StringToStruct = new Dictionary<string, TestStruct>() {
                        { "testone", new TestStruct() { id = "one", x = 1, y = 1 } },
                        { "testtwo", new TestStruct() { id = "two", x = 2, y = 2 } }
                    }
                };

                Assert.IsTrue(DeepCompare.DeepEquals(s.Deserialize<TestClassGenerics>(source), expected));
            }
        }

        [Test]
        public void TestDeserializeEnumerableOfPrimitives () {

            // test generic / typed

            {
                var source = new ArrayList() { 1L, 2L, 3L };
                var expected = new short[] { 1, 2, 3 };
                Assert.IsTrue(DeepCompare.DeepEquals(s.Deserialize(source, expected.GetType()), expected));
            }

            {
                var source = new ArrayList() { 1L, 2L, 3L };
                var expected = new List<short>() { 1, 2, 3 };
                Assert.IsTrue(DeepCompare.DeepEquals(s.Deserialize(source, expected.GetType()), expected));
            }

            {
                var source = new ArrayList() { new ArrayList() { "f", "o", "o" } };
                var expected = new List<char[]>() { new char[] { 'f', 'o', 'o' } };
                Assert.IsTrue(DeepCompare.DeepEquals(s.Deserialize(source, expected.GetType()), expected));
            }

            // test untyped

            {
                var source = new ArrayList() { "foo", "x", 123L, 42UL, new ArrayList() { true, false } };
                var expected = new ArrayList() { "foo", "x", 123L, 42UL, new ArrayList() { true, false } };
                Assert.IsTrue(DeepCompare.DeepEquals(s.Deserialize(source), expected)); // no expected type specified
            }

            {
                var source = new Hashtable() {
                    { "ArrayList", new ArrayList() { "one", 1L } },
                    { "Hashtable", new Hashtable() { { 1L, "one" } } },
                    { "ArrayOfInts", new ArrayList() { 1L, 2L } },
                    { "ArrayOfStructs", new ArrayList() {
                        new Hashtable() { { "id", "one" }, { "x", 1L }, { "y", 1L } },
                        new Hashtable() { { "id", "two" }, { "x", 2L }, { "y", 2L } },
                    } }
                };
                var expected = new TestClassNonGenerics() {
                    Hashtable = new Hashtable() { { 1L, "one" } },
                    ArrayList = new ArrayList() { "one", 1L },
                    ArrayOfInts = new int[] { 1, 2 },
                    ArrayOfStructs = new TestStruct[] {
                        new TestStruct() { id = "one", x = 1, y = 1 },
                        new TestStruct() { id = "two", x = 2, y = 2 },
                    }
                };
                Assert.IsTrue(DeepCompare.DeepEquals(s.Deserialize(source, expected.GetType()), expected));
            }
        }

        [Test]
        public void TestDeserializeEnumerableOfClassInstances () {
            {
                var source = new Hashtable() {
                    { "ListOfStructs", new ArrayList() {
                        new Hashtable() { { "id", "one" }, { "x", 1L }, { "y", 1L } },
                        new Hashtable() { { "id", "two" }, { "x", 2L }, { "y", 2L } }
                    } },
                    { "ListOfInterfaces", new ArrayList() {
                        new Hashtable() { { "#type", "SomaSim.SimLang.TestClasses+ClassA" }, { "fielda", 1L } },
                        new Hashtable() { { "#type", "SomaSim.SimLang.TestClasses+ClassB" }, { "fieldb", "two" } }
                    } },
                    { "ListOfAbstracts", new ArrayList() {
                        new Hashtable() { { "#type", "SomaSim.SimLang.TestClasses+ClassA" }, { "fielda", 1L } },
                        new Hashtable() { { "#type", "SomaSim.SimLang.TestClasses+ClassB" }, { "fieldb", "two" } }
                    } },
                    { "ListOfConcretes", new ArrayList() {
                        new Hashtable() { { "fielda", 1L } }
                    } }
                };

                var expected = new TestClassGenerics() {
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
                    }
                };

                Assert.IsTrue(DeepCompare.DeepEquals(s.Deserialize<TestClassGenerics>(source), expected));
            }
        }

        [Test]
        public void TestDeserializeWithImplicitNamespaces () {
            var sourceExplicit = new Hashtable() {
                { "ListOfInterfaces", new ArrayList() {
                    new Hashtable() { { "#type", "SomaSim.SimLang.TestClasses+ClassA" }, { "fielda", 1L } },
                    new Hashtable() { { "#type", "SomaSim.SimLang.TestClasses+ClassB" }, { "fieldb", "two" } }
                } },
            };

            var sourceImplicit = new Hashtable() {
                { "ListOfInterfaces", new ArrayList() {
                    new Hashtable() { { "#type", "ClassA" }, { "fielda", 1L } },
                    new Hashtable() { { "#type", "ClassB" }, { "fieldb", "two" } }
                } },
            };

            var expected = new TestClassGenerics() {
                ListOfInterfaces = new List<IClass>() {
                    new ClassA() { fielda = 1 },
                    new ClassB() { fieldb = "two" }
                },
            };

            Assert.IsTrue(DeepCompare.DeepEquals(s.Deserialize<TestClassGenerics>(sourceExplicit), expected));

            s.AddImplicitNamespace("SomaSim.SimLang.TestClasses", false);
            Assert.IsTrue(DeepCompare.DeepEquals(s.Deserialize<TestClassGenerics>(sourceImplicit), expected));

            s.RemoveImplicitNamespace("SomaSim.SimLang.TestClasses");
            Assert.IsTrue(DeepCompare.DeepEquals(s.Deserialize<TestClassGenerics>(sourceExplicit), expected));
        }

        [Test]
        public void TestTypedLabelDeserialize () {

            var s = new Serializer();

            s.AddCustomSerializer(TestLabel.Serialize, TestLabel.Deserialize);

            var orig = new TestLabel("foo");
            var serialized = s.Serialize(orig);
            Assert.IsTrue(serialized is string @string && @string == "foo");

            var foo1 = s.Deserialize<TestLabel>("foo");
            Assert.IsNotNull(foo1);
            Assert.IsTrue(foo1.Value == "foo");
            Assert.IsTrue(foo1.Hash == "foo".GetHashCode());

            var foo2 = s.Deserialize<TestLabel>("foo");
            Assert.IsTrue(foo1.Equals(foo2));        // they evaluate to the same struct
            Assert.IsTrue(foo1.Value == foo2.Value); // because they have the same string and
            Assert.IsTrue(foo1.Hash == foo2.Hash);   // they have the same hash. however,

            var bar1 = s.Deserialize<TestLabel>("bar");
            Assert.IsFalse(foo1.Equals(bar1));        // these are not the same,
            Assert.IsFalse(foo1.Value == bar1.Value); // either by string value
            Assert.IsFalse(foo1.Hash == bar1.Hash);   // or by hash

            s.RemoveCustomSerializer<TestLabel>();

            bar1.Hash = foo1.Hash;                   // let's hack our label instance and show that
            Assert.IsTrue(foo1.Hash == bar1.Hash);   // even though the hashes are the same (to simulate a hash collision)
            Assert.IsFalse(foo1.Value == bar1.Value); // the labels are still not equal, although now the test takes longer
        }
    }
}
