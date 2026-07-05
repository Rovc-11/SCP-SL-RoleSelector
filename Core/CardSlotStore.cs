// -----------------------------------------------------------------------
// <copyright file="CardSlotStore.cs" company="William">
// Role Selector - card slot persistence.
// </copyright>
// -----------------------------------------------------------------------

namespace RoleSelector.Core
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    using Exiled.API.Features;
    using PlayerRoles;
    using UnityEngine;
    using YamlDotNet.Serialization;
    using YamlDotNet.Serialization.NamingConventions;

    /// <summary>
    /// "kartkur"/"kartsil"/"kartlistele" komutlarıyla yönetilen kart slotlarının diskteki
    /// kalıcı listesi. Sunucu her yeniden başladığında aynı slotlar geri yüklenir.
    /// </summary>
    public sealed class CardSlotStore
    {
        private static readonly ISerializer Serializer = new SerializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .Build();

        private static readonly IDeserializer Deserializer = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();

        private readonly string filePath;
        private readonly List<CardSlot> slots = new();
        private int nextId = 1;

        /// <summary>
        /// Initializes a new instance of the <see cref="CardSlotStore"/> class.
        /// </summary>
        /// <param name="pluginConfigFolder">Slotların kaydedileceği klasör (ör. Exiled Configs/RoleSelector).</param>
        public CardSlotStore(string pluginConfigFolder)
        {
            Directory.CreateDirectory(pluginConfigFolder);
            filePath = Path.Combine(pluginConfigFolder, "card_slots.yml");
            Load();
        }

        /// <summary>
        /// Gets kayıtlı tüm kart slotları.
        /// </summary>
        public IReadOnlyList<CardSlot> All => slots;

        /// <summary>
        /// Yeni bir kart slotu ekler ve diske kaydeder.
        /// </summary>
        /// <param name="role">Kartın vereceği rol.</param>
        /// <param name="tag">İsteğe bağlı özel isim (Aşçı/Hademe/Baş B.İ./Çavuş vb.).</param>
        /// <param name="position">Kartın spawn olacağı konum.</param>
        /// <returns>Oluşturulan <see cref="CardSlot"/>.</returns>
        public CardSlot Add(RoleTypeId role, string tag, Vector3 position)
        {
            CardSlot slot = new()
            {
                Id = nextId++,
                Role = role,
                Tag = tag ?? string.Empty,
                Position = position,
            };

            slots.Add(slot);
            Save();
            return slot;
        }

        /// <summary>
        /// Bir kart slotunu kimliğine göre siler.
        /// </summary>
        /// <param name="id">Silinecek slotun kimliği.</param>
        /// <returns>Bir slot silindiyse <see langword="true"/>.</returns>
        public bool Remove(int id)
        {
            int removedCount = slots.RemoveAll(s => s.Id == id);
            if (removedCount > 0)
                Save();

            return removedCount > 0;
        }

        private void Load()
        {
            if (!File.Exists(filePath))
                return;

            try
            {
                string yaml = File.ReadAllText(filePath);
                List<CardSlot> loaded = Deserializer.Deserialize<List<CardSlot>>(yaml) ?? new List<CardSlot>();
                slots.Clear();
                slots.AddRange(loaded);
                nextId = slots.Count == 0 ? 1 : slots.Max(s => s.Id) + 1;
            }
            catch (Exception exception)
            {
                Log.Error($"[RoleSelector] card_slots.yml okunamadı: {exception.Message}");
            }
        }

        private void Save()
        {
            try
            {
                File.WriteAllText(filePath, Serializer.Serialize(slots));
            }
            catch (Exception exception)
            {
                Log.Error($"[RoleSelector] card_slots.yml yazılamadı: {exception.Message}");
            }
        }
    }
}
