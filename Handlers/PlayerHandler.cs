// -----------------------------------------------------------------------
// <copyright file="PlayerHandler.cs" company="William">
// Role Selector - player event handler.
// </copyright>
// -----------------------------------------------------------------------

namespace RoleSelector.Handlers
{
    using Exiled.Events.EventArgs.Player;
    using RoleSelector.Core;

    /// <summary>
    /// Oyuncu olaylarını (kart alma, ayrılma) <see cref="SelectionManager"/>'a bağlar.
    /// </summary>
    public sealed class PlayerHandler
    {
        private readonly SelectionManager manager;

        /// <summary>
        /// Initializes a new instance of the <see cref="PlayerHandler"/> class.
        /// </summary>
        /// <param name="manager">Bağlanacak seçim yöneticisi.</param>
        public PlayerHandler(SelectionManager manager) => this.manager = manager;

        /// <summary>
        /// Bir oyuncu herhangi bir eşyayı almaya çalıştığında tetiklenir.
        /// Eğer alınan obje bir rol kartıysa, gerçek envantere girmesi engellenir ve
        /// seçim mantığı devreye girer.
        /// </summary>
        /// <param name="ev">Pickup olayı bilgisi.</param>
        public void OnPickingUpItem(PickingUpItemEventArgs ev)
        {
            if (!manager.IsRoleCard(ev.Pickup))
                return;

            // Bu bir rol kartı: hiçbir zaman gerçek envantere girmemeli.
            ev.IsAllowed = false;

            manager.HandleCardPickup(ev.Player, ev.Pickup);
        }

        /// <summary>
        /// Bir oyuncu sunucudan ayrıldığında bekleme/seçim listelerinden temizler.
        /// </summary>
        /// <param name="ev">Ayrılma olayı bilgisi.</param>
        public void OnLeft(LeftEventArgs ev) => manager.HandlePlayerLeft(ev.Player);
    }
}
