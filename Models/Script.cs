using System.Collections.Generic;
using SoapUI.Common;

namespace SoapUI.Models
{
    /// <summary>
    /// A named script plus its arguments. For RBXGS the
    /// <see cref="EnvironmentId"/> selects the target environment.
    /// </summary>
    public sealed class Script
    {
        public Script(string name, string script, IList<LuaArgument> arguments)
            : this(name, script, arguments, "RBX0")
        {
        }

        public Script(string name, string script, IList<LuaArgument> arguments, string environmentId)
        {
            Guard.AgainstNullOrWhiteSpace(name, nameof(name));

            Name = name.Trim();
            Content = script ?? string.Empty;
            EnvironmentId = string.IsNullOrWhiteSpace(environmentId) ? "RBX0" : environmentId.Trim();
            Arguments = arguments != null
                ? new List<LuaArgument>(arguments)
                : new List<LuaArgument>();
        }

        public string Name { get; }

        public string Content { get; }

        public string EnvironmentId { get; }

        public IList<LuaArgument> Arguments { get; }

        public override string ToString()
        {
            return "Script(Name=" + Name + ", Args=" + Arguments.Count + ")";
        }
    }
}
