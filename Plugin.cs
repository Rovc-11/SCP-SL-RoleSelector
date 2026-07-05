// -----------------------------------------------------------------------
// <copyright file="Plugin.cs" company="William">
// Role Selector - EXILED plugin entry point.
// </copyright>
// -----------------------------------------------------------------------

namespace RoleSelector
{
    using System;
    using System.IO;

    using Exiled.API.Features;
    using RoleSelector.Core;
    using RoleSelector.Handlers;

    /// <summary>
    /// Roleplay sunucuları için "rol seçim lobisi" eklentisi.
    /// Tur başında oyuncular Project Mer ile hazırlanmış özel bir lobi haritasında doğar,
    /// kartlarını seçer, seçmeyenler D Sınıfı olur. Detaylar için README.md'ye bakın.
    /// </summary>
    public sealed class Plugin : Plugin<Config>
    {
        /// <summary>
        /// Gets the active plugin instance.
        /// </summary>
        public static Plugin Instance { get; private set; }

        /// <summary>
        /// Gets the round's selection state manager.
        /// </summary>
        public SelectionManager SelectionManager { get; private set; }

        /// <summary>
        /// Gets "kartkur"/"kartsil"/"kartlistele" komutlarıyla yönetilen kayıtlı kart slotları deposu.
        /// </summary>
        public CardSlotStore CardSlots { get; private set; }

        /// <summary>
        /// Gets "bariyerkur"/"bariyersil"/"bariyerlistele" komutlarıyla yönetilen, seçim açılınca
        /// çarpışması (PrimitiveFlags) kapatılacak Project Mer nesne Id'lerinin deposu.
        /// </summary>
        public MerIdListStore BarrierIds { get; private set; }

        /// <summary>
        /// Gets "spawnkur"/"spawnsil"/"spawnlistele" komutlarıyla yönetilen, oyuncuların lobiye
        /// ışınlanacağı Project Mer nesne Id'lerinin deposu.
        /// </summary>
        public MerIdListStore SpawnIds { get; private set; }

        /// <inheritdoc/>
        public override string Name => "RoleSelector";

        /// <inheritdoc/>
        public override string Author => "William";

        /// <inheritdoc/>
        public override Version Version => new(1, 0, 0);

        /// <inheritdoc/>
        public override Version RequiredExiledVersion => new(9, 0, 0);

        private ServerHandler serverHandler;
        private PlayerHandler playerHandler;

        /// <inheritdoc/>
        public override void OnEnabled()
        {
            Instance = this;
            CardSlots = new CardSlotStore(Path.Combine(Paths.Configs, "RoleSelector"));
            BarrierIds = new MerIdListStore(Path.Combine(Paths.Configs, "RoleSelector"), "barrier_ids.yml");
            SpawnIds = new MerIdListStore(Path.Combine(Paths.Configs, "RoleSelector"), "spawn_ids.yml");
            SelectionManager = new SelectionManager(Config, CardSlots);

            serverHandler = new ServerHandler(SelectionManager);
            playerHandler = new PlayerHandler(SelectionManager);

            Exiled.Events.Handlers.Server.RoundStarted += serverHandler.OnRoundStarted;
            Exiled.Events.Handlers.Server.RestartingRound += serverHandler.OnRestartingRound;
            Exiled.Events.Handlers.Server.RoundEnded += serverHandler.OnRoundEnded;

            Exiled.Events.Handlers.Player.PickingUpItem += playerHandler.OnPickingUpItem;
            Exiled.Events.Handlers.Player.Left += playerHandler.OnLeft;

            base.OnEnabled();
        }

        /// <inheritdoc/>
        public override void OnDisabled()
        {
            Exiled.Events.Handlers.Server.RoundStarted -= serverHandler.OnRoundStarted;
            Exiled.Events.Handlers.Server.RestartingRound -= serverHandler.OnRestartingRound;
            Exiled.Events.Handlers.Server.RoundEnded -= serverHandler.OnRoundEnded;

            Exiled.Events.Handlers.Player.PickingUpItem -= playerHandler.OnPickingUpItem;
            Exiled.Events.Handlers.Player.Left -= playerHandler.OnLeft;

            SelectionManager?.Shutdown();
            SelectionManager = null;
            CardSlots = null;
            BarrierIds = null;
            SpawnIds = null;
            serverHandler = null;
            playerHandler = null;
            Instance = null;

            base.OnDisabled();
        }
    }
}
