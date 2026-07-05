// -----------------------------------------------------------------------
// <copyright file="ServerHandler.cs" company="William">
// Role Selector - server event handler.
// </copyright>
// -----------------------------------------------------------------------

namespace RoleSelector.Handlers
{
    using Exiled.API.Features;
    using Exiled.Events.EventArgs.Server;
    using RoleSelector.Core;

    /// <summary>
    /// Round yaşam döngüsü olaylarını <see cref="SelectionManager"/>'a bağlar.
    /// </summary>
    public sealed class ServerHandler
    {
        private readonly SelectionManager manager;

        /// <summary>
        /// Initializes a new instance of the <see cref="ServerHandler"/> class.
        /// </summary>
        /// <param name="manager">Bağlanacak seçim yöneticisi.</param>
        public ServerHandler(SelectionManager manager) => this.manager = manager;

        /// <summary>
        /// Tur başladığında rol seçim akışını tetikler.
        /// </summary>
        public void OnRoundStarted()
        {
            if (!Plugin.Instance.Config.IsEnabled)
                return;

            if (Plugin.Instance.Config.ConfigMode)
            {
                Log.Info("[RoleSelector] 'config_mode' açık: eklenti aktif ama otomatik rol seçim akışı tur başında ÇALIŞTIRILMADI. Haritayı/kart/bariyer/spawn slotlarını rahatça kurabilirsiniz; bitince config.yml'de config_mode: false yapın.");
                return;
            }

            manager.Begin();
        }

        /// <summary>
        /// Tur yeniden başlatılırken (restart) devam eden akışı durdurur.
        /// </summary>
        public void OnRestartingRound() => manager.Stop();

        /// <summary>
        /// Tur bittiğinde devam eden akışı durdurur (bir sonraki tur temiz başlasın diye).
        /// </summary>
        /// <param name="ev">Round bitişi bilgisi.</param>
        public void OnRoundEnded(RoundEndedEventArgs ev) => manager.Stop();
    }
}
