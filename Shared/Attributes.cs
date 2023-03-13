using System;

namespace Veda
{
    [AttributeUsage(AttributeTargets.Method)]
    public class AliasesAttribute : Attribute
    {
        public string[] Aliases { get; }

        public AliasesAttribute(params string[] aliases) => Aliases = aliases;
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class CommandAttribute : Attribute
    {
        public string Command { get; }

        public CommandAttribute(string command) => Command = command;
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class DoNotShowInHelpAttribute : Attribute
    {
    }
    [AttributeUsage(AttributeTargets.Method)]
    public class HelpMessageAttribute : Attribute
    {
        public string HelpMessage { get; }

        public HelpMessageAttribute(string helpMessage) => HelpMessage = helpMessage;
    }
}
