using Newtonsoft.Json;
using SoapUI.Common;

namespace SoapUI.Models
{
    /// <summary>
    /// A single script argument (type + raw string value).
    /// Replaces the legacy duplicated LuaValueNew / JSONArgument types.
    /// Serialises to {"type": "...", "value": "..."} for import/export.
    /// </summary>
    public sealed class LuaArgument
    {
        public LuaArgument()
            : this(LuaType.Nil, null)
        {
        }

        public LuaArgument(string type, object value)
        {
            Type = string.IsNullOrWhiteSpace(type) ? LuaType.Nil : type.Trim();
            Value = value;
        }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("value")]
        public object Value { get; set; }

        public string ValueAsString
        {
            get { return Value != null ? Value.ToString() : string.Empty; }
        }

        public override string ToString()
        {
            return Type + " = " + ValueAsString;
        }
    }
}
