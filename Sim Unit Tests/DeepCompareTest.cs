// Copyright (C) SomaSim LLC and Robert Zubek

using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;

using static SomaSim.SimLang.DeepCompare;

namespace SomaSim.SimLang
{
    public class DeepCompareTest
    {
        internal enum TestEnum
        {
            Zero = 0,
            One = 1,
        }

        internal struct TestStruct
        {
            public int i;
            public string s;
        }

        internal class SimpleTestClass
        {
            public int ifield;
            public string sfield;
            public SimpleTestClass next;
            public TestEnum en = TestEnum.Zero;

            public string Strprop { get => _spriv; set => _spriv = value; }
            private string _spriv;
        }

        internal class LargeTestClass
        {
            public int ifield;
            public List<int> ilist;
            public int[] iarray;
            public ArrayList arraylist;

            public Dictionary<int, string> dict;
            public Hashtable hash;
        }

        [Test]
        public void TestComparePrimitives () {
            Assert.IsTrue(DeepEquals(1, 1));
            Assert.IsTrue(DeepEquals(1f, 1f));
            Assert.IsTrue(DeepEquals("1", "1"));
            Assert.IsTrue(DeepEquals('1', '1'));
            Assert.IsTrue(DeepEquals(true, true));
            Assert.IsTrue(DeepEquals(null, null));

            Assert.IsFalse(DeepEquals(1, 2));
            Assert.IsFalse(DeepEquals(1, 1f));
            Assert.IsFalse(DeepEquals("1", '1'));
            Assert.IsFalse(DeepEquals('1', 1));
            Assert.IsFalse(DeepEquals(true, false));
            Assert.IsFalse(DeepEquals(true, null));
        }

        [Test]
        public void TestCompareSimpleClass () {

            var a = new SimpleTestClass() { ifield = 1, sfield = "2", Strprop = "3", en = TestEnum.One, next = new SimpleTestClass() { ifield = 4 } };
            var b = new SimpleTestClass() { ifield = 1, sfield = "2", Strprop = "3", en = TestEnum.One, next = new SimpleTestClass() { ifield = 4 } };

            Assert.IsTrue(DeepEquals(a, a));
            Assert.IsTrue(DeepEquals(a, b));

            var bad1 = new SimpleTestClass() { ifield = 1, sfield = "2", Strprop = "3", next = new SimpleTestClass() { ifield = 0 } };
            var bad2 = new SimpleTestClass() { ifield = 1, sfield = "2", Strprop = "3" };
            var bad3 = new SimpleTestClass() { ifield = 1, sfield = "2", next = new SimpleTestClass() { ifield = 0 } };
            var bad4 = new SimpleTestClass() { ifield = 1, sfield = "2", Strprop = "3", en = TestEnum.One, next = new SimpleTestClass() { ifield = 0 } };

            Assert.IsFalse(DeepEquals(a, bad1));
            Assert.IsFalse(DeepEquals(a, bad2));
            Assert.IsFalse(DeepEquals(a, bad3));
            Assert.IsFalse(DeepEquals(a, bad4));
            Assert.IsFalse(DeepEquals(a, null));
        }

        [Test]
        public void TestCompareArrays () {
            int[] a = new int[] { 1, 2, 3 };

            Assert.IsTrue(DeepEquals(a, new int[] { 1, 2, 3 }));

            Assert.IsFalse(DeepEquals(a, new int[] { 1, 2 }));
            Assert.IsFalse(DeepEquals(a, new int[] { 1, 2, 3, 4 }));
            Assert.IsFalse(DeepEquals(a, new List<int>() { 1, 2, 3 }));
            Assert.IsFalse(DeepEquals(a, null));
        }

        [Test]
        public void TestCompareIEnumerables () {
            var a = new List<int> { 1, 2, 3 };

            Assert.IsTrue(DeepEquals(a, new List<int> { 1, 2, 3 }));

            Assert.IsFalse(DeepEquals(a, new List<int> { 1, 2 }));
            Assert.IsFalse(DeepEquals(a, new List<int> { 1, 2, 3, 4 }));
            Assert.IsFalse(DeepEquals(a, new int[] { 1, 2, 3 }));
            Assert.IsFalse(DeepEquals(a, new ArrayList() { 1, 2, 3 }));
            Assert.IsFalse(DeepEquals(a, null));


            var b = new ArrayList() { 1, 2, 3 };

            Assert.IsTrue(DeepEquals(b, new ArrayList() { 1, 2, 3 }));

            Assert.IsFalse(DeepEquals(b, new List<int> { 1, 2, 3 }));


            var c = new ArrayList() { 1, "1", new char[] { 'a', 'b' }, null };

            Assert.IsTrue(DeepEquals(c, new ArrayList() { 1, "1", new char[] { 'a', 'b' }, null }));

            Assert.IsFalse(DeepEquals(c, new ArrayList() { 1, "1", new char[] { 'a' }, null }));
            Assert.IsFalse(DeepEquals(c, new ArrayList() { 1, "1", new ArrayList() { 'a', 'b' }, null }));
        }

        [Test]
        public void TestCompareIDictionaries () {
            var a = new Dictionary<string, int>() { { "one", 1 }, { "two", 2 } };

            Assert.IsTrue(DeepEquals(a, new() { { "one", 1 }, { "two", 2 } }));

            Assert.IsFalse(DeepEquals(a, new() { { "one", 1 } }));
            Assert.IsFalse(DeepEquals(a, new() { { "one", 1 }, { "two", 2 }, { "three", 3 } }));
            Assert.IsFalse(DeepEquals(a, new() { { "one", 1 }, { "foo", 2 } }));


            var b = new Dictionary<string, List<int>>() { { "one", new List<int>() { 1 } }, { "two", new List<int>() { 2 } } };

            Assert.IsTrue(DeepEquals(b, new Dictionary<string, List<int>>() { { "one", new List<int>() { 1 } }, { "two", new List<int>() { 2 } } }));

            Assert.IsFalse(DeepEquals(b, new Dictionary<string, List<int>>() { { "one", new List<int>() { 0 } }, { "two", new List<int>() { 2 } } }));
            Assert.IsFalse(DeepEquals(b, new Dictionary<string, List<int>>() { { "one", new List<int>() { 1 } }, { "foo", new List<int>() { 2 } } }));
            Assert.IsFalse(DeepEquals(a, b));

        }

        [Test]
        public void TestCompareLargeClass () {
            static LargeTestClass factory () {
                return new LargeTestClass() {
                    ifield = 42,
                    ilist = new List<int>() { 1, 2, 3 },
                    arraylist = new ArrayList() { 'a', 'b', 'c' },
                    iarray = new int[] { 0, 1, 2 },
                    hash = new Hashtable() { { "foo", "bar" }, { 1, 2 }, { "test", new TestStruct() { i = 1, s = "one" } } },
                    dict = new Dictionary<int, string>() { { 1, "one" }, { 2, "two" } }
                };
            }

            var a = factory();

            Assert.IsTrue(DeepEquals(a, factory()));

            var bad = factory();
            bad.ilist[2] = 42;
            Assert.IsFalse(DeepEquals(a, bad));

            bad = factory();
            bad.hash["test"] = new TestStruct() { i = 42, s = "fortytwo" };
            Assert.IsFalse(DeepEquals(a, bad));

        }
    }
}
