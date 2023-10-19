// Copyright (C) SomaSim LLC and Robert Zubek

using NUnit.Framework;
using System;
using System.Collections;

namespace SomaSim.SimLang
{
    public class SimFileTests
    {
        [Test]
        public void TestSimFileParsePrimitives () {

            // booleans and null

            object t = SimFile.Parse("#true");
            Assert.IsTrue(t is Boolean bt && bt == true);

            object f = SimFile.Parse("#false");
            Assert.IsTrue(f is Boolean bf && bf == false);

            object n = SimFile.Parse("#null");
            Assert.IsTrue(n == null);

            Assert.IsTrue(((string) SimFile.Parse("#unknowntoken")) == "#unknowntoken");

            // numbers

            Assert.IsTrue((double) (SimFile.Parse("0")) == 0);
            Assert.IsTrue((double) (SimFile.Parse("1")) == 1);
            Assert.IsTrue((double) (SimFile.Parse("1.1")) == 1.1);
            Assert.IsTrue((double) (SimFile.Parse("-1")) == -1);
            Assert.IsTrue((double) (SimFile.Parse("+1")) == 1);

            AssertParseFails("+text");
            AssertParseFails("-text");
            AssertParseFails("-+1");
            AssertParseFails(".5");

            // simple strings

            Assert.IsTrue((string) (SimFile.Parse("HelloWorld")) == "HelloWorld");
            Assert.IsTrue((string) (SimFile.Parse("_Hello")) == "_Hello");
            Assert.IsTrue((string) (SimFile.Parse("string_foo-bar")) == "string_foo-bar");
            Assert.IsTrue((string) (SimFile.Parse("a123")) == "a123");

            AssertParseFails("-hello");
            Assert.IsTrue((double) (SimFile.Parse("31337haxxor")) == 31337.0); // two values, first a number, second a string
            Assert.IsTrue((string) (SimFile.Parse("Hello World")) == "Hello"); // these are actually two separate simple strings!

            Assert.IsTrue(SimFile.Parse("s-1") is string);
            Assert.IsTrue((string) SimFile.Parse("s-1") == "s-1");
            Assert.IsTrue(SimFile.Parse("s-10") is string);
            Assert.IsTrue((string) SimFile.Parse("s-10") == "s-10");

            // quoted strings

            // hard to read because we need to convert those strings into C# compatible format first... sigh...
            Assert.IsTrue((string) (SimFile.Parse("\"Hello\"")) == "Hello");
            Assert.IsTrue((string) (SimFile.Parse("\"Hello \\\"Bob\\\"\"")) == "Hello \"Bob\"");
            Assert.IsTrue((string) (SimFile.Parse(@"""c:\\windows""")) == @"c:\windows");
            Assert.IsTrue((string) (SimFile.Parse("'c:\\windows \"test\"'")) == "c:\\windows \"test\"");
            Assert.IsTrue((string) (SimFile.Parse("\"foo\\r\\nbar\"")) == "foo\r\nbar");

            AssertParseFails("\"Hello"); // unclosed quote
            AssertParseFails("\"Hello'"); // closed incorrectly
            Assert.IsTrue((string) (SimFile.Parse("'O\\'Hare'")) == "O\\"); // fail because can't escape inside single-quote
        }

        [Test]
        public void TestSimFileParseCollections () {

            // lists

            AssertEqual(SimFile.Parse("[foo bar \"baz baz\"]"), new ArrayList() { "foo", "bar", "baz baz" });
            AssertEqual(SimFile.Parse(" [foo  #null  [1   2  #true]] "), new ArrayList() { "foo", null, new ArrayList() { 1.0, 2.0, true } });

            AssertParseFails("[ foo bar }"); // not closed

            // dictionaries

            AssertEqual(SimFile.Parse("{name Dennis age 37 old #false}"),
                        new Hashtable() { { "name", "Dennis" }, { "age", 37.0 }, { "old", false } });

            AssertParseFails("{ foo bar ]"); // not closed
            AssertParseFails("{ foo bar baz }"); // missing value for key "baz"

            // lists of dictionaries

            AssertEqual(SimFile.Parse("[{name Dennis age 37} {name \"King Arthur\" age 100}]"),
                new ArrayList() {
                    new Hashtable() { { "name", "Dennis" }, { "age", 37.0 } },
                    new Hashtable() { { "name", "King Arthur" }, { "age", 100.0 } }
                });

            // dictionaries of lists 

            AssertEqual(SimFile.Parse("{a [foo bar baz] b [1 2 3]}"),
                new Hashtable() {
                    { "a", new ArrayList() { "foo", "bar", "baz" } },
                    { "b", new ArrayList() { 1.0, 2.0, 3.0 } }
                });

            // comments

            AssertEqual(SimFile.Parse("{name Dennis ; this is a comment until end of line... \n  age 37 old #false}"),
                        new Hashtable() { { "name", "Dennis" }, { "age", 37.0 }, { "old", false } });


        }

        [Test]
        public void TestSimFilePrintPrimitives () {
            int digits = SimFile.PrintSettings.MaxFloatDoubleDecimalDigits;
            SimFile.PrintSettings.MaxFloatDoubleDecimalDigits = -1;

            Assert.IsTrue(SimFile.Print(null) == "#null");
            Assert.IsTrue(SimFile.Print(true) == "#true");
            Assert.IsTrue(SimFile.Print(false) == "#false");
            Assert.IsTrue(SimFile.Print((double) 42) == "42");
            Assert.IsTrue(SimFile.Print((double) 1.123456) == "1.123456");
            Assert.IsTrue(SimFile.Print((float) 0.0001f) == "0.0001");
            Assert.IsTrue(SimFile.Print((float) 1.0e-20) == "0");
            Assert.IsTrue(SimFile.Print((float) 1.0e10) == "10000000000");
            Assert.IsTrue(SimFile.Print(42) == "42");
            Assert.IsTrue(SimFile.Print(42L) == "42");
            Assert.IsTrue(SimFile.Print("foo-bar_baz-123") == "foo-bar_baz-123"); // simple string
            Assert.IsTrue(SimFile.Print("foo\r\nbar") == "\"foo\\r\\nbar\"");
            Assert.IsTrue(SimFile.Print(@"c:\windows") == "\"c:\\windows\"");
            Assert.IsTrue(SimFile.Print("hello, \"world\"") == "\"hello, \\\"world\\\"\"");

            SimFile.PrintSettings.MaxFloatDoubleDecimalDigits = digits;
        }

        [Test]
        public void TestSimFilePrintMaxFloatDigits () {
            int digits = SimFile.PrintSettings.MaxFloatDoubleDecimalDigits;

            SimFile.PrintSettings.MaxFloatDoubleDecimalDigits = 10;
            Assert.IsTrue(SimFile.Print((double) 1.123456) == "1.123456");
            Assert.IsTrue(SimFile.Print((float) 1.123456f) == "1.123456");

            SimFile.PrintSettings.MaxFloatDoubleDecimalDigits = 5;
            Assert.IsTrue(SimFile.Print((double) 1.123456) == "1.12345");
            Assert.IsTrue(SimFile.Print((float) 1.123456f) == "1.12345");

            SimFile.PrintSettings.MaxFloatDoubleDecimalDigits = 3;
            Assert.IsTrue(SimFile.Print((double) 1.123456) == "1.123");
            Assert.IsTrue(SimFile.Print((float) 1.123456f) == "1.123");

            SimFile.PrintSettings.MaxFloatDoubleDecimalDigits = 1;
            Assert.IsTrue(SimFile.Print((double) 1.123456) == "1.1");
            Assert.IsTrue(SimFile.Print((float) 1.123456f) == "1.1");

            SimFile.PrintSettings.MaxFloatDoubleDecimalDigits = 0;
            Assert.IsTrue(SimFile.Print((double) 1.123456) == "1");
            Assert.IsTrue(SimFile.Print((float) 1.123456f) == "1");

            SimFile.PrintSettings.MaxFloatDoubleDecimalDigits = 3; // make sure we don't lose digits before decimal point
            Assert.IsTrue(SimFile.Print((double) int.MaxValue) == "2147483647");
            SimFile.PrintSettings.MaxFloatDoubleDecimalDigits = 0;
            Assert.IsTrue(SimFile.Print((double) int.MaxValue) == "2147483647");
            SimFile.PrintSettings.MaxFloatDoubleDecimalDigits = -1;
            Assert.IsTrue(SimFile.Print((double) int.MaxValue) == "2147483647");

            SimFile.PrintSettings.MaxFloatDoubleDecimalDigits = digits;
        }

        [Test]
        public void TestSimFilePrintDataScructures () {

            // simple list

            IList list = new ArrayList() { "foo", "bar\n", 1, 2.1, true, null };
            Assert.IsTrue(SimFile.Print(list) == "[ foo \"bar\\n\" 1 2.1 #true #null ]");

            // simple dictionary - alpha sorted on keys

            IDictionary dict = new Hashtable() { { "foo", 1 }, { "bar", 2 } };
            string dictprinted = SimFile.Print(dict, false, true);
            Assert.IsTrue(dictprinted == "{ bar 2 foo 1 }");

            // simple dictionary - alpha unsorted 

            dictprinted = SimFile.Print(dict, false, false);
            Assert.IsTrue(dictprinted == "{ foo 1 bar 2 }" || dictprinted == "{ bar 2 foo 1 }");
        }

        [Test]
        public void TestPrintAndReparse () {

            IList list = new ArrayList() {
                new Hashtable() { { "foo", "bar" }, { "value", 1.0 }, { "visible", true } },
                new Hashtable() { { "names", new ArrayList() { "Alice", "Bob", "Carol" } },
                                  { "values", new ArrayList() { 1.0, 2.0, 3.0 } } }
            };

            string printed = SimFile.Print(list);

            /* printed ==
                 [{
                    foo bar
                    visible #true
                    value 1}
                  {
                    names [Alice Bob Carol]
                    values [1 2 3]}]
             */

            object parsed = SimFile.Parse(printed);

            AssertEqual(list, parsed);
        }

        private static void AssertEqual (object left, object right) {
            if (left == null) {
                Assert.IsTrue(left == right); // trivially true
            } else if (left is ArrayList && right is ArrayList) {
                AssertLists(left as ArrayList, right as ArrayList);
            } else if (left is Hashtable && right is Hashtable) {
                AssertHashtables(left as Hashtable, right as Hashtable);
            } else {
                Assert.IsTrue(left.Equals(right));
            }
        }

        private static void AssertHashtables (Hashtable left, Hashtable right) {
            Assert.IsTrue(left.Count == right.Count);
            var lkeys = left.Keys;
            foreach (var lkey in lkeys) {
                Assert.IsTrue(right.ContainsKey(lkey));
                var lval = left[lkey];
                var rval = right[lkey];
                AssertEqual(lval, rval);
            }
        }

        private static void AssertLists (ArrayList left, ArrayList right) {
            Assert.IsTrue(left.Count == right.Count);
            for (int i = 0; i < left.Count; i++) {
                AssertEqual(left[i], right[i]);
            }
        }

        private static void AssertParseFails (string text) {
            bool caught = false;
            try {
                object results = SimFile.Parse(text);
            } catch (SimException e) {
                caught = (e != null);
            }

            Assert.IsTrue(caught);
        }
    }
}
