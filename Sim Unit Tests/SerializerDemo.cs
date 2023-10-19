// Copyright (C) SomaSim LLC and Robert Zubek

using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace SomaSim.SimLang
{
    /// <summary>
    /// This class shows a simple example of serializing and deserializing a strongly typed class
    /// that contains a list, a dictionary, and other types
    /// </summary>
    public class SerializerDemo
    {
        public class Person
        {
            public string id;
            public Info info;
            public Dictionary<Stat, int> stats;
            public List<Entry> inventory;
            public Dictionary<string, string> inuse;

            public class Info
            {
                public string name;
                public int age;
            }

            public enum Stat
            {
                Wisdom, Strength, Dexterity
            }

            public class Entry
            {
                public string id;
                public string name;
                public int quantity = 1;
                public bool magical = false;
                public List<Entry> contents;
            }
        }

        public Person Bob = new() {
            id = "person-123",
            info = new() {
                name = "Bob the Wizard",
                age = 100
            },
            stats = new() {
                { Person.Stat.Wisdom, 30 },
                { Person.Stat.Strength, 3 },
                { Person.Stat.Dexterity, 12 }
            },
            inventory = new() {
                new() { id = "item-234", name = "Magic Staff", magical = true },
                new() { id = "item-345", name = "Bow" },
                new() { id = "item-456", name = "Quiver",
                    contents = new() { new() { id = "item-567", name = "Arrows", quantity = 3 } } },
                new() { id = "item-678", name = "Pointy Hat" }
            },
            inuse = new() {
                { "head", "item-678" },
                { "righthand", "item-234" }
            }
        };


        [Test]
        public void TestSerializerDemo () {

            var s = new Serializer();
            s.Options.EnumSerialization = EnumSerializationOption.SerializeAsSimpleName;
            s.Options.SkipDefaultValuesDuringSerialization = true;

            var simfile = SimFile.Serialize(Bob);

            // let's see what that looks like!
            Console.WriteLine(simfile);

            /* the result is as follows (notice that whitespace is compressed, 
             * strings are simplified, and default values are skipped, for efficiency):

            { 
              id person-123
              info { age 100 name "Bob the Wizard" }
              inuse { head item-678 righthand item-234 }
              inventory [ 
                { id item-234 magical #true name "Magic Staff" }
                { id item-345 name Bow }
                { 
                  contents [ { id item-567 name Arrows quantity 3 } ]
                  id item-456
                  name Quiver
                }
                { id item-678 name "Pointy Hat" }
              ]
              stats { dexterity 12 strength 3 wisdom 30 }
            }
            */

            // and turn it back into a strongly typed class instance
            var newBob = SimFile.Deserialize<Person>(simfile);

            // is it the same Bob? yes, yes it is
            Assert.IsTrue(DeepCompare.DeepEquals(Bob, newBob));

            // and how does it compare with JSON?
            var jsonbob = JsonSerializer.Serialize(Bob, new JsonSerializerOptions() {
                WriteIndented = true,
                IncludeFields = true,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingDefault
            });

            Console.WriteLine(jsonbob);

            /* this produces the considerably more verbose and noisy output:

            {
                "id": "person-123",
                "info": {
                    "name": "Bob the Wizard",
                    "age": 100
                },
                "stats": {
                    "Wisdom": 30,
                    "Strength": 3,
                    "Dexterity": 12
                },
                "inventory": [
                {
                    "id": "item-234",
                    "name": "Magic Staff",
                    "quantity": 1,
                    "magical": true
                },
                {
                    "id": "item-345",
                    "name": "Bow",
                    "quantity": 1
                },
                {
                    "id": "item-456",
                    "name": "Quiver",
                    "quantity": 1,
                    "contents": [
                    {
                        "id": "item-567",
                        "name": "Arrows",
                        "quantity": 3
                    }
                    ]
                },
                {
                    "id": "item-678",
                    "name": "Pointy Hat",
                    "quantity": 1
                }
                ],
                "equipped": {
                    "head": "item-678",
                    "righthand": "item-234"
                }
            }

            */
        }
    }

}
