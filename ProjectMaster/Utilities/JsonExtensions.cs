using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace ProjectMaster.Utilities
{
    public static class JsonExtensions
    {
        public static JArray GetArrayOrNull(this JObject token, string name) => token[name] as JArray;
        public static JObject GetObjectOrNull(this JObject token, string name) => token[name] as JObject;

        public static IEnumerable<JToken> GetObjectOfArrayChildren(this JArray token, string name)
        {
            foreach (var jToken in token)
            {
                var value = (jToken as JObject)?[name];
                if (value != null)
                    yield return value;
            }
        }

        public static JArray ForceToArray(this JToken token)
        {
            if (token is JArray) return (JArray) token;
            if (token is JObject)
            {
                var writer = new JTokenWriter();
                writer.WriteStartArray();
                writer.WriteToken(new JTokenReader(token));
                writer.WriteEndArray();
                return (JArray) writer.Token;
            }

            throw new ArgumentException("token should be either JArray or JObject", nameof(token));
        }
    }
}