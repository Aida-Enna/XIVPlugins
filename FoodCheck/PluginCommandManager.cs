using Dalamud.Game.Command;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FoodCheck.Attributes;
using static Dalamud.Game.Command.CommandInfo;

namespace FoodCheck
{
    public class PluginCommandManager<THost> : IDisposable
    {
        private readonly CommandManager commandManager;
        private readonly (string, CommandInfo)[] pluginCommands;
        private readonly THost host;

        public PluginCommandManager(THost host, CommandManager commandManager)
        {
            this.commandManager = commandManager;
            this.host = host;

            this.pluginCommands = host.GetType().GetMethods(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance)
                .Where(method => method.GetCustomAttribute<CommandAttribute>() != null)
                .SelectMany(GetCommandInfoTuple)
                .ToArray();

            AddCommandHandlers();
        }
        
        private void AddCommandHandlers()
        {
            foreach (var (command, commandInfo) in this.pluginCommands)
            {
                this.commandManager.AddHandler(command, commandInfo);
            }
        }

        private void RemoveCommandHandlers()
        {
            foreach (var (command, _) in this.pluginCommands)
            {
                this.commandManager.RemoveHandler(command);
            }
        }

        private IEnumerable<(string, CommandInfo)> GetCommandInfoTuple(MethodInfo method)
        {
            var handlerDelegate = (HandlerDelegate)Delegate.CreateDelegate(typeof(HandlerDelegate), this.host, method);

            var command = handlerDelegate.Method.GetCustomAttribute<CommandAttribute>();
            var aliases = handlerDelegate.Method.GetCustomAttribute<AliasesAttribute>();
            var helpMessage = handlerDelegate.Method.GetCustomAttribute<HelpMessageAttribute>();
            var doNotShowInHelp = handlerDelegate.Method.GetCustomAttribute<DoNotShowInHelpAttribute>();

            var commandInfo = new CommandInfo(handlerDelegate)
            {
                HelpMessage = helpMessage?.HelpMessage ?? string.Empty,
                ShowInHelp = doNotShowInHelp == null,
            };

            // Create list of tuples that will be filled with one tuple per alias, in addition to the base command tuple.
            var commandInfoTuples = new List<(string, CommandInfo)> { (command!.Command, commandInfo) };
            if (aliases != null)
            {
                foreach (var alias in aliases.Aliases)
                {
                    commandInfoTuples.Add((alias, commandInfo));
                }
            }

            return commandInfoTuples;
        }

        public void Dispose()
        {
            RemoveCommandHandlers();
            GC.SuppressFinalize(this);
        }
    }
}
