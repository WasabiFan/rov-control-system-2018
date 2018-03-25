using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RovOperatorInterface.Communication
{
    public struct SerialMessage
    {
        public static bool TryParse(string source, out SerialMessage message)
        {
            if (!source.StartsWith("!") || source.Length <= 1 || source.Contains("\n"))
            {
                message = new SerialMessage { Type = null, Parameters = null };
                return false;
            }

            var allParts = source.Split(" ");
            message = new SerialMessage
            {
                Type = allParts.First().Substring(1),
                Parameters = allParts.Skip(1).ToArray()
            };
            return true;
        }

        public string Serialize()
        {
            return $"!{Type}{String.Join("", Parameters.Select(s => $" {s}"))}";
        }

        public SerialMessage(string type, params string[] parameters)
        {
            this.Type = type;
            this.Parameters = parameters;
        }

        public string Type { get; private set; }
        public string[] Parameters { get; private set; }
    }
}
