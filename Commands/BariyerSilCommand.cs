// -----------------------------------------------------------------------
// <copyright file="BariyerSilCommand.cs" company="William">
// Role Selector - "bariyersil" RA command.
// </copyright>
// -----------------------------------------------------------------------

namespace RoleSelector.Commands
{
    using System;

    using CommandSystem;
    using RemoteAdmin;

    /// <summary>
    /// "bariyersil &lt;MerId&gt;" — daha önce "bariyerkur" ile kaydedilmiş bir bariyer Id'sini siler.
    /// Kayıtlı Id'leri görmek için "bariyerlistele" kullanın.
    /// </summary>
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    public sealed class BariyerSilCommand : ICommand
    {
        /// <inheritdoc/>
        public string Command => "bariyersil";

        /// <inheritdoc/>
        public string[] Aliases => Array.Empty<string>();

        /// <inheritdoc/>
        public string Description => "Kayıtlı bir bariyer Id'sini siler. Kullanım: bariyersil <MerId> (Id'ler için: bariyerlistele)";

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
            bool removed = Plugin.Instance.BarrierIds.Remove(id);
            response = removed ? $"Bariyer '{id}' silindi." : $"'{id}' adında kayıtlı bir bariyer bulunamadı.";
            return removed;
        }
    }
}
