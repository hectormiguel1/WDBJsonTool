using System.Text.Json;

namespace WDBJsonTool.Support
{
    internal class JsonMethods
    {
        public static void CheckTokenType(string tokenType, ref Utf8JsonReader jsonReader, string property)
        {
            _ = jsonReader.Read();

            switch (tokenType)
            {
                case "Array":
                    if (jsonReader.TokenType != JsonTokenType.StartArray)
                    {
                        SharedMethods.ErrorExit($"Specified {property} property's value is not a number");
                    }
                    break;

                case "Bool":
                    if (jsonReader.TokenType != JsonTokenType.True)
                    {
                        if (jsonReader.TokenType != JsonTokenType.False)
                        {
                            SharedMethods.ErrorExit($"Specified {property} property's value is not a boolean");
                        }
                    }
                    break;

                case "Number":
                    if (jsonReader.TokenType != JsonTokenType.Number)
                    {
                        SharedMethods.ErrorExit($"Specified {property} property's value is not a number");
                    }
                    break;

                case "PropertyName":
                    if (jsonReader.TokenType != JsonTokenType.PropertyName)
                    {
                        SharedMethods.ErrorExit($"{property} type is not a valid PropertyName");
                    }
                    break;

                case "String":
                    if (jsonReader.TokenType != JsonTokenType.String)
                    {
                        SharedMethods.ErrorExit($"Specified {property} property's value is not a string");
                    }
                    break;
            }
        }


        public static void CheckPropertyName(ref Utf8JsonReader jsonReader, string propertyName)
        {
            if (jsonReader.GetString() != propertyName)
            {
                SharedMethods.ErrorExit($"Missing {propertyName} property at expected position");
            }
        }


        public static List<int> GetNumbersFromArrayPropertyInt(ref Utf8JsonReader jsonReader, string arrayProperty)
        {
            var numbersList = new List<int>();

            while (true)
            {
                _ = jsonReader.Read();

                if (jsonReader.TokenType == JsonTokenType.EndArray)
                {
                    break;
                }

                if (jsonReader.TokenType != JsonTokenType.Number)
                {
                    SharedMethods.ErrorExit($"Detected a value that is not a number in {arrayProperty} property");
                }

                numbersList.Add(jsonReader.GetInt32());
            }

            return numbersList;
        }


        public static List<uint> GetNumbersFromArrayPropertyUInt(ref Utf8JsonReader jsonReader, string arrayProperty)
        {
            var numbersList = new List<uint>();

            while (true)
            {
                _ = jsonReader.Read();

                if (jsonReader.TokenType == JsonTokenType.EndArray)
                {
                    break;
                }

                if (jsonReader.TokenType != JsonTokenType.Number)
                {
                    SharedMethods.ErrorExit($"Detected a value that is not a number in {arrayProperty} property");
                }

                numbersList.Add(jsonReader.GetUInt32());
            }

            return numbersList;
        }


        public static List<string> GetStringsFromArrayProperty(ref Utf8JsonReader jsonReader, string arrayProperty)
        {
            var stringList = new List<string>();

            while (true)
            {
                _ = jsonReader.Read();

                if (jsonReader.TokenType == JsonTokenType.EndArray)
                {
                    break;
                }

                if (jsonReader.TokenType != JsonTokenType.String)
                {
                    SharedMethods.ErrorExit($"Detected a value that is not a string in {arrayProperty} property");
                }

                stringList.Add(jsonReader.GetString());
            }

            return stringList;
        }
    }
}