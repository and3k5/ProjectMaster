using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace ProjectMaster.Models.Nuget
{
    public class NuspecString
    {
        private readonly string rawStr;

        public NuspecString(string str, Dictionary<string, string> nuspecTokens)
        {
            this.rawStr = str;
            var paramsList = new List<string>();
            foreach (Match m in Regex.Matches(str, @"\$([^\$]+)\$", RegexOptions.Multiline))
                paramsList.Add(m.Value);
            this.Parameters = paramsList.ToArray();
            this.Parameter = nuspecTokens;
        }

        public string[] Parameters { get; }

        private Dictionary<string, string> Parameter;

        public NuspecString(string str) : this(str, new Dictionary<string, string>())
        {
        }

        public void AddParameter(string key, string value)
        {
            if (key.StartsWith("$"))
                key = key.Substring(1);
            if (key.EndsWith("$"))
                key = key.Substring(0, key.Length - 1);
            Parameter.Add(key, value);
        }

        public override string ToString()
        {
            var missingKeys = new List<string>();
            foreach (var par in Parameters)
            {
                if (!Parameter.ContainsKey(par))
                    missingKeys.Add(par);
            }

            if (missingKeys.Count > 0)
                throw new Exception("Missing variable in nugetstring: " + rawStr + " variables: " + string.Join(", ", missingKeys));

            var strResult = rawStr;
            foreach (var parameter in Parameter)
            {
                strResult = strResult.Replace("$" + parameter.Key + "$", parameter.Value);
            }

            if (strResult.Contains("$"))
                throw new Exception("Missing variable in nugetstring (not found in parse): " + strResult);
            return strResult;
        }
    }
}