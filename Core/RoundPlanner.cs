// -----------------------------------------------------------------------
// <copyright file="RoundPlanner.cs" company="William">
// Role Selector - per-round card quota calculation.
// </copyright>
// -----------------------------------------------------------------------

namespace RoleSelector.Core
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Tek bir havuz için bu tur hesaplanan hedef/kayıtlı/aktif sayılarını taşır — sadece admine
    /// "neden az kart çıktı" sorusuna anında cevap verebilmek için (bkz. <see cref="RoundPlanner.BuildRoundPlan"/>).
    /// Not: net48'te C# 9 "record"/"init" kullanılmadı (ekstra bir polyfill gerektirmesin diye),
    /// bu yüzden sıradan salt-okunur bir sınıf olarak yazıldı.
    /// </summary>
    public sealed class PoolPlanInfo
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PoolPlanInfo"/> class.
        /// </summary>
        public PoolPlanInfo(CardPool pool, int target, int registeredCount, int activatedCount)
        {
            Pool = pool;
            Target = target;
            RegisteredCount = registeredCount;
            ActivatedCount = activatedCount;
        }

        /// <summary>Gets havuz.</summary>
        public CardPool Pool { get; }

        /// <summary>Gets bu tur için hesaplanan hedef (kaç kart aktif olması gerektiği).</summary>
        public int Target { get; }

        /// <summary>Gets bu havuzda "kartkur" ile kayıtlı TOPLAM KONUM sayısı (etiketli + normal).</summary>
        public int RegisteredCount { get; }

        /// <summary>Gets gerçekten aktive edilen (spawn edilecek) kart sayısı (bir konumdan birden fazla kopya spawnlanabildiği için bu, RegisteredCount'tan büyük olabilir).</summary>
        public int ActivatedCount { get; }

        /// <summary>
        /// Gets a value indicating whether hedefe ulaşılamadı mı — bu artık SADECE bu havuzda o payı
        /// karşılayacak HİÇBİR normal (etiketsiz) konum kayıtlı olmadığında olur (etiketli/tekil
        /// paylar için konum sayısı önemli değildir, tek bir konum yeterlidir).
        /// </summary>
        public bool IsShortOnRegisteredSlots => ActivatedCount < Target;
    }

    /// <summary>
    /// "kartkur" ile kayıtlı KONUMLARDAN, o turki oyuncu sayısına göre hangi roldeki kaç kartın
    /// nerede spawnlanacağını hesaplar.
    /// <para>
    /// <b>Önemli mantık:</b> Bir "kartkur &lt;Rol&gt;" kaydı artık "1 kart" değil, "o rolün kartlarının
    /// spawnlanacağı bir KONUM" anlamına gelir. Bir havuzun (ör. Güvenlik) o tur ihtiyacı olan kart
    /// sayısı (hedef) hesaplanır hesaplanmaz, o havuzdaki etiketsiz ("normal") konum(lar)a — admin
    /// sadece 1 tane kaydetmiş olsa bile — ihtiyaç kadar kart KOPYASI (aynı konumda, küçük bir yığın
    /// gibi) spawnlanır. Yani <b>bir rol için 1 kere "kartkur" yeterlidir</b>; o konumda gerektiği
    /// kadar o rolün kartı çıkar. Birden fazla konum kaydedilirse (ör. güvenlik kartlarını 2 farklı
    /// masaya dağıtmak isterseniz), ihtiyaç sırayla (round-robin) aralarında paylaştırılır.
    /// </para>
    /// Havuz başına hedef formülleri:
    /// <list type="bullet">
    /// <item><b>Free</b>: kota yok, kayıtlı her konumdan tam 1 kart (Joker, Arkaplan GameAdmin, İzleyici vb. — bunlar zaten kendi başına tekil kabul edilir).</item>
    /// <item><b>Research</b>: hedef = max(ResearchMinimum, ceil(N / ResearchPlayersPerCard)).</item>
    /// <item><b>Security</b>: hedef = max(SecurityMinimum, ceil(N / SecurityPlayersPerCard)).</item>
    /// <item><b>Scp</b>: Research hedefinin büyüklüğüne göre kademeli (3/2/1); aynı SCP türünden ASLA 2 kart aktive edilmez (kopyalama uygulanmaz — her SCP türü kendi başına tekildir).</item>
    /// <item><b>DClass</b>: hedef = max(DClassMinimum, N - (diğer tüm havuzlarda o an aktif olan kart sayısı)).</item>
    /// </list>
    /// Her havuzda, boş olmayan bir "Tag" (Aşçı/Hademe/Baş B.İ./Çavuş gibi) taşıyan konumlar özel/tekil
    /// sayılır: bunlardan HİÇBİR ZAMAN kopya spawnlanmaz (her zaman tam 1 kart) — aynı Tag'e sahip
    /// birden fazla konum varsa bile o Tag'den sadece 1 tanesi aktive edilir. Bir Tag hiç kayıtlı
    /// DEĞİLSE (ör. hiç "kartkur ClassD Asci" çalıştırılmamışsa), o Tag için ayrılmış pay basitçe
    /// boşta kalmaz — hedeften düşülmeden, geri kalan hedef aynı havuzun etiketsiz ("normal")
    /// konumlarına (kopyalanarak) verilir (ör. DClass havuzunda Aşçı yoksa, o kişi normal ClassD
    /// kartına gider). Eğer bir havuzda hiç etiketsiz konum kayıtlı değilse (ve etiketliler de hedefi
    /// karşılamıyorsa), o havuz için hedefe ulaşılamaz — kart alamayan oyuncular round sonunda zaten
    /// <c>Config.FallbackRole</c>'a (varsayılan ClassD) düşer, yani kimse "havada" kalmaz.
    /// </summary>
    public static class RoundPlanner
    {
        /// <summary>
        /// Bu tur aktif olacak (yani gerçekten kart olarak spawn edilecek) slotları hesaplar.
        /// </summary>
        /// <param name="allSlots">"kartkur" ile kayıtlı tüm slotlar.</param>
        /// <param name="playerCount">Kartlar spawn olurkenki toplam oyuncu sayısı.</param>
        /// <param name="config">Kota ayarlarını içeren config.</param>
        /// <param name="breakdown">Havuz başına hedef/kayıtlı/aktif sayılarının dökümü (sadece log/diagnostik amaçlı).</param>
        /// <returns>Bu tur aktif edilecek slotların listesi.</returns>
        public static List<CardSlot> BuildRoundPlan(IReadOnlyList<CardSlot> allSlots, int playerCount, Config config, out List<PoolPlanInfo> breakdown)
        {
            Random random = new();
            List<CardSlot> activated = new();
            breakdown = new List<PoolPlanInfo>();

            // 1) Serbest kartlar: formül yok, kayıtlı her şey her zaman spawnlanır.
            List<CardSlot> freeSlots = allSlots.Where(s => s.Pool == CardPool.Free).ToList();
            activated.AddRange(freeSlots);
            breakdown.Add(new PoolPlanInfo(CardPool.Free, freeSlots.Count, freeSlots.Count, freeSlots.Count));

            // 2) Araştırma (Bilim İnsanları) havuzu.
            int researchTarget = Math.Max(config.ResearchMinimum, CeilDiv(playerCount, config.ResearchPlayersPerCard));
            List<CardSlot> researchActivated = ActivatePool(allSlots, CardPool.Research, researchTarget, random, out int researchRegistered);
            activated.AddRange(researchActivated);
            breakdown.Add(new PoolPlanInfo(CardPool.Research, researchTarget, researchRegistered, researchActivated.Count));

            // 3) Güvenlik havuzu.
            int securityTarget = Math.Max(config.SecurityMinimum, CeilDiv(playerCount, config.SecurityPlayersPerCard));
            List<CardSlot> securityActivated = ActivatePool(allSlots, CardPool.Security, securityTarget, random, out int securityRegistered);
            activated.AddRange(securityActivated);
            breakdown.Add(new PoolPlanInfo(CardPool.Security, securityTarget, securityRegistered, securityActivated.Count));

            // 4) SCP havuzu: araştırma havuzunun büyüklüğüne bağlı kademeli sayı.
            int scpTarget = researchTarget >= 4 ? config.ScpCountTier4Plus
                : researchTarget == 3 ? config.ScpCountTier3
                : config.ScpCountTier2OrLess;
            List<CardSlot> scpActivated = ActivateScpPool(allSlots, scpTarget, random, out int scpRegistered);
            activated.AddRange(scpActivated);
            breakdown.Add(new PoolPlanInfo(CardPool.Scp, scpTarget, scpRegistered, scpActivated.Count));

            // 5) D Sınıfı havuzu: kalan oyuncu sayısı kadar (diğer tüm havuzlar -Free dahil- düşülerek).
            int dClassTarget = Math.Max(config.DClassMinimum, playerCount - activated.Count);
            List<CardSlot> dClassActivated = ActivatePool(allSlots, CardPool.DClass, dClassTarget, random, out int dClassRegistered);
            activated.AddRange(dClassActivated);
            breakdown.Add(new PoolPlanInfo(CardPool.DClass, dClassTarget, dClassRegistered, dClassActivated.Count));

            return activated;
        }

        private static int CeilDiv(int players, int divisor) =>
            divisor <= 0 ? players : (int)Math.Ceiling(players / (double)divisor);

        /// <summary>
        /// Genel havuz aktivasyonu: önce her farklı Tag'den en fazla 1 tane (hiç kopyalanmaz, hedefi
        /// aşmayacak şekilde — etiket sayısı hedeften fazlaysa rastgele bir alt küme seçilir), sonra
        /// kalan hedef, etiketsiz ("normal") konumlar arasında SIRAYLA (round-robin) dağıtılarak
        /// KOPYALANARAK doldurulur. Dönen listede aynı <see cref="CardSlot"/> birden fazla kez
        /// geçebilir — her tekrar, o konumda spawnlanacak bir kart kopyasını temsil eder (bkz.
        /// <see cref="SelectionManager.SpawnCards"/>, aynı konumdaki kopyalar küçük bir ofsetle
        /// üst üste spawnlanır).
        /// </summary>
        private static List<CardSlot> ActivatePool(IReadOnlyList<CardSlot> allSlots, CardPool pool, int target, Random random, out int registeredCount)
        {
            List<CardSlot> poolSlots = allSlots.Where(s => s.Pool == pool).ToList();
            registeredCount = poolSlots.Count;

            // Etiketli (Aşçı/Hademe gibi) konumlar hiçbir zaman kopyalanmaz — her biri tam 1 kart.
            List<CardSlot> taggedPicks = poolSlots
                .Where(s => !string.IsNullOrEmpty(s.Tag))
                .GroupBy(s => s.Tag)
                .Select(taggedGroup => PickRandom(taggedGroup.ToList(), random))
                .ToList();
            List<CardSlot> result = PickRandomSubset(taggedPicks, Math.Min(target, taggedPicks.Count), random);

            // Kalan hedef, etiketsiz konumlara round-robin ile KOPYALANARAK dağıtılır: admin sadece
            // 1 konum kaydetmiş olsa bile, o konum tek başına ihtiyacın tamamını karşılar (kopya
            // spawnlanır); birden fazla konum varsa yük aralarında sırayla paylaştırılır.
            int remaining = Math.Max(0, target - result.Count);
            List<CardSlot> regularSlots = poolSlots.Where(s => string.IsNullOrEmpty(s.Tag)).ToList();
            if (regularSlots.Count > 0)
            {
                for (int i = 0; i < remaining; i++)
                    result.Add(regularSlots[i % regularSlots.Count]);
            }

            return result;
        }

        /// <summary>
        /// SCP havuzu aktivasyonu: aynı RoleTypeId'den asla 2 tane aktive edilmez.
        /// </summary>
        private static List<CardSlot> ActivateScpPool(IReadOnlyList<CardSlot> allSlots, int target, Random random, out int registeredCount)
        {
            List<CardSlot> scpSlots = allSlots.Where(s => s.Pool == CardPool.Scp).ToList();
            registeredCount = scpSlots.Count;

            List<CardSlot> distinctCandidates = scpSlots
                .GroupBy(s => s.Role)
                .Select(group => PickRandom(group.ToList(), random))
                .ToList();

            return PickRandomSubset(distinctCandidates, Math.Min(target, distinctCandidates.Count), random);
        }

        private static CardSlot PickRandom(List<CardSlot> candidates, Random random) => candidates[random.Next(candidates.Count)];

        private static List<CardSlot> PickRandomSubset(List<CardSlot> source, int count, Random random)
        {
            if (count >= source.Count)
                return new List<CardSlot>(source);

            return source.OrderBy(_ => random.Next()).Take(Math.Max(0, count)).ToList();
        }
    }
}
