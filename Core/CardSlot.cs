// -----------------------------------------------------------------------
// <copyright file="CardSlot.cs" company="William">
// Role Selector - persisted card slot definition.
// </copyright>
// -----------------------------------------------------------------------

namespace RoleSelector.Core
{
    using Exiled.API.Extensions;
    using PlayerRoles;
    using UnityEngine;
    using YamlDotNet.Serialization;

    /// <summary>
    /// "kartkur" komutuyla kaydedilmiş, tek bir kart konumunu temsil eder.
    /// </summary>
    public sealed class CardSlot
    {
        /// <summary>
        /// Gets or sets kalıcı, otomatik atanan kimlik (kartsil ile silmek için kullanılır).
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets bu kart alındığında oyuncuya verilecek rol.
        /// </summary>
        public RoleTypeId Role { get; set; }

        /// <summary>
        /// Gets or sets isteğe bağlı özel isim (ör. "Asci", "Hademe", "BasBI", "Cavus").
        /// Boş değilse, bu slot kendi havuzunda "sabit/tekil" bir alt rol sayılır: aynı Tag'e
        /// sahip birden fazla slot kaydedilse bile, o tur için bu Tag'den sadece 1 tanesi
        /// rastgele seçilip aktif edilir (ör. tek bir Aşçı, tek bir Baş Bilim İnsanı gibi).
        /// Boşsa, bu slot ilgili havuzun "normal/dinamik sayılı" üyelerinden biridir.
        /// </summary>
        public string Tag { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the X ekseni konumu.
        /// </summary>
        public float PositionX { get; set; }

        /// <summary>
        /// Gets or sets the Y ekseni konumu.
        /// </summary>
        public float PositionY { get; set; }

        /// <summary>
        /// Gets or sets the Z ekseni konumu.
        /// </summary>
        public float PositionZ { get; set; }

        /// <summary>
        /// Gets or sets konumu <see cref="Vector3"/> olarak (YAML'a X/Y/Z olarak yazılır).
        /// </summary>
        [YamlIgnore]
        public Vector3 Position
        {
            get => new(PositionX, PositionY, PositionZ);
            set
            {
                PositionX = value.x;
                PositionY = value.y;
                PositionZ = value.z;
            }
        }

        /// <summary>
        /// Gets bu slotun ait olduğu kota havuzu; <see cref="Role"/>'ün vanilla Team'inden hesaplanır.
        /// </summary>
        [YamlIgnore]
        public CardPool Pool => ResolvePool(Role);

        /// <summary>
        /// Gets admin panelinde / loglarda gösterilecek okunabilir isim.
        /// </summary>
        [YamlIgnore]
        public string DisplayName => string.IsNullOrEmpty(Tag) ? Role.ToString() : $"{Role}_{Tag}";

        /// <summary>
        /// Bir <see cref="RoleTypeId"/>'nin hangi kota havuzuna ait olduğunu, oyunun kendi
        /// <see cref="Team"/> sınıflandırmasından çıkarır. Böylece "hangi rol hangi bölüme
        /// ait" sorusu için ayrıca bir eşleme tablosu tutmaya gerek kalmaz.
        /// </summary>
        /// <param name="role">Kontrol edilecek rol.</param>
        /// <returns>Çözümlenen <see cref="CardPool"/>.</returns>
        public static CardPool ResolvePool(RoleTypeId role) => Exiled.API.Extensions.RoleExtensions.GetTeam(role) switch
        {
            Team.ClassD => CardPool.DClass,
            Team.Scientists => CardPool.Research,
            Team.SCPs => CardPool.Scp,
            Team.FoundationForces or Team.ChaosInsurgency => CardPool.Security,
            _ => CardPool.Free,
        };
    }
}
