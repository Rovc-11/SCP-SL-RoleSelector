// -----------------------------------------------------------------------
// <copyright file="BariyerKurCommand.cs" company="William">
// Role Selector - "bariyerkur" RA command.
// </copyright>
// -----------------------------------------------------------------------

namespace RoleSelector.Commands
{
    using System;

    using CommandSystem;
    using RemoteAdmin;
    using RoleSelector.Core;

    /// <summary>
    /// "bariyerkur &lt;MerId&gt;" — verilen Project Mer nesne Id'sini bir "bariyer" olarak kaydeder.
    /// Seçim açılınca (bariyerlerin kalktığı an), bu Id'ye sahip nesnenin/nesnelerin collider'ı
    /// koddan kapatılır; harita dosyasında hiçbir değişiklik gerekmez. İstenildiği kadar bariyer
    /// eklenebilir.
    /// <para>
    /// Id'yi öğrenmek için: haritayı Project Mer ile düzenlerken (mp toolgun açıkken) bariyer
    /// olacak Primitive'e bakıp <c>mp select</c> yazın (seçmek için), ardından <c>mp modify</c>
    /// yazın (argümansız) — yanıtta "ID: ..." satırını göreceksiniz, işte kaydedeceğiniz Id budur.
    /// </para>
    /// </summary>
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    public sealed class BariyerKurCommand : ICommand
    {
        /// <inheritdoc/>
        public string Command => "bariyerkur";

        /// <inheritdoc/>
        public string[] Aliases => Array.Empty<string>();

        /// <inheritdoc/>
        public string Description => "Verilen Project Mer nesne Id'sini bariyer olarak kaydeder. Kullanım: bariyerkur <MerId> (Id için: nesneye bakıp 'mp select', sonra 'mp modify')";

        /// <inheritdoc/>
        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            if (Plugin.Instance is null || !Plugin.Instance.Config.IsEnabled)
            {
                response = "RoleSelector eklentisi devre dışı.";
                return false;
            }

            if (arguments.Count < 1 || string.IsNullOrWhiteSpace(KartKurCommand.ArgAt(arguments, 0)))
            {
                response = Description;
                return false;
            }

            string id = KartKurCommand.ArgAt(arguments, 0);

            if (!Plugin.Instance.BarrierIds.Add(id))
            {
                response = $"'{id}' zaten kayıtlı bir bariyer Id'si.";
                return false;
            }

            bool foundInScene = ProjectMerBridge.ExistsInScene(id);
            response = foundInScene
                ? $"Bariyer kaydedildi: '{id}' (sahnede bulundu, doğru nesne)."
                : $"Bariyer kaydedildi: '{id}' — UYARI: şu an sahnede bu Id'ye sahip bir nesne bulunamadı (harita yüklü olmayabilir ya da Id yanlış girilmiş olabilir; seçim anında tekrar kontrol edilecek).";
            return true;
        }
    }
}
