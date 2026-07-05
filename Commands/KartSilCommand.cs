// -----------------------------------------------------------------------
// <copyright file="KartSilCommand.cs" company="William">
// Role Selector - "kartsil" RA command.
// </copyright>
// -----------------------------------------------------------------------

namespace RoleSelector.Commands
{
    using System;

    using CommandSystem;
    using RemoteAdmin;

    /// <summary>
    /// "kartsil &lt;slotId&gt;" — daha önce "kartkur" ile kaydedilmiş bir kart slotunu siler.
    /// Slot Id'lerini görmek için "kartlistele" kullanın. Bir konumu "değiştirmek" için önce
    /// kartsil ile eski slotu silip sonra yeni konumda tekrar kartkur çalıştırın.
    /// </summary>
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    public sealed class KartSilCommand : ICommand
    {
        /// <inheritdoc/>
        public string Command => "kartsil";

        /// <inheritdoc/>
        public string[] Aliases => Array.Empty<string>();

        /// <inheritdoc/>
        public string Description => "Kayıtlı bir kart slotunu siler. Kullanım: kartsil <slotId> (id'ler için: kartlistele)";

        /// <inheritdoc/>
        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            if (Plugin.Instance is null || !Plugin.Instance.Config.IsEnabled)
            {
                response = "RoleSelector eklentisi devre dışı.";
                return false;
            }

            if (arguments.Count < 1 || !int.TryParse(KartKurCommand.ArgAt(arguments, 0), out int id))
            {
                response = Description;
                return false;
            }

            bool removed = Plugin.Instance.CardSlots.Remove(id);
            response = removed ? $"Slot #{id} silindi." : $"#{id} numaralı bir slot bulunamadı.";
            return removed;
        }
    }
}
