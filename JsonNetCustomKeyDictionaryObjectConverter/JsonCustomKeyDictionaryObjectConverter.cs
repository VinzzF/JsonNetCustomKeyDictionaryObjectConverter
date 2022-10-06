using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace JsonNetCustomKeyDictionaryObjectConverter
{
    /// <summary>
    /// <para>Converter from and to <see cref="Dictionary{TKey, TValue}"/> types.</para>
    /// <para>Works with non-string key types, as long as they can be serialized as plain strings (no complex structures).</para>
    /// <para>Works for all dictionary types, without explicit generics binding.</para>
    /// </summary>
    public class JsonCustomKeyDictionaryObjectConverter : JsonConverter
    {
        /// <summary>
        /// <para>Determines whether this instance can convert the specified object type.</para>
        /// <para>true, if the specified object type is a <see cref="Dictionary{TKey, TValue}"/> and will be (de-)serialized</para>
        /// </summary>
        /// <param name="objectType">Type of the object.</param>
        /// <returns>true, if the specified object type is a <see cref="Dictionary{TKey, TValue}"/> and will be (de-)serialized</returns>
        public override bool CanConvert(Type objectType)
        {
            return typeof(IDictionary).IsAssignableFrom(objectType)
                || TypeImplementsGenericInterface(objectType, typeof(IDictionary<,>));
        }

        /// <summary>
        /// Determines whether <paramref name="concreteType"/> implements <paramref name="interfaceType"/>
        /// </summary>
        /// <param name="concreteType">Type checked to contain interface type.</param>
        /// <param name="interfaceType">Interface type.</param>
        /// <returns>true, if <paramref name="concreteType"/> implements <paramref name="interfaceType"/></returns>
        private static bool TypeImplementsGenericInterface(Type concreteType, Type interfaceType)
        {
            return concreteType.GetInterfaces()
                   .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == interfaceType);
        }

        /// <summary>
        /// Acquires the generic Dictionary arguments via reflection.
        /// </summary>
        /// <param name="objectType">Type of the dictionary.</param>
        /// <param name="dictionaryTypes">Output types of the generic arguments.</param>
        /// <param name="isStringKey">Output whether the key is a <see cref="string"/> type.</param>
        private static void GetDictGenericTypes(Type objectType, out Type[] dictionaryTypes, out bool isStringKey)
        {
            dictionaryTypes = objectType.GetGenericArguments();
            if ((dictionaryTypes?.Length ?? 0) < 2)
                throw new InvalidOperationException($"Deserializing Json dictionary with less than two types {objectType.Name}");
            isStringKey = dictionaryTypes[0] == typeof(string);
        }

        /// <summary>
        /// <para>Reads the JSON representation of the object.</para>
        /// <para>Deseralizes each <see cref="KeyValuePair{TKey, TValue}"/> back into the resulting <see cref="Dictionary{TKey, TValue}"/>.</para>
        /// </summary>
        /// <param name="reader">The <see cref="JsonReader"/> to read from.</param>
        /// <param name="objectType">Type of the object.</param>
        /// <param name="existingValue">The existing value of object being read.</param>
        /// <param name="serializer">The calling serializer.</param>
        /// <returns><see cref="Dictionary{TKey, TValue}"/> consisting of each deserialized <see cref="KeyValuePair{TKey, TValue}"/>.</returns>
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            // Aquire reflection info & create resulting dictionary:
            GetDictGenericTypes(objectType, out Type[] dictionaryTypes, out bool isStringKey);
            var res = Activator.CreateInstance(objectType) as IDictionary;

            // Read each key-value-pair:
            object key = null;
            object value = null;

            if (reader.TokenType != JsonToken.StartObject)
                throw new JsonException("Json Dictionary is not represented as object");

            while (reader.Read())
            {
                if (reader.TokenType == JsonToken.EndObject)
                {
                    if (key != null || value != null) throw new JsonException($"Json Dictionary ended while still expecting key or value {key}, {value}");
                    break;
                }

                if (reader.TokenType == JsonToken.PropertyName)
                {
                    if (key != null) throw new JsonException($"Json Dictionary key {key} followed by another key, not value");
                    if (reader.ValueType != typeof(string)) throw new JsonException($"Json Dictionary key {reader.Value} is no string type");
                    key = isStringKey ? reader.Value : serializer.Deserialize(reader, dictionaryTypes[0]);
                    if (value != null) throw new JsonException($"Json Dictionary key {key} read while value present");
                }
                else
                {
                    if (key == null) throw new JsonException($"Json Dictionary value read {reader.ReadAsString()} but no read before");
                    if (value != null) throw new JsonException($"Json Dictionary value read {reader.ReadAsString()} but already has value {value}");
                    value = serializer.Deserialize(reader, dictionaryTypes[1]);

                    res.Add(key, value);
                    key = null;
                    value = null;
                }
            }

            return res;
        }

        /// <summary>
        /// <para>Writes the JSON representation of the object.</para>
        /// <para>Forces serialization of the key type, uses default serialization for the value type.</para>
        /// </summary>
        /// <param name="writer">The <see cref="JsonWriter"/> to write to.</param>
        /// <param name="value">The value.</param>
        /// <param name="serializer">The calling serializer.</param>
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            // Aquire reflection info & get key-value-pairs:
            Type type = value.GetType();
            GetDictGenericTypes(type, out Type[] dictionaryTypes, out bool isStringKey);
            IEnumerable keys = (IEnumerable)type.GetProperty("Keys").GetValue(value, null);
            IEnumerable values = (IEnumerable)type.GetProperty("Values").GetValue(value, null);
            IEnumerator valueEnumerator = values.GetEnumerator();

            // Write each key-value-pair:
            StringBuilder sb = new StringBuilder();
            using (StringWriter tempWriter = new StringWriter(sb))
            {
                writer.WriteStartObject();
                foreach (object key in keys)
                {
                    valueEnumerator.MoveNext();

                    // convert key, force serialization of non-string keys
                    string keyStr = null;
                    if (isStringKey)
                    {
                        keyStr = (string)key;
                    }
                    else
                    {
                        sb.Clear();
                        serializer.Serialize(tempWriter, key);
                        keyStr = RemoveStripCapsulation(sb.ToString());
                        // TO-DO: Validate key resolves to single string, no complex structure
                    }
                    writer.WritePropertyName(keyStr);

                    // default serialize value
                    serializer.Serialize(writer, valueEnumerator.Current);
                }
                writer.WriteEndObject();
            }
        }

        /// <summary>
        /// Removes string literal encapsulation if present on given string.
        /// </summary>
        /// <param name="str">String to remove encapsulation from.</param>
        /// <returns>String without encapsulation.</returns>
        private string RemoveStripCapsulation(string str)
        {
            if (str[0] == '\"' && str[str.Length-1] == '\"') return str.Substring(1, str.Length - 1);
            return str;
        }
    }
}
