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
    /// "kartkur" ile kayıtlı kart slotlarından, o turki oyuncu sayısına göre hangilerinin bu
    /// tur gerçekten kart olarak spawn olacağını hesaplar. Havuz başına mantık:
    /// <list type="bullet">
    /// <item><b>Free</b>: kota yok, kayıtlı her slot her zaman aktif (Joker, Arkaplan GameAdmin, İzleyici vb.).</item>
    /// <item><b>Research</b>: hedef = max(ResearchMinimum, ceil(N / ResearchPlayersPerCard)).</item>
    /// <item><b>Security</b>: hedef = max(SecurityMinimum, ceil(N / SecurityPlayersPerCard)).</item>
    /// <item><b>Scp</b>: Research hedefinin büyüklüğüne göre kademeli (3/2/1), aynı SCP türünden asla 2 tane seçilmez.</item>
    /// <item><b>DClass</b>: hedef = max(DClassMinimum, N - (diğer tüm havuzlarda o an aktif olan kart sayısı)).</item>
    /// </list>
    /// Her havuzda, boş olmayan bir "Tag" (Aşçı/Hademe/Baş B.İ./Çavuş gibi) taşıyan slotlar özel/tekil
    /// sayılır: aynı Tag'e sahip birden fazla slot varsa bile o Tag'den sadece 1 tanesi aktive edilir
    /// ve hedeften düşülür; geri kalan hedef, Tag'siz ("normal") slotlardan rastgele doldurulur.
    /// </summary>
    public static class RoundPlanner
    {
        /// <summary>
        /// Bu tur aktif olacak (yani gerçekten kart olarak spawn edilecek) slotları hesaplar.
        /// </summary>
        /// <param name="allSlots">"kartkur" ile kayıtlı tüm slotlar.</param>
        /// <param name="playerCount">Kartlar spawn olurkenki toplam oyuncu sayısı.</param>
        /// <param name="config">Kota ayarlarını içeren config.</param>
        /// <returns>Bu tur aktif edilecek slotların listesi.</returns>
        public static List<CardSlot> BuildRoundPlan(IReadOnlyList<CardSlot> allSlots, int playerCount, Config config)
        {
            Random random = new();
            List<CardSlot> activated = new();

            // 1) Serbest kartlar: formül yok, kayıtlı her şey her zaman spawnlanır.
            activated.AddRange(allSlots.Where(s => s.Pool == CardPool.Free));

            // 2) Araştırma (Bilim İnsanları) havuzu.
            int researchTarget = Math.Max(config.ResearchMinimum, CeilDiv(playerCount, config.ResearchPlayersPerCard));
            activated.AddRange(ActivatePool(allSlots, CardPool.Research, researchTarget, random));

            // 3) Güvenlik havuzu.
            int securityTarget = Math.Max(config.SecurityMinimum, CeilDiv(playerCount, config.SecurityPlayersPerCard));
            activated.AddRange(ActivatePool(allSlots, CardPool.Security, securityTarget, random));

            // 4) SCP havuzu: araştırma havuzunun büyüklüğüne bağlı kademeli sayı.
            int scpTarget = researchTarget >= 4 ? config.ScpCountTier4Plus
                : researchTarget == 3 ? config.ScpCountTier3
                : config.ScpCountTier2OrLess;
            activated.AddRange(ActivateScpPool(allSlots, scpTarget, random));

            // 5) D Sınıfı havuzu: kalan oyuncu sayısı kadar (diğer tüm havuzlar -Free dahil- düşülerek).
            int dClassTarget = Math.Max(config.DClassMinimum, playerCount - activated.Count);
            activated.AddRange(ActivatePool(allSlots, CardPool.DClass, dClassTarget, random));

            return activated;
        }

        private static int CeilDiv(int players, int divisor) =>
            divisor <= 0 ? players : (int)Math.Ceiling(players / (double)divisor);

        /// <summary>
        /// Genel havuz aktivasyonu: önce her farklı Tag'den 1 tane, sonra kalan hedefi
        /// Tag'siz (normal) slotlardan rastgele doldurur.
        /// </summary>
        private static List<CardSlot> ActivatePool(IReadOnlyList<CardSlot> allSlots, CardPool pool, int target, Random random)
        {
            List<CardSlot> poolSlots = allSlots.Where(s => s.Pool == pool).ToList();
            List<CardSlot> result = new();

            foreach (IGrouping<string, CardSlot> taggedGroup in poolSlots
                .Where(s => !string.IsNullOrEmpty(s.Tag))
                .GroupBy(s => s.Tag))
            {
                result.Add(PickRandom(taggedGroup.ToList(), random));
            }

            int remaining = Math.Max(0, target - result.Count);
            List<CardSlot> regularSlots = poolSlots.Where(s => string.IsNullOrEmpty(s.Tag)).ToList();
            result.AddRange(PickRandomSubset(regularSlots, remaining, random));

            return result;
        }

        /// <summary>
        /// SCP havuzu aktivasyonu: aynı RoleTypeId'den asla 2 tane aktive edilmez.
        /// </summary>
        private static List<CardSlot> ActivateScpPool(IReadOnlyList<CardSlot> allSlots, int target, Random random)
        {
            List<CardSlot> scpSlots = allSlots.Where(s => s.Pool == CardPool.Scp).ToList();

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
