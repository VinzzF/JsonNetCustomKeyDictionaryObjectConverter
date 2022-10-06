using JsonNetCustomKeyDictionaryObjectConverter;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

// Non-string dictionary key type that serializes from and to a string
class CustomKey
{
    public string Name { get; set; }
}

// Converter from and to string for custom key type
class CustomKeyConverter : JsonConverter<CustomKey>
{
    public override CustomKey ReadJson(JsonReader reader, Type objectType, CustomKey existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        return new CustomKey { Name = (string)reader.Value };
    }

    public override void WriteJson(JsonWriter writer, CustomKey value, JsonSerializer serializer)
    {
        writer.WriteValue(value.Name);
    }
}

// Value type that requires more complex (de-)serialization
class ComplexVal
{
    public string Name { get; set; }

    public int SomeInt { get; set; }

    public string SomeOtherInfo { get; set; }
}

// Test object containing different nested dictionary types
class Obj
{
    public Dictionary<string, string> DictPlain { get; set; } = new Dictionary<string, string>();
    public Dictionary<CustomKey, string> DictSimple { get; set; } = new Dictionary<CustomKey, string>();
    public Dictionary<CustomKey, ComplexVal> DictComplex { get; set; } = new Dictionary<CustomKey, ComplexVal>();
}

class Program
{
    static void Main(string[] args)
    {
        // Test objects
        var obj = new Obj();
        obj.DictPlain.Add("key1", "val1");
        obj.DictPlain.Add("key2", "val2");
        obj.DictPlain.Add("key3", "val3");
        obj.DictSimple.Add(new CustomKey { Name = "key1" }, "val1");
        obj.DictSimple.Add(new CustomKey { Name = "key2" }, "val2");
        obj.DictSimple.Add(new CustomKey { Name = "key3" }, "val3");
        obj.DictComplex.Add(new CustomKey { Name = "key1" }, new ComplexVal { Name = "val1", SomeInt = 7, SomeOtherInfo = "bla" });
        obj.DictComplex.Add(new CustomKey { Name = "key2" }, new ComplexVal { Name = "val2", SomeInt = 42, SomeOtherInfo = "lorem ipsum..." });
        obj.DictComplex.Add(new CustomKey { Name = "key3" }, new ComplexVal { Name = "val3", SomeInt = 1337, SomeOtherInfo = "idk" });

        // Serializer settings
        var serializerSettings = new JsonSerializerSettings
        {
            Converters = new List<JsonConverter>
            {
                new JsonCustomKeyDictionaryObjectConverter(),
                new CustomKeyConverter()
            },
            Formatting = Formatting.Indented
        };

        // Serialization
        const string testfile = "test.json";
        using (var sw = new StreamWriter(testfile))
            sw.Write(JsonConvert.SerializeObject(obj, serializerSettings));

        // Deserialization
        Obj objDes = null;
        using (var sr = new StreamReader(testfile))
            objDes = JsonConvert.DeserializeObject<Obj>(sr.ReadToEnd(), serializerSettings);

        // Result print
        foreach (var kvp in objDes.DictPlain) Console.WriteLine($"Key: {kvp.Key}, Value: {kvp.Value}");
        foreach (var kvp in objDes.DictSimple) Console.WriteLine($"Key: {kvp.Key.Name}, Value: {kvp.Value}");
        foreach (var kvp in objDes.DictComplex) Console.WriteLine($"Key: {kvp.Key.Name}, Value: {kvp.Value.Name}");
        Console.Read();
    }
}

