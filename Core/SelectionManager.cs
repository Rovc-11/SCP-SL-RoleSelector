// -----------------------------------------------------------------------
// <copyright file="SelectionManager.cs" company="William">
// Role Selector - core round-flow logic.
// </copyright>
// -----------------------------------------------------------------------

namespace RoleSelector.Core
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;

    using AdminToys;
    using Exiled.API.Enums;
    using Exiled.API.Features;
    using Exiled.API.Features.Pickups;
    using PlayerRoles;
    using UnityEngine;

    /// <summary>
    /// Rol seçim turunun hangi aşamasında olduğunu belirtir.
    /// </summary>
    public enum SelectionPhase
    {
        /// <summary>Seçim çalışmıyor (round arası ya da devre dışı).</summary>
        Idle,

        /// <summary>Oyuncular lobiye düşüyor, toplanma bekleniyor.</summary>
        Gathering,

        /// <summary>Son X saniye geri sayımı, bariyerler hâlâ kapalı.</summary>
        CountingDown,

        /// <summary>Bariyerler kalktı, oyuncular kart alabilir.</summary>
        Open,

        /// <summary>Seçim bitti, roller dağıtıldı.</summary>
        Ended,
    }

    /// <summary>
    /// Tur boyunca rol seçim akışını (harita yükleme, fazlar, kart takibi, rol atama) yönetir.
    /// Project Mer'in DLL'ine derleme zamanında bağımlı DEĞİLDİR (bkz. <see cref="ProjectMerBridge"/> —
    /// harita yükleme/kaldırma ve bariyer/spawn nesnesi bulma, reflection ile çalışma zamanında
    /// yapılır); ayrıca MEC gibi harici bir coroutine kütüphanesine de ihtiyaç duymaz:
    /// <list type="bullet">
    /// <item>Harita yükleme/kaldırma, <see cref="ProjectMerBridge.LoadMap"/>/<see cref="ProjectMerBridge.UnloadMap"/>
    /// üzerinden, ProjectMER'in "mp load/unload" RA komutlarının içindeki asıl static metotlara
    /// doğrudan ulaşılarak yapılır (komutun kendi RA izin kontrolünü atlamak için — bkz. o sınıfın
    /// XML dokümantasyonu).</item>
    /// <item>Zamanlama, sadece UnityEngine.CoreModule'de bulunan <see cref="MonoBehaviour"/>
    /// tabanlı <see cref="CoroutineRunner"/> ile yapılır.</item>
    /// <item>Bariyer/spawn objeleri, "bariyerkur"/"spawnkur" ile kaydedilmiş Project Mer nesne
    /// Id'lerinden (<see cref="ProjectMerBridge.FindByMerId"/>) bulunur — GameObject ismine ASLA
    /// bakılmaz, çünkü ProjectMER spawnladığı nesnelerin Unity sahne adını hiçbir zaman
    /// değiştirilebilir kılmaz.</item>
    /// </list>
    /// </summary>
    public sealed class SelectionManager
    {
        private readonly Config config;
        private readonly CardSlotStore cardSlots;
        private readonly Dictionary<Player, CardSlot> chosenRoles = new();
        private readonly HashSet<Player> playersAwaitingChoice = new();
        private readonly Dictionary<Pickup, CardSlot> activeCardPickups = new();
        private Coroutine activeSequence;

        /// <summary>
        /// Initializes a new instance of the <see cref="SelectionManager"/> class.
        /// </summary>
        /// <param name="config">Eklenti ayarları.</param>
        /// <param name="cardSlots">"kartkur" ile kaydedilmiş kart slotlarının deposu.</param>
        public SelectionManager(Config config, CardSlotStore cardSlots)
        {
            this.config = config;
            this.cardSlots = cardSlots;
        }

        /// <summary>
        /// Gets the current phase of the selection round.
        /// </summary>
        public SelectionPhase Phase { get; private set; } = SelectionPhase.Idle;

        /// <summary>
        /// Tur başında çağrılır: lobiyi yükler ve tüm seçim akışını başlatır.
        /// </summary>
        public void Begin()
        {
            Stop();
            chosenRoles.Clear();
            playersAwaitingChoice.Clear();
            activeSequence = CoroutineRunner.Instance.StartCoroutine(RunSequence());
        }

        /// <summary>
        /// Akışı anında durdurur (round restart / plugin disable durumunda).
        /// </summary>
        public void Stop()
        {
            if (activeSequence != null)
            {
                CoroutineRunner.Instance.StopCoroutine(activeSequence);
                activeSequence = null;
            }

            Phase = SelectionPhase.Idle;
            chosenRoles.Clear();
            playersAwaitingChoice.Clear();
            DestroyRemainingCards();
        }

        /// <summary>
        /// Plugin tamamen devre dışı bırakıldığında (OnDisabled) çağrılmalı; coroutine taşıyıcısını
        /// sahneden temizler.
        /// </summary>
        public void Shutdown()
        {
            Stop();
            CoroutineRunner.Destroy();
        }

        /// <summary>
        /// Bir oyuncunun ayrılması durumunda çağrılır, bekleme/seçim listelerinden temizler.
        /// </summary>
        /// <param name="player">Ayrılan oyuncu.</param>
        public void HandlePlayerLeft(Player player)
        {
            playersAwaitingChoice.Remove(player);
            chosenRoles.Remove(player);
        }

        /// <summary>
        /// Bir pickup'ın, bu tur plugin tarafından spawnlanmış aktif bir rol kartı olup olmadığını kontrol eder.
        /// </summary>
        /// <param name="pickup">Kontrol edilecek pickup.</param>
        /// <returns>Rol kartıysa <see langword="true"/>.</returns>
        public bool IsRoleCard(Pickup pickup) => pickup != null && activeCardPickups.ContainsKey(pickup);

        /// <summary>
        /// Bir oyuncunun rol kartı almaya çalışmasını işler. Gerçek envantere hiçbir zaman girmez;
        /// bu metot çağrıldığında çağıran taraf (event handler) pickup'ı her zaman iptal etmelidir.
        /// </summary>
        /// <param name="player">Kartı almaya çalışan oyuncu.</param>
        /// <param name="pickup">Alınmaya çalışılan kart.</param>
        public void HandleCardPickup(Player player, Pickup pickup)
        {
            if (Phase != SelectionPhase.Open)
                return;

            if (!playersAwaitingChoice.Contains(player))
                return; // zaten seçim yaptı (kart değiştirme kapalı), ya da lobi dışında biri.

            if (!activeCardPickups.TryGetValue(pickup, out CardSlot slot))
                return;

            chosenRoles[player] = slot;
            playersAwaitingChoice.Remove(player);
            activeCardPickups.Remove(pickup);

            player.Role.Set(config.WaitingRole, SpawnReason.ForceClass);
            
            string displayName = string.IsNullOrEmpty(slot.Tag) ? slot.Role.ToString() : slot.Tag;
            player.ShowHint(string.Format(config.HintCardPicked, displayName), 6f);

            if (config.DestroyCardOnPick)
                pickup.Destroy();

            if (config.Debug)
                Log.Debug($"[RoleSelector] {player.Nickname} -> {displayName} seçti.");
        }

        private IEnumerator RunSequence()
        {
            Phase = SelectionPhase.Gathering;

            ProjectMerBridge.LoadMap(config.LobbyMapName);
            yield return new WaitForSeconds(0.5f); // objelerin sahneye spawn olması için kısa bekleme

            SpawnCards(Player.List.Count());

            List<(Player Player, Vector3 Point)> assignments = PrepareLobby();
            yield return new WaitForSeconds(0.25f); // rol atamalarının bitmesini bekle, sonra pozisyonu üzerine yaz
            foreach ((Player player, Vector3 point) in assignments)
            {
                if (player.IsConnected)
                    player.Position = point;
            }

            Map.Broadcast((ushort)Mathf.Max(config.GatherDuration, 5f), config.BroadcastGathering);
            yield return new WaitForSeconds(config.GatherDuration);

            Phase = SelectionPhase.CountingDown;
            float countdownRemaining = config.PreSelectionCountdown;
            while (countdownRemaining > 0)
            {
                Map.Broadcast(6, string.Format(config.BroadcastCountdownFormat, Mathf.CeilToInt(countdownRemaining)), default, true);
                float step = Mathf.Min(5f, countdownRemaining);
                yield return new WaitForSeconds(step);
                countdownRemaining -= step;
            }

            DisableBarriers();
            Phase = SelectionPhase.Open;

            float selectionRemaining = config.SelectionDuration;
            while (selectionRemaining > 0 && playersAwaitingChoice.Count > 0)
            {
                Map.Broadcast(6, string.Format(config.BroadcastSelectionOpenFormat, Mathf.CeilToInt(selectionRemaining)), default, true);
                float step = Mathf.Min(5f, selectionRemaining);
                yield return new WaitForSeconds(step);
                selectionRemaining -= step;
            }

            FinishSelection();
        }

        /// <summary>
        /// "kartkur" ile kayıtlı KONUMLARDAN, o turki oyuncu sayısına göre hesaplanan kartları
        /// gerçekten spawnlar. Bir konum bu turun planında (<see cref="RoundPlanner.BuildRoundPlan"/>)
        /// birden fazla kez geçebilir — bu, o konumda birden fazla kart KOPYASI spawnlanması gerektiği
        /// anlamına gelir (ör. tek bir "kartkur FacilityGuard" konumu, o tur ihtiyaç 5 ise 5 kart
        /// üretir). Aynı konumdaki kopyalar <see cref="Config.CardStackSpacing"/> aralıklarla ÜST ÜSTE
        /// (dikey bir yığın/kule gibi) spawnlanır — fizik motoru zaten birbirini ittirip etrafa biraz
        /// saçacaktır, bu normaldir. Her kart ayrıca <see cref="Config.CardScale"/> oranında büyütülür.
        /// Her spawnlanan <see cref="Pickup"/>, hangi rolü temsil ettiğiyle birlikte
        /// <see cref="activeCardPickups"/> içinde bellekte tutulur (isimden çözümleme YOK).
        /// </summary>
        private void SpawnCards(int playerCount)
        {
            DestroyRemainingCards();

            IReadOnlyList<CardSlot> allSlots = cardSlots.All;
            List<CardSlot> activated = RoundPlanner.BuildRoundPlan(allSlots, playerCount, config, out List<PoolPlanInfo> breakdown);

            // Aynı konumun (referans olarak) kaç kopyaya ihtiyacı olduğunu önce grupla, sonra o
            // konumda dikey bir yığın olarak spawnla. Gruplama, RoundPlanner'ın kopyaları sırayla mı
            // yoksa round-robin ile karışık mı ürettiğine bakılmaksızın doğru çalışır.
            foreach (IGrouping<CardSlot, CardSlot> group in activated.GroupBy(slot => slot))
            {
                CardSlot slot = group.Key;
                int total = group.Count();

                for (int copyIndex = 0; copyIndex < total; copyIndex++)
                {
                    Vector3 position = slot.Position + (Vector3.up * (copyIndex * config.CardStackSpacing));

                    // ÖNEMLİ: Scale, pickup SPAWN EDİLMEDEN ÖNCE ayarlanmalı. Zaten spawn edilmiş bir
                    // pickup'ta Pickup.Scale setter'ı içeride UnSpawn+Spawn yaparak ağı yeniden
                    // tetikliyor (Exiled.API.Features.Pickups.Pickup.Scale) — bu genelde çalışır ama
                    // "CreateAndSpawn" ile aynı anda art arda Spawn/UnSpawn/Spawn çağırmak yerine,
                    // objeyi önce oluşturup (Create), boyutunu ayarlayıp (henüz ağa hiç spawn
                    // edilmediği için Scale setter'ı düz bir SetWorldScale yapıyor, respawn YOK),
                    // sonra tek seferde spawn etmek (Spawn(position)) daha güvenilir: doğru boyut
                    // daha ilk ağ paketinde gidiyor.
                    Pickup pickup = Pickup.Create(config.CardItemType);
                    pickup.Scale = Vector3.one * config.CardScale;
                    pickup.Spawn(position);
                    pickup.GameObject.name = copyIndex == 0 ? $"{config.CardNamePrefix}{slot.DisplayName}" : $"{config.CardNamePrefix}{slot.DisplayName}_{copyIndex + 1}";
                    activeCardPickups[pickup] = slot;
                }
            }

            // Her tur, havuz başına hedef/kayıtlı-konum/aktif dökümünü HER ZAMAN logla (Debug açık
            // olsun olmasın) — bir havuz hedefe ulaşamadıysa (o havuzda hiç etiketsiz/uygun konum
            // yoksa) "!!! KONUM YOK !!!" ile işaretlenir. Normalde tek bir "kartkur" konumu, o
            // konumda kopyalanarak hedefi tek başına karşılar; bu uyarı SADECE o havuzda hiç uygun
            // konum kayıtlı değilse çıkar.
            string poolSummary = string.Join(" | ", breakdown.Select(p =>
                $"{p.Pool}: {p.ActivatedCount}/{p.Target} aktif (kayıtlı konum: {p.RegisteredCount}){(p.IsShortOnRegisteredSlots ? " !!! BU HAVUZDA UYGUN KONUM YOK, en az 1 'kartkur' kaydedin !!!" : string.Empty)}"));
            Log.Info($"[RoleSelector] {playerCount} oyuncu -> {activated.Count}/{allSlots.Count} kayıtlı kart slotu aktive edildi. {poolSummary}");
        }

        /// <summary>
        /// Seçim bitmeden kalan (alınmamış) kart pickup'larını yok eder ve takip sözlüğünü temizler.
        /// "mp unload" bizim manuel spawnladığımız pickup'ları temizlemez, bu yüzden ayrıca yapılır.
        /// </summary>
        private void DestroyRemainingCards()
        {
            foreach (Pickup pickup in activeCardPickups.Keys.ToList())
            {
                if (pickup != null)
                    pickup.Destroy();
            }

            activeCardPickups.Clear();
        }

        private List<(Player Player, Vector3 Point)> PrepareLobby()
        {
            List<Vector3> spawnPoints = Plugin.Instance.SpawnIds.All
                .SelectMany(ProjectMerBridge.FindByMerId)
                .Select(t => t.position)
                .ToList();

            if (spawnPoints.Count == 0)
                Log.Warn("[RoleSelector] 'spawnkur' ile kayıtlı hiçbir lobi spawn noktası şu an sahnede bulunamadı (hiç kaydedilmemiş olabilir ya da harita yüklenemedi). Oyuncular mevcut konumunda kalacak.");

            List<(Player, Vector3)> assignments = new();
            int index = 0;
            foreach (Player player in Player.List.ToList())
            {
                player.Role.Set(config.LobbyRole, SpawnReason.RoundStart);
                playersAwaitingChoice.Add(player);

                if (spawnPoints.Count > 0)
                {
                    assignments.Add((player, spawnPoints[index % spawnPoints.Count]));
                    index++;
                }
            }

            return assignments;
        }

        /// <summary>
        /// "bariyerkur" ile kayıtlı Id'lere sahip nesnelerin çarpışmasını kapatır.
        /// <para>
        /// ÖNEMLİ: SCP:SL'de bir Primitive'in oyuncuya çarpması, Unity'nin fiziksel
        /// <see cref="Collider"/> bileşeninin durumundan DEĞİL, ağ üzerinden senkronize edilen
        /// <see cref="PrimitiveObjectToy.NetworkPrimitiveFlags"/> SyncVar'ından (<see cref="PrimitiveFlags"/>)
        /// okunuyor. Bu yüzden ilk denemede yapıldığı gibi sadece <c>Collider.enabled = false</c>
        /// yapmak HİÇBİR ŞEYİ değiştirmiyordu — oyuncular hâlâ çarpışmaya devam ediyordu (bildirdiğiniz
        /// sorun). Doğru/kanıtlanmış yöntem — tam olarak <c>mp modify primitiveflags 0</c> komutunun elle
        /// yaptığı gibi — <see cref="PrimitiveObjectToy"/> bileşeninin <c>NetworkPrimitiveFlags</c>'ini
        /// <see cref="PrimitiveFlags.None"/> (0) yapmak: bu hem görünürlüğü hem çarpışmayı aynı anda,
        /// doğru şekilde ağa senkronize ederek kapatır. <see cref="PrimitiveObjectToy"/>, ProjectMER'in
        /// değil oyunun kendi (<c>AdminToys</c> namespace, Assembly-CSharp) tipi olduğundan, bu köprü
        /// (<see cref="ProjectMerBridge"/>) için reflection'a gerek yok — doğrudan referans veriliyor.
        /// </para>
        /// </summary>
        private void DisableBarriers()
        {
            int count = 0;
            foreach (string barrierId in Plugin.Instance.BarrierIds.All)
            {
                bool foundAny = false;
                foreach (Transform barrier in ProjectMerBridge.FindByMerId(barrierId))
                {
                    foundAny = true;
                    count++;

                    PrimitiveObjectToy primitiveToy = barrier.GetComponent<PrimitiveObjectToy>();
                    if (primitiveToy != null)
                    {
                        primitiveToy.NetworkPrimitiveFlags = PrimitiveFlags.None;
                        continue;
                    }

                    // Primitive değilse (ör. ileride başka bir Project Mer nesne tipi bariyer olarak
                    // kullanılırsa), en azından Unity Collider'ını kapatmayı dene.
                    foreach (Collider collider in barrier.GetComponentsInChildren<Collider>())
                        collider.enabled = false;
                }

                if (!foundAny)
                    Log.Warn($"[RoleSelector] 'bariyerkur' ile kayıtlı '{barrierId}' Id'li bariyer şu an sahnede bulunamadı (Id yanlış olabilir ya da harita yüklenemedi).");
            }

            if (config.Debug)
                Log.Debug($"[RoleSelector] {count}/{Plugin.Instance.BarrierIds.All.Count} kayıtlı bariyer devre dışı bırakıldı (PrimitiveFlags.None).");
        }

        private void FinishSelection()
        {
            Phase = SelectionPhase.Ended;
            Map.ClearBroadcasts();
            Map.Broadcast(6, config.BroadcastSelectionEnded);

            foreach (Player player in playersAwaitingChoice.ToList())
            {
                if (!player.IsConnected)
                    continue;

                player.Role.Set(config.FallbackRole, SpawnReason.RoundStart);
                player.ShowHint(config.HintFallbackAssigned, 6f);
            }

            foreach (KeyValuePair<Player, CardSlot> kvp in chosenRoles)
            {
                if (!kvp.Key.IsConnected)
                    continue;

                object profile = GetFlasetclassProfile(kvp.Value, out _);
                if (profile != null)
                {
                    ApplyFlasetclassProfile(kvp.Key, profile);
                }
                else
                {
                    kvp.Key.Role.Set(kvp.Value.Role, SpawnReason.RoundStart);
                }
            }

            playersAwaitingChoice.Clear();
            chosenRoles.Clear();
            DestroyRemainingCards();

            if (config.UnloadMapAfterSelection)
                ProjectMerBridge.UnloadMap(config.LobbyMapName);

            Phase = SelectionPhase.Idle;
        }

        private object GetFlasetclassProfile(CardSlot slot, out object unitsDict)
        {
            unitsDict = null;
            try
            {
                var flasetclassType = Type.GetType("Flasetclass.Flasetclass, Flasetclass");
                if (flasetclassType == null)
                    return null;

                var instanceProp = flasetclassType.GetProperty("Instance", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                var instance = instanceProp?.GetValue(null);
                if (instance == null)
                    return null;

                var configProp = flasetclassType.GetProperty("Config", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                var configObj = configProp?.GetValue(instance);
                if (configObj == null)
                    return null;

                var unitsProp = configObj.GetType().GetProperty("Units", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                unitsDict = unitsProp?.GetValue(configObj);

                if (unitsDict is System.Collections.IDictionary dict)
                {
                    // 1) Try lookup by Tag
                    if (!string.IsNullOrEmpty(slot.Tag))
                    {
                        foreach (System.Collections.DictionaryEntry entry in dict)
                        {
                            if (string.Equals(entry.Key as string, slot.Tag, StringComparison.OrdinalIgnoreCase))
                            {
                                return entry.Value;
                            }
                        }
                    }

                    // 2) Try lookup by mapped RoleTypeId
                    string mappedKey = slot.Role switch
                    {
                        RoleTypeId.Scientist => "bilim-insanı",
                        RoleTypeId.FacilityGuard => "tesis-görevlisi",
                        RoleTypeId.NtfCaptain => "tesis-komutanı",
                        RoleTypeId.NtfSergeant => "tesis-çavuşu",
                        _ => null
                    };

                    if (mappedKey != null)
                    {
                        foreach (System.Collections.DictionaryEntry entry in dict)
                        {
                            if (string.Equals(entry.Key as string, mappedKey, StringComparison.OrdinalIgnoreCase))
                            {
                                return entry.Value;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[RoleSelector] Flasetclass profile lookup failed: {ex}");
            }

            return null;
        }

        private void ApplyFlasetclassProfile(Player player, object profile)
        {
            try
            {
                var roleProp = profile.GetType().GetProperty("Role", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                var tagProp = profile.GetType().GetProperty("Tag", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                var infoProp = profile.GetType().GetProperty("Info", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                var itemsProp = profile.GetType().GetProperty("Items", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                var ammoProp = profile.GetType().GetProperty("Ammo", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

                RoleTypeId role = roleProp != null ? (RoleTypeId)roleProp.GetValue(profile) : RoleTypeId.None;
                string tag = tagProp?.GetValue(profile) as string;
                string info = infoProp?.GetValue(profile) as string;
                var items = itemsProp?.GetValue(profile) as List<ItemType>;
                var ammo = ammoProp?.GetValue(profile) as System.Collections.IDictionary;

                if (role != RoleTypeId.None)
                {
                    player.Role.Set(role, SpawnReason.RoundStart);
                }

                if (!string.IsNullOrEmpty(tag))
                {
                    player.CustomName = $"{tag} {player.Nickname}";
                }

                if (!string.IsNullOrEmpty(info))
                {
                    player.CustomInfo = info;
                }

                if (items != null)
                {
                    player.ResetInventory(items);
                }

                if (ammo != null)
                {
                    player.ClearAmmo();
                    foreach (System.Collections.DictionaryEntry entry in ammo)
                    {
                        try
                        {
                            AmmoType aType = (AmmoType)Convert.ChangeType(entry.Key, typeof(AmmoType));
                            ushort qty = (ushort)Convert.ChangeType(entry.Value, typeof(ushort));
                            player.SetAmmo(aType, qty);
                        }
                        catch { }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[RoleSelector] ApplyFlasetclassProfile failed for {player.Nickname}: {ex}");
            }
        }
    }
}
