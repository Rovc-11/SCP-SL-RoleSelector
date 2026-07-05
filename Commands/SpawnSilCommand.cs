// -----------------------------------------------------------------------
// <copyright file="SpawnSilCommand.cs" company="William">
// Role Selector - "spawnsil" RA command.
// </copyright>
// -----------------------------------------------------------------------

namespace RoleSelector.Commands
{
    using System;

    using CommandSystem;
    using RemoteAdmin;

    /// <summary>
    /// "spawnsil &lt;MerId&gt;" — daha önce "spawnkur" ile kaydedilmiş bir lobi spawn Id'sini siler.
    /// Kayıtlı Id'leri görmek için "spawnlistele" kullanın.
    /// </summary>
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    public sealed class SpawnSilCommand : ICommand
    {
        /// <inheritdoc/>
        public string Command => "spawnsil";

        /// <inheritdoc/>
        public string[] Aliases => Array.Empty<string>();

        /// <inheritdoc/>
        public string Description => "Kayıtlı bir spawn Id'sini siler. Kullanım: spawnsil <MerId> (Id'ler için: spawnlistele)";

        /// <inheritdoc/>
        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            if (Plugin.Instance is null || !Plugin.Instance.Config.IsEnabled)
            {
                response = "RoleSelector eklentisi devre dışı.";
                return false;
            }

            if (arguments.Count < 1)
            {
                response = Description;
                return false;
            }

            string id = KartKurCommand.ArgAt(arguments, 0);
            bool removed = Plugin.Instance.SpawnIds.Remove(id);
            response = removed ? $"Spawn noktası '{id}' silindi." : $"'{id}' adında kayıtlı bir spawn noktası bulunamadı.";
            return removed;
        }
    }
}
