/**
 * Oddmatics.RozWorld.Server.Command -- RozWorld Server Command Class
 *
 * This source-code is part of the server library for the RozWorld project by rozza of Oddmatics:
 * <<http://www.oddmatics.uk>>
 * <<http://roz.world>>
 * <<http://github.com/rozniak/RozWorld-Server>>
 *
 * Sharing, editing and general licence term information can be found inside of the "LICENCE.MD" file that should be located in the root of this project's directory structure.
 */

using Oddmatics.RozWorld.API.Server;
using Oddmatics.RozWorld.API.Server.Event;

namespace Oddmatics.RozWorld.Server
{
    /// <summary>
    /// Represents a server command.
    /// </summary>
    internal class Command
    {
        /// <summary>
        /// Gets the CommandSentCallback delegate for this Command.
        /// </summary>
        public CommandSentCallback Delegate { get; private set; }

        /// <summary>
        /// Gets the description of this Command.
        /// </summary>
        public string Description { get; private set; }

        /// <summary>
        /// Gets the plugin registrar of this Command.
        /// </summary>
        public string PluginRegistrar { get; private set; }

        /// <summary>
        /// Gets the usage information for this Command.
        /// </summary>
        public string Usage { get; private set; }


        /// <summary>
        /// Initializes a new instance of the Command class with specified details.
        /// </summary>
        /// <param name="func">The delegate that is to be called with this Command.</param>
        /// <param name="registrar">The name of the plugin that registered this Command.</param>
        /// <param name="description">The description of this Command.</param>
        /// <param name="usage">The usage information for this Command.</param>
        public Command(CommandSentCallback func, string registrar, string description, string usage)
        {
            Delegate = func;
            Description = description;
            PluginRegistrar = registrar;
            Usage = usage;
        }
    }
}
