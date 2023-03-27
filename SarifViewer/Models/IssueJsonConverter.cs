using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SarifViewer.Models;

public class IssueJsonConverter : JsonConverter<Issue>
{
    public override Issue Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException("Expected StartObject token.");
        }

        string id = null;
        List<Location> locations = new List<Location>();
        string message = null;

        while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
        {
            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                string propertyName = reader.GetString();

                if (propertyName == "id")
                {
                    id = JsonSerializer.Deserialize<string>(ref reader, options);
                }
                else if (propertyName == "location")
                {
                    reader.Read();

                    if (reader.TokenType == JsonTokenType.StartArray)
                    {
                        locations = JsonSerializer.Deserialize<List<Location>>(ref reader, options);
                    }
                    else
                    {
                        Location singleLocation = JsonSerializer.Deserialize<Location>(ref reader, options);
                        locations.Add(singleLocation);
                    }
                }
                else if (propertyName == "message")
                {
                    message = JsonSerializer.Deserialize<string>(ref reader, options);
                }
            }
        }

        return new Issue { Id = id, Location = locations.ToArray(), Message = message };
    }

    public override void Write(Utf8JsonWriter writer, Issue value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}