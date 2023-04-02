using System;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SarifViewer.Models;

public class SkipBaseClassPropertiesJsonConverter<T> : JsonConverter<T> where T : class
{
    public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => throw new NotImplementedException("Deserialization is not implemented for this converter.");

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        var derivedClassProperties = value.GetType()
            .GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance);

        foreach (var property in derivedClassProperties)
        {
            writer.WritePropertyName(property.Name);
            JsonSerializer.Serialize(writer, property.GetValue(value), property.PropertyType, options);
        }

        writer.WriteEndObject();
    }
}
