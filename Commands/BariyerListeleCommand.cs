// -----------------------------------------------------------------------
// <copyright file="BariyerListeleCommand.cs" company="William">
// Role Selector - "bariyerlistele" RA command.
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
    /// "bariyerlistele" — kayıtlı tüm bariyer Id'lerini, o an sahnede bulunup bulunmadıklarıyla
    /// birlikte listeler. "bariyersil" ile silmek için Id'lere buradan bakabilirsiniz.
    /// </summary>
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    public sealed class BariyerListeleCommand : ICommand
    {
        /// <inheritdoc/>
        public string Command => "bariyerlistele";

        /// <inheritdoc/>
        public string[] Aliases => new[] { "bariyerler" };

        /// <inheritdoc/>
        public string Description => "Kayıtlı tüm bariyer Id'lerini listeler.";

        /// <inheritdoc/>
        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            if (Plugin.Instance is null)
            {
                response = "RoleSelector eklentisi yüklü değil.";
                return false;
            }

            IReadOnlyList<string> all = Plugin.Instance.BarrierIds.All;
            if (all.Count == 0)
            {
                response = "Kayıtlı bariyer yok. 'bariyerkur <MerId>' ile ekleyebilirsiniz.";
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
