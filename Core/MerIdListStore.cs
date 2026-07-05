// -----------------------------------------------------------------------
// <copyright file="MerIdListStore.cs" company="William">
// Role Selector - persisted list of Project Mer object Ids.
// </copyright>
// -----------------------------------------------------------------------

namespace RoleSelector.Core
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    using Exiled.API.Features;
    using YamlDotNet.Serialization;
    using YamlDotNet.Serialization.NamingConventions;

    /// <summary>
    /// "bariyerkur"/"bariyersil"/"bariyerlistele" ve "spawnkur"/"spawnsil"/"spawnlistele"
    /// komutlarıyla yönetilen, Project Mer nesne Id'lerinden (<c>MapEditorObject.Id</c>, bkz.
    /// <see cref="ProjectMerBridge"/>) oluşan basit, kalıcı bir liste. İki ayrı örneği vardır: biri
    /// bariyer Id'leri, diğeri lobi spawn Id'leri için (bkz. Plugin.cs — <c>BarrierIds</c>/<c>SpawnIds</c>).
    /// Sunucu yeniden başlasa da kaydedilen Id'ler kaybolmaz.
    /// </summary>
    public sealed class MerIdListStore
    {
        private static readonly ISerializer Serializer = new SerializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .Build();

        private static readonly IDeserializer Deserializer = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();

        private readonly string filePath;
        private readonly List<string> ids = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="MerIdListStore"/> class.
        /// </summary>
        /// <param name="pluginConfigFolder">Dosyanın kaydedileceği klasör (ör. Exiled Configs/RoleSelector).</param>
        /// <param name="fileName">Dosya adı (ör. "barrier_ids.yml", "spawn_ids.yml").</param>
        public MerIdListStore(string pluginConfigFolder, string fileName)
        {
            Directory.CreateDirectory(pluginConfigFolder);
            filePath = Path.Combine(pluginConfigFolder, fileName);
            Load();
        }

        /// <summary>
        /// Gets kayıtlı tüm Id'ler.
        /// </summary>
        public IReadOnlyList<string> All => ids;

        /// <summary>
        /// Yeni bir Id ekler (zaten kayıtlıysa hiçbir şey yapmaz).
        /// </summary>
        /// <param name="id">Eklenecek Project Mer nesne Id'si.</param>
        /// <returns>Gerçekten eklendiyse <see langword="true"/>; zaten kayıtlıysa <see langword="false"/>.</returns>
        public bool Add(string id)
        {
            if (ids.Any(existing => string.Equals(existing, id, StringComparison.OrdinalIgnoreCase)))
                return false;

            ids.Add(id);
            Save();
            return true;
        }

        /// <summary>
        /// Bir Id'yi siler.
        /// </summary>
        /// <param name="id">Silinecek Id.</param>
        /// <returns>Bir kayıt silindiyse <see langword="true"/>.</returns>
        public bool Remove(string id)
        {
            int removedCount = ids.RemoveAll(existing => string.Equals(existing, id, StringComparison.OrdinalIgnoreCase));
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
                List<string> loaded = Deserializer.Deserialize<List<string>>(yaml) ?? new List<string>();
                ids.Clear();
                ids.AddRange(loaded);
            }
            catch (Exception exception)
            {
                Log.Error($"[RoleSelector] '{Path.GetFileName(filePath)}' okunamadı: {exception.Message}");
            }
        }

        private void Save()
        {
            try
            {
                File.WriteAllText(filePath, Serializer.Serialize(ids));
            }
            catch (Exception exception)
            {
                Log.Error($"[RoleSelector] '{Path.GetFileName(filePath)}' yazılamadı: {exception.Message}");
            }
        }
    }
}
