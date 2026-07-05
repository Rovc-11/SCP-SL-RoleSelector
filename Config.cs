// -----------------------------------------------------------------------
// <copyright file="Config.cs" company="William">
// Role Selector - EXILED plugin config.
// </copyright>
// -----------------------------------------------------------------------

namespace RoleSelector
{
    using Exiled.API.Interfaces;
    using InventorySystem.Items;
    using PlayerRoles;

    /// <summary>
    /// Role Selector eklentisinin tüm ayarları.
    /// Bu dosya ilk açılışta sunucunun EXILED config klasöründe (config.yml içinde "role_selector" başlığı altında) otomatik oluşur.
    /// </summary>
    public sealed class Config : IConfig
    {
        /// <inheritdoc/>
        public bool IsEnabled { get; set; } = true;

        /// <inheritdoc/>
        public bool Debug { get; set; } = false;

        /// <summary>
        /// Gets or sets bir değer: <see langword="true"/> ise eklenti tur başında hiçbir otomatik rol
        /// seçim akışı BAŞLATMAZ (harita yüklenmez, kart/bariyer/spawn spawnlanmaz, roller atanmaz) —
        /// ama "kartkur"/"kartsil"/"kartlistele"/"bariyerkur"/"bariyersil"/"bariyerlistele"/"spawnkur"/
        /// "spawnsil"/"spawnlistele" komutları normal çalışmaya devam eder. Yani eklenti "aktif"tir
        /// (komutlar çalışır) ama round akışını tetiklemez. Haritayı Project Mer ile elle
        /// yükleyip/kaydedip, kart/bariyer/spawn slotlarını rahatça (round ortasında sürekli
        /// kesintiye uğramadan) kurmak için açın; kurulum bitince tekrar <see langword="false"/> yapın.
        /// </summary>
        public bool ConfigMode { get; set; } = false;

        /// <summary>
        /// Gets or sets Project Mer'de "mp save {isim}" ile kaydedilmiş rol seçim lobisinin harita adı.
        /// Tur başında bu harita "mp load {isim}" komutuyla otomatik yüklenir.
        /// </summary>
        public string LobbyMapName { get; set; } = "role_selection_lobby";

        /// <summary>
        /// Gets or sets seçim bitince lobi haritasının kaldırılıp kaldırılmayacağı ("mp unload").
        /// </summary>
        public bool UnloadMapAfterSelection { get; set; } = true;

        /// <summary>
        /// Gets or sets oyuncuların lobide, kartlar açılana kadar bulunacağı geçici rol.
        /// Bu rolün fiziksel bir bedeni olmalı ki görünmez bariyerlere çarpabilsin (Spectator olmaz, noclip'li).
        /// </summary>
        public RoleTypeId LobbyRole { get; set; } = RoleTypeId.Tutorial;

        /// <summary>
        /// Gets or sets kart seçtikten sonra oyuncunun bekleme rolü (izleyici).
        /// </summary>
        public RoleTypeId WaitingRole { get; set; } = RoleTypeId.Spectator;

        /// <summary>
        /// Gets or sets seçim bitiminde hiç kart almamış oyunculara verilecek rol.
        /// </summary>
        public RoleTypeId FallbackRole { get; set; } = RoleTypeId.ClassD;

        /// <summary>
        /// Gets or sets oyuncuların lobide toplanması için beklenecek süre (saniye).
        /// Bu süre boyunca hiçbir şey olmaz, sadece oyuncuların haritaya düşmesi beklenir.
        /// </summary>
        public float GatherDuration { get; set; } = 35f;

        /// <summary>
        /// Gets or sets toplanma bitince, kartların açılmasına kadar geri sayılan süre (saniye).
        /// "Rol seçimine son X saniye" anonsu bu süre boyunca gösterilir.
        /// </summary>
        public float PreSelectionCountdown { get; set; } = 15f;

        /// <summary>
        /// Gets or sets bariyerler kalktıktan sonra, oyuncuların fiilen kart alabileceği toplam süre (saniye).
        /// Bu süre bitince kart almayanlara FallbackRole atanır ve harita kaldırılır.
        /// </summary>
        public float SelectionDuration { get; set; } = 60f;

        /// <summary>
        /// Gets or sets otomatik spawnlanan kart pickup'larının GameObject adına verilecek ön ek
        /// (sadece admin/log tarafında okunabilirlik için; tespit artık isimden değil, plugin'in
        /// kendi spawnladığı Pickup referansından yapılıyor).
        /// </summary>
        public string CardNamePrefix { get; set; } = "RoleCard_";

        /// <summary>
        /// Gets or sets "kartkur" ile kaydedilmiş her slotta spawnlanacak fiziksel item türü
        /// (görsel/etkileşim amaçlı; rolle bir ilgisi yoktur, sadece E ile alınabilen bir "kart" görünümüdür).
        /// </summary>
        public ItemType CardItemType { get; set; } = ItemType.KeycardJanitor;

        /// <summary>
        /// Gets or sets bir "kartkur" konumunda birden fazla kart kopyası spawnlanınca (bkz. bölüm 3,
        /// RoundPlanner), kopyaların birbirinden ne kadar (metre) uzağa, GENİŞ bir alana (ızgara
        /// düzeninde, dikey değil yatay) yayılacağını belirler. Örn. bir konumda 12 DClass kartı
        /// gerekiyorsa, hepsi tek noktada üst üste yığılmaz — bu değer kadar aralıklarla düzlemsel
        /// bir ızgaraya yayılır, oyuncular rahatça birbirinden ayrı ayrı alabilir.
        /// </summary>
        public float CardSpreadSpacing { get; set; } = 0.75f;

        /// <summary>
        /// Gets or sets bir oyuncu kart aldıktan sonra kartı fiziksel olarak yok edilsin mi (true) yoksa
        /// sadece pasif hale mi getirilsin (false, diğer oyuncular hâlâ görür ama alamaz).
        /// </summary>
        public bool DestroyCardOnPick { get; set; } = true;

        /// <summary>
        /// Gets or sets kart alan bir oyuncu, seçim süresi bitene kadar rolünü/kartını değiştirebilsin mi.
        /// Hayır ise seçim kesindir, oyuncu tekrar kart alamaz.
        /// </summary>
        public bool AllowChangingCard { get; set; } = false;

        // ---- Kart kotaları (kartkur ile kayıtlı slotlardan, her tur kaç tanesinin aktif olacağı) ----

        /// <summary>
        /// Gets or sets araştırma (Bilim İnsanları) havuzu için "kaç oyuncuya 1 kart" oranı.
        /// Hedef = max(ResearchMinimum, ceil(OyuncuSayısı / ResearchPlayersPerCard)).
        /// </summary>
        public int ResearchPlayersPerCard { get; set; } = 9;

        /// <summary>
        /// Gets or sets araştırma havuzunun minimum toplam kart sayısı (Baş B.İ. dahil).
        /// </summary>
        public int ResearchMinimum { get; set; } = 2;

        /// <summary>
        /// Gets or sets güvenlik havuzu için "kaç oyuncuya 1 kart" oranı.
        /// NOT: İstek metninde "1/4" yazıyordu ama verilen 45 kişilik örnekte (15 güvenlik / 45 oyuncu)
        /// oran aslında 1/3'e denk geliyor; örnekle tutarlı olsun diye varsayılan 3 yapıldı.
        /// Yanlış yorumladıysam bu değeri 4 yapman yeterli.
        /// </summary>
        public int SecurityPlayersPerCard { get; set; } = 3;

        /// <summary>
        /// Gets or sets güvenlik havuzunun minimum toplam kart sayısı (Çavuş dahil).
        /// </summary>
        public int SecurityMinimum { get; set; } = 2;

        /// <summary>
        /// Gets or sets d Sınıfı havuzunun (Aşçı + Hademe + normal D Sınıfı) minimum toplam kart sayısı.
        /// </summary>
        public int DClassMinimum { get; set; } = 3;

        /// <summary>
        /// Gets or sets araştırma havuzu hedefi 4 veya üzeriyse spawnlanacak SCP sayısı.
        /// </summary>
        public int ScpCountTier4Plus { get; set; } = 3;

        /// <summary>
        /// Gets or sets araştırma havuzu hedefi tam 3 ise spawnlanacak SCP sayısı.
        /// </summary>
        public int ScpCountTier3 { get; set; } = 2;

        /// <summary>
        /// Gets or sets araştırma havuzu hedefi 2 veya altıysa spawnlanacak SCP sayısı (aynı zamanda "en az 1 SCP" kuralını karşılar).
        /// </summary>
        public int ScpCountTier2OrLess { get; set; } = 1;

        // ---- Mesajlar ----

        public string BroadcastGathering { get; set; } = "<b>Rol Seçimi</b>\nOyuncular toplanıyor... Seçim birazdan başlayacak.";

        public string BroadcastCountdownFormat { get; set; } = "<b>Rol Seçimi</b>\nKartlar <color=yellow>{0}</color> saniye içinde açılıyor!";

        public string BroadcastSelectionOpenFormat { get; set; } = "<b>Rol Seçimi Başladı!</b>\nRolünün kartına gidip <color=yellow>E</color> tuşuna bas. Kalan süre: {0} saniye.";

        public string HintCardPicked { get; set; } = "Rolünü seçtin: <b>{0}</b>\nDiğer oyuncuların seçimi bitene kadar bekle.";

        public string BroadcastSelectionEnded { get; set; } = "<b>Rol Seçimi Bitti!</b>\nHerkes rolüyle atandı.";

        public string HintFallbackAssigned { get; set; } = "Kart seçmediğin için D Sınıfı olarak atandın.";
    }
}
