// -----------------------------------------------------------------------
// <copyright file="ProjectMerBridge.cs" company="William">
// Role Selector - reflection-based bridge to Project Mer (ProjectMER).
// </copyright>
// -----------------------------------------------------------------------

namespace RoleSelector.Core
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    using Exiled.API.Features;
    using UnityEngine;

    /// <summary>
    /// Project Mer'in (ProjectMER) DLL'ine derleme zamanında hiç referans vermeden, çalışma zamanında
    /// (reflection ile) onunla konuşmak için köprü. CLAUDE.md'deki karara uygun olarak ProjectMER'in
    /// DLL'i bu bilgisayara indirilmedi/derlemeye eklenmedi; bu sınıf sadece ProjectMER'in GitHub
    /// reposundaki GÜNCEL kaynak koduna (aşağıda anılan dosyalar) bakılarak, çalışma zamanında sunucuda
    /// zaten yüklü olan ProjectMER assembly'sine reflection ile ulaşacak şekilde yazıldı.
    ///
    /// <para>Bu köprü iki ayrı, gerçek sorunu çözer:</para>
    ///
    /// <list type="bullet">
    /// <item>
    /// <b>1) Harita yükleme hiç çalışmıyordu ("Project Mer sadece RM panelinde izin veriyor").</b>
    /// ProjectMER'in GÜNCEL "mp load/unload" RA komutları (Commands/Map/Load.cs, Unload.cs),
    /// çalıştırmadan önce <c>sender.HasAnyPermission($"mpr.{{command}}")</c> kontrolü yapıyor
    /// (LabApi.Features.Permissions). Bu kontrol gerçek bir RA oyuncusu için (yetki grubu varsa)
    /// geçer, ama plugin'in kendi kodundan eskiden yapıldığı gibi <c>Server.Host?.Sender</c> ile
    /// çalıştırıldığında BAŞARISIZ olur: "Host", EXILED'ın sahte/dummy bir ReferenceHub'ı olduğundan
    /// <c>Player.Get(sender)</c> onu gerçek bir Player'a (Server.Host) çözüyor, ama bu sahte oyuncunun
    /// hiçbir RA yetki grubu yok — bu yüzden izin reddediliyor ve "mp load" hiçbir zaman gerçekten
    /// çalışmıyordu (sadece elle, gerçek bir RA panelinden yazıldığında çalışıyordu — bildirdiğiniz
    /// sorun tam olarak buydu). Çözüm: komut/izin katmanını (ICommand.Execute) tamamen atlayıp, o
    /// komutların asıl işi yapan koduna, yani hiçbir izin/sender kontrolü İÇERMEYEN
    /// <c>ProjectMER.Features.MapUtils.LoadMap(string)</c>/<c>UnloadMap(string)</c> static
    /// metodlarına doğrudan reflection ile ulaşmak. Bu metodlar zaten "mp load"/"mp unload"
    /// komutlarının içeriden çağırdığı TEK gerçek iş mantığı; hiçbir işlevsellik kaybı olmaz.
    /// </item>
    /// <item>
    /// <b>2) Bariyer/lobi-spawn nesnelerini isimden bulmak hiç mümkün değildi.</b>
    /// Eski tasarım, admin'in Primitive objelerin Unity sahne adını ("GameObject.name") elle
    /// "RoleBarrier_1" gibi bir ön ekle değiştirmesini varsayıyordu. Ancak ProjectMER'in güncel
    /// kaynağına (Features/Serializable/SerializablePrimitive.cs) bakıldığında, spawnlanan her
    /// Primitive'in GameObject'i bir prefab klonu olup ADI HİÇBİR ZAMAN ProjectMER tarafından
    /// (ya da admin tarafından herhangi bir "mp" komutuyla) değiştirilmiyor — yani bu yaklaşım
    /// zaten en baştan çalışamazdı. Ayrıca "mp save" ile kaydedilen bir haritadaki HER nesnenin
    /// <c>MapEditorObject.MapName</c> alanı haritanın kendi adı olur (hepsi birbirinin aynı),
    /// bu da isimle ayırt etmeyi imkansız kılıyor (bildirdiğiniz ikinci sorun). Ancak her nesnenin,
    /// harita içinde tekil kalan AYRI bir <c>MapEditorObject.Id</c>'si vardır — tam olarak haritayı
    /// düzenlerken nesneye toolgun ile bakıp "mp select" (seçmek için), sonra "mp modify" (bilgileri
    /// görmek için) yazdığınızda yanıtta göreceğiniz "ID: ..." değeri. Bu köprü, verilen bir "Mer
    /// Id"sine sahip sahnedeki <c>MapEditorObject</c> bileşenini bulur; "bariyerkur"/"spawnkur"
    /// komutları da tam olarak bu Id'yi kaydeder (bkz. <see cref="MerIdListStore"/>).
    /// </item>
    /// </list>
    /// </summary>
    internal static class ProjectMerBridge
    {
        private const string AssemblyName = "ProjectMER";

        private static bool initialized;
        private static bool available;

        private static Type mapEditorObjectType;
        private static PropertyInfo idProperty;

        private static MethodInfo loadMapMethod;
        private static MethodInfo unloadMapMethod;
        private static MethodInfo findMapEditorObjectsMethod;

        /// <summary>
        /// Gets a value indicating whether ProjectMER assembly'si ve beklenen tip/metodlar çalışma
        /// zamanında bulunabildi mi (yani sunucuda Project Mer gerçekten yüklü ve bu köprünün
        /// beklediği sürümle uyumlu mu).
        /// </summary>
        public static bool IsAvailable
        {
            get
            {
                EnsureInitialized();
                return available;
            }
        }

        /// <summary>
        /// ProjectMER'in "mp load {mapName}" komutuyla BİREBİR AYNI işi yapan, hiçbir RA izin
        /// kontrolü içermeyen <c>MapUtils.LoadMap(string)</c> metodunu doğrudan çağırır.
        /// </summary>
        /// <param name="mapName">Yüklenecek haritanın adı (dosya adıyla aynı, uzantısız).</param>
        /// <returns>Çağrı hatasız tamamlandıysa <see langword="true"/>.</returns>
        public static bool LoadMap(string mapName)
        {
            if (!IsAvailable)
            {
                Log.Error("[RoleSelector] Project Mer (ProjectMER) sunucuda yüklü/etkin bulunamadı; harita yüklenemiyor.");
                return false;
            }

            try
            {
                loadMapMethod.Invoke(null, new object[] { mapName });
                Log.Info($"[RoleSelector] '{mapName}' haritası (ProjectMER.MapUtils.LoadMap üzerinden doğrudan) yüklendi.");
                return true;
            }
            catch (TargetInvocationException exception)
            {
                Log.Error($"[RoleSelector] '{mapName}' haritası yüklenemedi: {exception.InnerException?.Message ?? exception.Message}");
                return false;
            }
            catch (Exception exception)
            {
                Log.Error($"[RoleSelector] '{mapName}' haritası yüklenirken beklenmeyen hata: {exception.Message}");
                return false;
            }
        }

        /// <summary>
        /// ProjectMER'in "mp unload {mapName}" komutuyla BİREBİR AYNI işi yapan
        /// <c>MapUtils.UnloadMap(string)</c> metodunu doğrudan çağırır.
        /// </summary>
        /// <param name="mapName">Kaldırılacak haritanın adı.</param>
        public static void UnloadMap(string mapName)
        {
            if (!IsAvailable)
                return;

            try
            {
                unloadMapMethod.Invoke(null, new object[] { mapName });
            }
            catch (TargetInvocationException exception)
            {
                Log.Error($"[RoleSelector] '{mapName}' haritası kaldırılamadı: {exception.InnerException?.Message ?? exception.Message}");
            }
            catch (Exception exception)
            {
                Log.Error($"[RoleSelector] '{mapName}' haritası kaldırılırken beklenmeyen hata: {exception.Message}");
            }
        }

        /// <summary>
        /// Sahnedeki tüm Project Mer nesneleri arasından, verilen "Mer Id"sine (<c>MapEditorObject.Id</c>)
        /// sahip olan(lar)ı bulur. Normalde bir Id tekildir ama güvenlik için birden fazla eşleşme
        /// döndürebilir.
        /// </summary>
        /// <param name="mapEditorId">"mp select &lt;id&gt;" + "mp modify" ile görülen "ID:" değeri.</param>
        /// <returns>Eşleşen nesnelerin <see cref="Transform"/>'ları.</returns>
        public static IEnumerable<Transform> FindByMerId(string mapEditorId)
        {
            if (!IsAvailable || string.IsNullOrWhiteSpace(mapEditorId))
                yield break;

            // UnityEngine.Object.FindObjectsByType<T>(FindObjectsSortMode) generic metodu, bu kod
            // tabanında (eskiden FindByNamePrefix'te) zaten kanıtlanmış şekilde çalışıyor; burada
            // aynı generic metodu, çalışma zamanında çözülen MapEditorObject tipiyle
            // (MakeGenericMethod) çağırıyoruz — böylece var olmadığından emin olamayacağımız bir
            // non-generic overload'a bel bağlamamış oluyoruz.
            object result = findMapEditorObjectsMethod.Invoke(null, new object[] { FindObjectsSortMode.None });
            foreach (object candidate in (System.Collections.IEnumerable)result)
            {
                if (candidate is not Component component)
                    continue;

                string id = idProperty.GetValue(candidate) as string;
                if (string.Equals(id, mapEditorId, StringComparison.OrdinalIgnoreCase))
                    yield return component.transform;
            }
        }

        /// <summary>
        /// Verilen "Mer Id"sinin şu anda sahnede (herhangi bir yüklü haritada) gerçekten mevcut olup
        /// olmadığını kontrol eder. Sadece "bariyerkur"/"spawnkur"/"...listele" komutlarında admine
        /// anında geri bildirim vermek için kullanılır — obje bulunamasa da kayıt yine de yapılır,
        /// çünkü harita henüz yüklenmemiş olabilir (round başında tekrar aranır).
        /// </summary>
        /// <param name="mapEditorId">Kontrol edilecek Id.</param>
        /// <returns>Sahnede bulunduysa <see langword="true"/>.</returns>
        public static bool ExistsInScene(string mapEditorId) => FindByMerId(mapEditorId).Any();

        private static void EnsureInitialized()
        {
            if (initialized)
                return;

            initialized = true;

            try
            {
                Assembly assembly = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => string.Equals(a.GetName().Name, AssemblyName, StringComparison.OrdinalIgnoreCase));

                if (assembly == null)
                {
                    Log.Error("[RoleSelector] 'ProjectMER' assembly'si yüklü bulunamadı. Project Mer sunucuda yüklü ve etkin mi?");
                    available = false;
                    return;
                }

                Type mapUtilsType = assembly.GetType("ProjectMER.Features.MapUtils");
                mapEditorObjectType = assembly.GetType("ProjectMER.Features.Objects.MapEditorObject");

                loadMapMethod = mapUtilsType?.GetMethod("LoadMap", BindingFlags.Public | BindingFlags.Static);
                unloadMapMethod = mapUtilsType?.GetMethod("UnloadMap", BindingFlags.Public | BindingFlags.Static);
                idProperty = mapEditorObjectType?.GetProperty("Id", BindingFlags.Public | BindingFlags.Instance);

                if (mapEditorObjectType != null)
                {
                    MethodInfo openGenericFindObjects = typeof(UnityEngine.Object)
                        .GetMethods(BindingFlags.Public | BindingFlags.Static)
                        .FirstOrDefault(m => m.Name == "FindObjectsByType" && m.IsGenericMethodDefinition && m.GetParameters().Length == 1);

                    findMapEditorObjectsMethod = openGenericFindObjects?.MakeGenericMethod(mapEditorObjectType);
                }

                available = mapUtilsType != null && mapEditorObjectType != null
                    && loadMapMethod != null && unloadMapMethod != null && idProperty != null && findMapEditorObjectsMethod != null;

                if (!available)
                {
                    Log.Error(
                        "[RoleSelector] ProjectMER bulundu ama beklenen tip/metodlar eşleşmedi (ProjectMER sürümü değişmiş olabilir): "
                        + $"MapUtils={mapUtilsType != null}, MapEditorObject={mapEditorObjectType != null}, "
                        + $"LoadMap={loadMapMethod != null}, UnloadMap={unloadMapMethod != null}, Id={idProperty != null}, "
                        + $"FindObjectsByType<T>={findMapEditorObjectsMethod != null}.");
                }
            }
            catch (Exception exception)
            {
                Log.Error($"[RoleSelector] ProjectMER reflection köprüsü kurulamadı: {exception}");
                available = false;
            }
        }
    }
}
