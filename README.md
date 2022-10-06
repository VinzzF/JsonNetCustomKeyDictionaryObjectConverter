# Advanced Json.NET Converter for Dictionaries
A more complex Json.NET converter that keeps the object representation of a dictionary in JSON format and supports custom key types as well (as long as they can be serialized as plain strings)

# Usage
Include the provided class library or just the class "JsonNetCustomKeyDictionaryObjectConverter/JsonCustomKeyDictionaryObjectConverter.cs" direclty in your project. Then add it as a converter to your Json.NET serializer settings.
Example:
```cs
static void Main(string[] args)
{
   var serializerSettings = new JsonSerializerSettings
   {
       Converters = new List<JsonConverter>
       {
           // Converter goes here:
           new JsonCustomKeyDictionaryObjectConverter()
       }
   };

   using (var sw = new StreamWriter("Test.json"))
       sw.Write(JsonConvert.SerializeObject(new TestObj(), serializerSettings));
}
```

# Motivation
It pains me that this isn't the default behavior. A JSON object is quite the definition of a Dictionary in of itself. Yet, it is not possible to have custom key types in plain Json.NET even if they could be (de-)serialized as plain strings. I understand this is an issue with more complex types, but there is no issue still throwing an exception then, right? The (de-)serialization of custom key types is skipped entirely by dafault for any dictionaries. This project attempts to solve this.

Common suggestion is to use arrays (or array "contracts") instead of plain JSON objects. This defeats the purpose imo.

# Sources
This work is losely based on a few topics on this matter:
- [How can I serialize/deserialize a dictionary with custom keys using Json.Net?](https://stackoverflow.com/a/27043792/2334932)
- [Basic Reading and Writing JSON](https://www.newtonsoft.com/json/help/html/readingwritingjson.htm)
- [C# (CSharp) Newtonsoft.Json JsonReader Examples](https://csharp.hotexamples.com/examples/Newtonsoft.Json/JsonReader/-/php-jsonreader-class-examples.html)

Thanks to the authors!

# Issues
This is work-in-progress.

For one, I have not found a clean solution to invoke the (default) serialization of a plain string directly in the Json writing process. This is needed to serialize the key type as Json object property name. The only somewhat reliable way around this I've found is writing/serializing to a custom TextWriter in the serialization process that is then converted into a plain string which is then written as Json object property. Surely there is a cleaner way around this. Feel free to pull request!

Furthermore custom key types could result in complex Json structures, but must be a plain string as a Json object property. This can and should not work, it is however not validated currently and will result in less intuitive exceptions.
