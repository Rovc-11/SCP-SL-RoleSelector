// -----------------------------------------------------------------------
// <copyright file="CardPool.cs" company="William">
// Role Selector - card quota pools.
// </copyright>
// -----------------------------------------------------------------------

namespace RoleSelector.Core
{
    /// <summary>
    /// Bir kart slotunun hangi kota havuzuna ait olduğunu belirtir. Havuz, slotun rolünün
    /// vanilla oyundaki <see cref="PlayerRoles.Team"/> değerinden otomatik olarak çıkarılır
    /// (bkz. <see cref="CardSlot.ResolvePool"/>), admin ayrıca bir havuz belirtmez.
    /// </summary>
    public enum CardPool
    {
        /// <summary>D Sınıfı (Aşçı / Hademe / normal D Sınıfı).</summary>
        DClass,

        /// <summary>Bilim insanları (Baş Bilim İnsanı / Bilim İnsanı).</summary>
        Research,

        /// <summary>Güvenlik (Çavuş / Güvenlik Personeli, NTF ya da Chaos rolleri).</summary>
        Security,

        /// <summary>SCP rolleri.</summary>
        Scp,

        /// <summary>Kota uygulanmayan serbest kartlar (Joker, Arkaplan GameAdmin, İzleyici, vb.).</summary>
        Free,
    }
}
