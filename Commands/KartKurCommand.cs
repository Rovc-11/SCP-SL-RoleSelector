// -----------------------------------------------------------------------
// <copyright file="KartKurCommand.cs" company="William">
// Role Selector - "kartkur" RA command.
// </copyright>
// -----------------------------------------------------------------------

namespace RoleSelector.Commands
{
    using System;

    using CommandSystem;
    using Exiled.API.Features;
    using PlayerRoles;
    using RemoteAdmin;
    using RoleSelector.Core;

    /// <summary>
    /// "kartkur &lt;Rol&gt; [özel_isim]" — bulunduğunuz konuma bir rol kartı slotu kaydeder.
    /// Havuz (D Sınıfı / Araştırma / Güvenlik / SCP / Serbest), verilen Rol'ün vanilla Team'inden
    /// otomatik olarak çözülür; ayrıca bir havuz belirtmenize gerek yoktur.
    /// Sadece bu komutla kaydedilen slotlar tur başında kart olarak spawn olur.
    /// </summary>
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    public sealed class KartKurCommand : ICommand
    {
        /// <inheritdoc/>
        public string Command => "kartkur";

        /// <inheritdoc/>
        public string[] Aliases => Array.Empty<string>();

        /// <inheritdoc/>
        public string Description => "Bulunduğunuz konuma bir rol kartı slotu kaydeder. Kullanım: kartkur <Rol> [özel_isim] (örn: kartkur Scp173  /  kartkur ClassD Asci)";

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

            if (!Enum.TryParse(ArgAt(arguments, 0), true, out RoleTypeId role) || role == RoleTypeId.None)
            {
                response = $"'{ArgAt(arguments, 0)}' geçerli bir RoleTypeId değil (örn: Scp173, ClassD, Scientist, NtfSergeant, FacilityGuard...).";
                return false;
            }

            Player player = ResolveSenderPlayer(sender);
            if (player is null)
            {
                response = "Bu komutu oyun içinde bir oyuncu/spectator olarak (konumunuz kaydedilecek şekilde) çalıştırmalısınız.";
                return false;
            }

            string tag = arguments.Count >= 2 ? ArgAt(arguments, 1) : string.Empty;

            CardSlot slot = Plugin.Instance.CardSlots.Add(role, tag, player.Position);

            response = $"Kart slotu #{slot.Id} kaydedildi: {slot.DisplayName}  |  Havuz: {slot.Pool}  |  Konum: {player.Position}";
            return true;
        }

        private static Player ResolveSenderPlayer(ICommandSender sender)
        {
            if (sender is PlayerCommandSender playerSender)
                return Player.Get(playerSender.ReferenceHub);

            return Player.Get(sender as CommandSender);
        }

        /// <summary>
        /// net48'in <see cref="ArraySegment{T}"/> uygulamasında <c>this[int]</c> indeksleyicisi
        /// bulunmadığından (bu, netstandard2.1+ ile eklendi), elle Array/Offset üzerinden erişir.
        /// </summary>
        internal static string ArgAt(ArraySegment<string> arguments, int index) => arguments.Array[arguments.Offset + index];
    }
}
