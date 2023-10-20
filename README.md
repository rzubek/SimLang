# SimLang - sim file markup language

**SimLang** is a serialization language by SomaSim. We use it to serialize all 
configuration files and save game files in all our simulation and strategy games.

**SimLang** is intentionally similar to JSON, but easier for humans to read
and easier to write out by hand. Specifically:

- It's optimized to be human-readable and -writable
    - Minimal punctuation and reduced visual noise
    - As similar to Lisp s-expressions as possible
- Easy de/serialization into C# class instances
- Easy to read "like English" if the classes are set up correctly

We've been using it in production since 2014.


## Example 

Very quick example of **SimLang** syntax:

```
{    
    ;; this is a sample game character
    ;;
    #type   "Person"
    id      person-123
    info    { name "Bob the Wizard" age 100 }
    stats   { wisdom 20 strength 3 dexterity 12 }
    inventory [
        { id item-234 name "Magic Staff" magical #true }
        { id item-345 name Bow }
        { id item-456 name Quiver contents [
            { id item-567 name Arrows quantity 3 }
        ] }
        { id item-678 name "Pointy Hat" }
    ]
    inuse {
        head item-678
        righthand item-234
    }
}
```

Some highlights:

- Compared to JSON, there are no extraneous double-quotes, colons, or commas.
  A JSON object like `{ "name": "Bob", "age": 100 }` is represented 
  more compactly as `{ name Bob age 100 }`. 
- Strings are not double quoted if not necessary, if they only contain 
  ASCII characters and dashes/underscores. So, `Bob` instead of `"Bob"`.
  This greatly improves human readability - and writeability.
- Comments are supported

The **SimLang** library can then deserialize this into a plain C# class instance (POCO),
or serialize POCOs back out to text files.

***Note:*** SimLang files have the extension `.sim` and
we refer to them in this document as _sim files_.



## Design

**SimLang** design goals and constraints, in no particular order:

- **Human friendly**
  - Removes visual noise, such as unnecessary quotation of short strings
  - Removes spurious delimiters between collection elements which 
    are prone to merge conflicts, typos, etc.
  - Allows for comments in the data file
  - Whitespace is used as a token separator, but otherwise it's not meaningful 
    (unlike in some other markup languages - looking at you, yaml and make!)
  - Key/value collections are alpha-sorted by default to help with merging
    (but this can be turned off if desired, e.g. for very large data)

- **Clear and simple semantics**
  - Supports similar value types as JSON
    - primitives (numbers, strings, booleans)
    - lists of values
    - maps from strings to values
  - Special syntax for null values which are supported
  - Special syntax for boolean values so that can't be mistaken for strings
  - Allows for explicit type annotation when needed

- **Fast**
  - Parsing a sim file into data structures must be possible with a single pass over the character stream,
    without backtracking or an overly complex state machine
  - Value serialized into a sim file can have only one canonical representation

- **Extensible**
  - Users can specify custom de/serializers for chosen data types
  - Users can intercept the data stream in the middle of serialization, e.g.
    to modify or to send to a different printing backend
  - Users can add custom _reader macros_, e.g. to send text to an external
    language interpreter before it's parsed (we use it to embed Lisp expressions
    that get evaluated at parse time)

**SimLang** design borrows a lot from S-expressions in Scheme and Lisp, but presents a
more modern syntax that has special forms for maps vs arrays.


### Why?

Our games are strongly data-driven. Game configuration gets loaded up from data files,
which are created and maintained by human designers - entirely by hand. 

We used JSON in the past, and it worked... fine. Not great. JSON is simple and 
ubiquitous, which is good. But some of the rough edges make it particularly 
annoying for human writers who have to write it by hand: things like having to 
double-quote even the shortest strings everywhere, having to add commas and colons 
everywhere and make sure there's the right number of them, lack of comments,
or merge problems caused by stray commas in lists of objects. 

**SimLang** is a data serialization format with a syntax designed to be human-friendly, 
while maintaining the simplicity of JSON. 

We've now used it since 2014, and shipped several commercial titles with it.


# Detailed Examples

Some common primitive value types - here's how they look serialized:
```
    Strings: 
        "I'm a string", thisIsAStringToo, another_string, also-a-string
    Numbers: 
        42, -1, +1, 3.14159
    Booleans: 
        #true, #false
    Null: 
        #null
```
This list contains four primitive values, and another embedded list:
```
    [ some-string 42 #true #null 
        [ "I'm inside a second list" ] ]
```
This dictionary maps from string names, to lists that contain numbers:
```
    { 
      "John Smith" [ 312 555 1212 ]
      "Jane Doe" [ 415 555 1212 ]
    }
```
Strings including escaped characters, or using single-quotes for convenience:
```
    "This is how you add a quote in a string: \"Hello\" "
    "To escape the escape character, double it: \\"
    "This is the first line \n ... and this is the second line"
    'Single-quote syntax disables escaping, eg. "c:\windows\system"'
```
Whitespace is used to separate values, but otherwise not significant:
```
    [this is a list of    seven     strings]
    {    name "Dennis" age 37}
```
Comments start with ; and go until EOL
```
    [here is a  ; this is comment that goes to the end of the line
     list of strings]
```
Longer example of a made-up data object
```
    {
        ;; this is a sample sprite definition
        identity { template npc }
        navigation {
            animations {
                directions [ left right up down ]
                sprites [ "walk-l" "walk-r" "stand-l" "stand-r" ]
            }
            path '\images\npc'
        }
        placement {
            size { x 1 y 1 }
            snap #false
            center #true
        }
    }
```

# Technical details

## Syntax Definition

This is a BNF-ish description of the syntax:

```
    Compound values:

    <document>        :: <ignored>* <value> <ignored>*
    <value>           :: <primitive> | <list> | <dictionary> | <macro>
    <list>            :: "[" <ignored>* <value>? (<ignored>+ <value>)* <ignored>* "]"
    <dictionary>      :: "{" <ignored>* <entry>? (<ignored>+ <entry>)* <ignored>* "}"
    <entry>           :: <value> <ignored>+ <value>
    <ignored>         :: <comment> | <whitespace>
    <macro>           :: "(" <macro-char>+ ")"
    <comment>         :: ";" and all subsequent characters up to <newline>

    Primitive values:

    <primitive>       :: <null> | <boolean> | <number> | <string>
    <null>            :: "#null"
    <boolean>         :: "#true" | "#false"
    <number>          :: ("+" | "-")? <digit>+ ("." <digit>+)?
    <string>          :: <verbatim-string> | <escaped-string> | <simple-string>
    <verbatim-string> :: "\'" <verbatim-char>* "\'"
    <escaped-string>  :: "\"" (<escaped-char> | <basic-char>)* "\""
    <simple-string>   :: <simple-char> (<simple-char> | <digit> | "-" | "." )*

    Tokenization elements:

    <digit>           :: "0" | .. | "9"
    <simple-char>     :: "a" | .. | "z" | "A" .. "Z" | "_"
    <verbatim-char>   :: utf8 character except "\'" or control character
    <escaped-char>    :: "\n" | "\r"
    <basic-char>      :: utf8 character except "\\", "\"", or control character
    <macro-char>      :: utf8 character except "("
    <whitespace>      :: white space characters and all control codes, i.e. 0x00 - 0x20
    <not-newline>     :: utf8 character except for newline characters
```

## Serialization Example 

**Note: there's a full serialization example under [Sim Unit Tests/SerializerDemo.cs](Sim%20Unit%20Tests/SerializerDemo.cs).**

Example **SimLang** serializer output for the sample game character from the introduction:

```
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
```

This example will naturally serialize from / deserialize into the following classes:

```csharp
public class Person {
    public string id;
    public Info info;
    public Dictionary<Stat, int> stats;
    public List<Entry> inventory;
    public Dictionary<string, string> inuse;

    public class Info { 
        public string name; 
        public int age; 
    } 

    public enum Stat {
        Wisdom, Strength, Dexterity
    }

    public class Entry { 
        public string id;
        public string name; 
        public int quantity = 1; 
        public bool magical = false;
        public List<Entry> contents;
    }
}
```

What this example shows:
- Most strings are serialized without double quotes, unless they contain spaces or other special characters
- Dictionaries and classes are serialized using curly braces, and they don't use colons or commas, they're separated with whitespace
- Lists and arrays are serialized using square braces, and don't use colons, they're separated with whitespace
- Enums are supported as either names or values
- All classes and data structures can be arbitrarily nested, as long as they're trees (so no cycles)
- One-line comments are supported, they start after the first unescaped ";" token 


### C# API

Serialization:

```csharp
   var Bob = new Person { ......... };
   var simfile = SimFile.Serialize(Bob);
```

Deserialization:

```csharp
    var newBob = SimFile.Deserialize<Person>(simfile);

    // is it the same Bob? yes, yes it is
    Assert.IsTrue(DeepCompare.DeepEquals(Bob, newBob));
```


## Comparison with JSON

Here is the verbatim output as SimLang file (note that keys are alphabetically sorted by default):

```
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
```
 
Compare to output as JSON:

```
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
    "inuse": {
        "head": "item-678",
        "righthand": "item-234"
    }
}
```

## Serialization Details

### Two-stage serialization / deserialization

All de/serialization happens in two stages, going through an intermediate representation (IR).

For example, serialization:
1. Serialize strongly typed instance into IR
2. Print IR as text

Similarly, deserialization:
1. Parse text into IR
2. Deserialize IR into a strongly typed class instance

Or schematically:

```
   Plain C# object  --(serialize)--->   IR   --(print)->   Sim file

   Plain C# object  <-(deserialize)--   IR   <-(parse)--   Sim file
```

The IR is an untyped collection of primitives. It allows only the following types: 
**String, U/Int64, Double, ArrayList, Hashtable.** The primitive types are the
widest available for each category, and data structures are untyped.

Once the string is parsed into those types, the serializer's job is to
convert hashtables to class instances (or dictionaries), untyped array lists to 
strongly typed collections, and wide primitives into appropriate specific ones.

This has several benefits:
- The job of tokenizing/parsing/printing is different from the job of reflection-based serialization,
  so this allows for much cleaner implementation
- This lets us swap out front-ends, and use e.g. JSON printer instead of a **SimLang** printer if desired
  (which in fact we did on one project)

On the other hand, this means the serializer is not a streaming serializer, meaning
that it has to allocate the entire parse tree in memory.
However, nothing about the language design prevents a streaming serializer implementation in the future.


### Custom serialization handlers

To extend the serializer to process custom types, you can add custom serializer/deserializer 
functions under `SerializationSettings.CustomSerializers`/`Deserializers`.

These will be called whenever accessing an object of that type.


### Type information

As you can see above, type information is inferred from class member definition,
and if there is no ambiguity, class name will not be serialized out, or needed during deserialization.

However in some cases you need type information, for example when dealing with a generic list
where the generic type is an interface, or an ArrayList which is completely untyped.

In this case, the serializer will write out type name, e.g. `{ #type "Person" ... }`.
Namespace information may be included or omitted, based on adding or removing
that namespace from `SerializationSettings.ImplicitNamespaces`.

### Reader macros

Text inside raw parentheses, e.g. `{ strength (* 2 base-strength) }` is a reader macro
and during the _parsing_ step it will be sent over to the registered macro processor.
The processor should then transform this expression into a new value, which will
replace the old one and parsing will resume.

If a macro processor has not been registered, a macro will result in a syntax error  
since expressions of the form `( ... )` are not part of the grammar.



## Limitations

### Limitations around public / protected / private access modifiers

As a design principle, only public information gets serialized.
For classes, this means fully-public fields and properties.

Specifically the following **do** get serialized:
- All public, writable class fields
- All properties which have a public setter and a public getter

While the following **do not** get serialized:
- Private, protected class fields
- Readonly fields
- Properties without a public getter
- Properties without a public setter


### Limitations around collections

In the current implementation, the following collections are supported by the serializer:

```
Non-generic:
- T[] array (e.g. int[], MyClass[])
- ArrayList
- Hashtable

Generic:
- List<T>
- Dictionary<K,V>
- HashSet<T>
(Including recursively, e.g. Dictionary<K, List<T>>)
```

For generic types, the serializer _should_ work with all `ICollection<T>` types, 
but hasn't been extensively tested because there's a lot of them. :)

While other collection types may be added in the future, users can also use
the custom serialization functionality to provide their own in the meantime.

### Other limitations

Some known issues:
- Anonymous tuples fail during serialization, e.g. `List<(string, string)>`.
  It is recommended to convert those into named tuple structs first.
- Circular references are neither supported nor detected.
  Attempts to serialize data structures with circular references
  will result in stack overflow.
