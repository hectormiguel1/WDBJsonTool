using System.Text.Json;
using WDBJsonTool.DataStructures; // Assuming WDBFile is in WDBJsonTool.Core
using System.Linq; // For .Any()
using System.Collections.Generic; // For List<T>

namespace WDBJsonTool.Support
{
    internal static class JsonWriter
    {
        public static void WriteWDBFileToJson(WDBFile wdbFile, string jsonFilePath)
        {
            using (var jsonStream = new MemoryStream())
            {
                var options = new JsonWriterOptions
                {
                    Indented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };

                using (var jsonUtf8Writer = new Utf8JsonWriter(jsonStream, options))
                {
                    jsonUtf8Writer.WriteStartObject();

                    // Write sections
                    foreach (var sectionEntry in wdbFile.Sections)
                    {
                        jsonUtf8Writer.WritePropertyName(sectionEntry.Key);
                        WriteValue(jsonUtf8Writer, sectionEntry.Value);
                    }

                    // Write records
                    if (wdbFile.Records.Any())
                    {
                        jsonUtf8Writer.WriteStartArray(JsonVariables.RecordsArrayToken);
                        foreach (var record in wdbFile.Records)
                        {
                            jsonUtf8Writer.WriteStartObject();
                            foreach (var field in record)
                            {
                                jsonUtf8Writer.WritePropertyName(field.Key);
                                WriteValue(jsonUtf8Writer, field.Value);
                            }
                            jsonUtf8Writer.WriteEndObject();
                        }
                        jsonUtf8Writer.WriteEndArray();
                    }

                    jsonUtf8Writer.WriteEndObject();
                }

                Log.Info("");
                Log.Info("");
                Log.Info("Writing wdb data to json file....");

                if (File.Exists(jsonFilePath))
                {
                    File.Delete(jsonFilePath);
                }

                jsonStream.Seek(0, SeekOrigin.Begin);
                File.WriteAllBytes(jsonFilePath, jsonStream.ToArray());
            }

            Log.Info("");
            Log.Info("");
            Log.Info("Finished extracting wdb data to json file");
        }

        // Helper method to write various types to Utf8JsonWriter
        private static void WriteValue(Utf8JsonWriter writer, object value)
        {
            switch (value)
            {
                case int i:
                    writer.WriteNumberValue(i);
                    break;
                case uint u:
                    writer.WriteNumberValue(u);
                    break;
                case long l:
                    writer.WriteNumberValue(l);
                    break;
                case ulong ul:
                    writer.WriteNumberValue(ul);
                    break;
                case float f:
                    writer.WriteNumberValue(f);
                    break;
                case double d:
                    writer.WriteNumberValue(d);
                    break;
                case bool b:
                    writer.WriteBooleanValue(b);
                    break;
                case string s:
                    writer.WriteStringValue(s);
                    break;
                case List<uint> uintList:
                    writer.WriteStartArray();
                    foreach (var item in uintList)
                    {
                        writer.WriteNumberValue(item);
                    }
                    writer.WriteEndArray();
                    break;
                case List<int> intList:
                    writer.WriteStartArray();
                    foreach (var item in intList)
                    {
                        writer.WriteNumberValue(item);
                    }
                    writer.WriteEndArray();
                    break;
                case List<string> stringList:
                    writer.WriteStartArray();
                    foreach (var item in stringList)
                    {
                        writer.WriteStringValue(item);
                    }
                    writer.WriteEndArray();
                    break;
                case WDBSection section: // Handles nested sections (Dictionaries)
                    writer.WriteStartObject();
                    foreach (var entry in section)
                    {
                        writer.WritePropertyName(entry.Key);
                        WriteValue(writer, entry.Value);
                    }
                    writer.WriteEndObject();
                    break;
                case WDBRecord record: // Handles nested records (Dictionaries)
                    writer.WriteStartObject();
                    foreach (var entry in record)
                    {
                        writer.WritePropertyName(entry.Key);
                        WriteValue(writer, entry.Value);
                    }
                    writer.WriteEndObject();
                    break;
                case Dictionary<string, object> dictionary: // Generic dictionary fallback
                    writer.WriteStartObject();
                    foreach (var entry in dictionary)
                    {
                        writer.WritePropertyName(entry.Key);
                        WriteValue(writer, entry.Value);
                    }
                    writer.WriteEndObject();
                    break;
                default:
                    // Fallback for types not explicitly handled
                    // Consider adding a more robust serialization for complex objects
                    // or throwing an exception for unsupported types
                    writer.WriteStringValue(value?.ToString()); // Or handle as desired
                    break;
            }
        }
    }
}
