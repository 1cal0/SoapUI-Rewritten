using System;

namespace SoapUI.Models
{
    /// <summary>
    /// Well-known Lua value type names used by RCC/RBXGS SOAP payloads.
    /// </summary>
    public static class LuaType
    {
        public const string Nil = "LUA_TNIL";
        public const string Boolean = "LUA_TBOOLEAN";
        public const string Number = "LUA_TNUMBER";
        public const string String = "LUA_TSTRING";
        public const string Table = "LUA_TTABLE";

        /// <summary>
        /// Maps a CLR value to its Lua wire type. Unknown types degrade to NIL
        /// rather than failing the whole request.
        /// </summary>
        public static string FromClrValue(object value)
        {
            if (value == null)
            {
                return Nil;
            }

            var type = value.GetType();

            if (type == typeof(bool))
            {
                return Boolean;
            }

            if (type == typeof(string))
            {
                return String;
            }

            if (type.IsArray)
            {
                return Table;
            }

            if (type == typeof(int) || type == typeof(long) || type == typeof(short) ||
                type == typeof(byte) || type == typeof(float) || type == typeof(double) ||
                type == typeof(decimal))
            {
                return Number;
            }

            return Nil;
        }

        public static string FriendlyPrefix(string luaType)
        {
            switch (luaType)
            {
                case Nil: return "[NIL] ";
                case Boolean: return "[BOOLEAN] ";
                case Number: return "[NUMBER] ";
                case String: return "[STRING] ";
                case Table: return "[TABLE] ";
                default: return "[UNKNOWN TYPE] ";
            }
        }
    }
}
