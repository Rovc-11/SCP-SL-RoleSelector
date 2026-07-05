// -----------------------------------------------------------------------
// <copyright file="KartListeleCommand.cs" company="William">
// Role Selector - "kartlistele" RA command.
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
    /// "kartlistele" — kayıtlı tüm kart slotlarını (Id, havuz, rol/etiket, konum) listeler.
    /// "kartsil" ile silmek için Id'lere buradan bakabilirsiniz. Bu komut kullanıcının açık
    /// isteği olmasa da, kartsil'in çalışabilmesi için pratikte zorunlu olduğundan eklendi.
    /// </summary>
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    public sealed class KartListeleCommand : ICommand
    {
        /// <inheritdoc/>
        public string Command => "kartlistele";

        /// <inheritdoc/>
        public string[] Aliases => new[] { "kartlar" };

        /// <inheritdoc/>
        public string Description => "Kayıtlı tüm kart slotlarını listeler (Id, havuz, rol/etiket, konum).";

        /// <inheritdoc/>
        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            if (Plugin.Instance is null)
            {
                response = "RoleSelector eklentisi yüklü değil.";
                return false;
            }

            IReadOnlyList<CardSlot> all = Plugin.Instance.CardSlots.All;
            if (all.Count == 0)
            {
                response = "Kayıtlı kart slotu yok. 'kartkur <Rol> [özel_isim]' ile ekleyebilirsiniz.";
                return true;
            }

            StringBuilder builder = new();
            foreach (CardSlot slot in all)
                builder.AppendLine($"#{slot.Id} | {slot.Pool} | {slot.DisplayName} | {slot.Position}");

            response = builder.ToString();
            return true;
        }
    }
}
