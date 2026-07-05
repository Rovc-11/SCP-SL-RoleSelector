// -----------------------------------------------------------------------
// <copyright file="SpawnKurCommand.cs" company="William">
// Role Selector - "spawnkur" RA command.
// </copyright>
// -----------------------------------------------------------------------

namespace RoleSelector.Commands
{
    using System;

    using CommandSystem;
    using RemoteAdmin;
    using RoleSelector.Core;

    /// <summary>
    /// "spawnkur &lt;MerId&gt;" — verilen Project Mer nesne Id'sini, oyuncuların round başında
    /// lobiye ışınlanacağı bir nokta olarak kaydeder. İstenildiği kadar spawn eklenebilir; oyuncular
    /// eklenen tüm spawnlar arasında sırayla dağıtılır (kalabalık olmasın diye).
    /// <para>
    /// Id'yi öğrenmek için: haritayı Project Mer ile düzenlerken (mp toolgun açıkken) spawn noktası
    /// olacak Primitive'e bakıp <c>mp select</c> yazın (seçmek için), ardından <c>mp modify</c>
    /// yazın (argümansız) — yanıtta "ID: ..." satırını göreceksiniz, işte kaydedeceğiniz Id budur.
    /// </para>
    /// </summary>
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    public sealed class SpawnKurCommand : ICommand
    {
        /// <inheritdoc/>
        public string Command => "spawnkur";

        /// <inheritdoc/>
        public string[] Aliases => Array.Empty<string>();

        /// <inheritdoc/>
        public string Description => "Verilen Project Mer nesne Id'sini lobi spawn noktası olarak kaydeder. Kullanım: spawnkur <MerId> (Id için: nesneye bakıp 'mp select', sonra 'mp modify')";

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

            if (!Plugin.Instance.SpawnIds.Add(id))
            {
                response = $"'{id}' zaten kayıtlı bir spawn Id'si.";
                return false;
            }

            bool foundInScene = ProjectMerBridge.ExistsInScene(id);
            response = foundInScene
                ? $"Spawn noktası kaydedildi: '{id}' (sahnede bulundu, doğru nesne)."
                : $"Spawn noktası kaydedildi: '{id}' — UYARI: şu an sahnede bu Id'ye sahip bir nesne bulunamadı (harita yüklü olmayabilir ya da Id yanlış girilmiş olabilir; round başında tekrar kontrol edilecek).";
            return true;
        }
    }
}
