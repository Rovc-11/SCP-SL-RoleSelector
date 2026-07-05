// -----------------------------------------------------------------------
// <copyright file="SpawnListeleCommand.cs" company="William">
// Role Selector - "spawnlistele" RA command.
// </copyright>
// -----------------------------------------------------------------------

namespace RoleSelector.Commands
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    using CommandSystem;
    using RemoteAdmin;
    using RoleSelector.Core;

    /// <summary>
    /// "spawnlistele" — kayıtlı tüm lobi spawn Id'lerini, o an sahnede bulunup bulunmadıklarıyla
    /// birlikte listeler. "spawnsil" ile silmek için Id'lere buradan bakabilirsiniz.
    /// </summary>
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    public sealed class SpawnListeleCommand : ICommand
    {
        /// <inheritdoc/>
        public string Command => "spawnlistele";

        /// <inheritdoc/>
        public string[] Aliases => new[] { "spawnlar" };

        /// <inheritdoc/>
        public string Description => "Kayıtlı tüm lobi spawn Id'lerini listeler.";

        /// <inheritdoc/>
        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            if (Plugin.Instance is null)
            {
                response = "RoleSelector eklentisi yüklü değil.";
                return false;
            }

            IReadOnlyList<string> all = Plugin.Instance.SpawnIds.All;
            if (all.Count == 0)
            {
                response = "Kayıtlı spawn noktası yok. 'spawnkur <MerId>' ile ekleyebilirsiniz.";
                return true;
            }

            StringBuilder builder = new();
            foreach (string id in all)
            {
                bool inScene = ProjectMerBridge.ExistsInScene(id);
                builder.AppendLine($"{id}  |  {(inScene ? "sahnede bulundu" : "şu an sahnede yok")}");
            }

            response = builder.ToString();
            return true;
        }
    }
}
