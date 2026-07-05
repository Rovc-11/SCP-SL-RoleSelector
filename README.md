# RoleSelector — Rol Seçim Lobisi (EXILED + Project Mer)

Roleplay sunucusu için: her tur başında oyuncular Project Mer ile hazırlanmış özel bir "lobi"
haritasında doğar, roller için hazırlanmış kartlardan birini seçer; seçmeyenler tur içi normal
role dönmeden önce D Sınıfı olarak atanır.

## 1. Akış (round flow)

1. **Tur başlar.** Plugin `mp load {LobbyMapName}` komutunu çalıştırır, o tur **aktif olacak kartları
   hesaplayıp gerçekten spawnlar** (bkz. bölüm 3), ve tüm oyuncuları `LobbyRole` (varsayılan:
   `Tutorial`) rolüyle lobiye ışınlar.
2. **Toplanma (`GatherDuration`, varsayılan 35 sn).** Oyuncular haritaya düşer, hiçbir şey olmaz.
3. **Geri sayım (`PreSelectionCountdown`, varsayılan 15 sn).** "Kartlar X saniye içinde açılıyor"
   anonsu yapılır. Bariyerler hâlâ kapalıdır.
4. **Seçim açık (`SelectionDuration`, varsayılan 60 sn).** Görünmez bariyerlerin çarpışması
   kapatılır, oyuncular kartlara yürüyüp **E** ile alabilir. Kartı alan oyuncu anında
   `WaitingRole` (varsayılan: `Spectator`) olur ve beklemeye geçer, bir daha kart alamaz.
5. **Bitiş.** Süre dolunca (ya da herkes seçim yapınca) kart almayanlara `FallbackRole`
   (varsayılan: `ClassD`) atanır, seçim yapanlara seçtikleri rol verilir, kalan (alınmamış)
   kartlar yok edilir ve `mp unload` ile lobi haritası kaldırılır.

## 2. Kart slotu kaydetme: `kartkur` / `kartsil` / `kartlistele`

Artık kartları Project Mer'in harita dosyasına elle yerleştirmenize gerek yok — plugin, siz nereye
"buraya bir kart olsun" derseniz orada **kendisi** spawnlar. Bunun için 3 RA (Remote Admin) komutu var:

### `kartkur <Rol> [özel_isim]`

Bulunduğunuz **konumu**, verilen role bağlı bir kart slotu olarak kaydeder (konum, komutu çalıştıran
oyuncunun/spectator'ın o anki pozisyonundan otomatik alınır — koordinat yazmanıza gerek yok, sadece
o noktaya gidip komutu çalıştırın). **Sadece `kartkur` ile kaydedilmiş slotlar** kart olarak spawn
olabilir; harita üzerinde başka hiçbir obje otomatik kart sayılmaz.

- `<Rol>`: `PlayerRoles.RoleTypeId` değeri (Scp173, Scp106, Scp096, Scp049, Scp0492, Scp939, Scp079,
  ClassD, Scientist, FacilityGuard, NtfPrivate, NtfSergeant, NtfSpecialist, NtfCaptain,
  ChaosConscript, ChaosRifleman, ChaosMarauder, ChaosRepressor, Tutorial, Spectator, Overwatch...).
- `[özel_isim]` (opsiyonel): serbest metin bir etiket (ör. `Asci`, `Hademe`, `BasBI`, `Cavus`).
  Boş bırakılırsa slot, ait olduğu havuzun **normal/dinamik sayılı** üyesi olur. Bir etiket
  verilirse, o etiket o havuzda **"tek/sabit" bir alt rol** sayılır (bkz. bölüm 3).

Havuzu (D Sınıfı / Araştırma / Güvenlik / SCP / Serbest) **siz belirtmezsiniz** — plugin, verdiğiniz
Rol'ün oyundaki normal takımından (`RoleTypeId.GetTeam()`) otomatik çıkarır: ClassD → D Sınıfı,
Scientist → Araştırma, herhangi bir SCP rolü → SCP, NTF/Chaos rolleri (FacilityGuard, NtfSergeant,
NtfPrivate, NtfCaptain, NtfSpecialist, ChaosConscript, ChaosRifleman, ChaosMarauder, ChaosRepressor)
→ Güvenlik, geri kalan her şey (Tutorial, Spectator, Overwatch, CustomRole vb. — Joker/Arkaplan
GameAdmin/İzleyici için kullanabileceğiniz roller) → Serbest.

Örnekler:
```
kartkur Scp173                → SCP havuzu, normal SCP-173 slotu
kartkur ClassD                → D Sınıfı havuzu, normal D Sınıfı slotu
kartkur ClassD Asci           → D Sınıfı havuzu, "Aşçı" sabit slotu (her tur en fazla 1 aktif)
kartkur ClassD Hademe         → D Sınıfı havuzu, "Hademe" sabit slotu
kartkur Scientist             → Araştırma havuzu, normal Bilim İnsanı slotu
kartkur Scientist BasBI       → Araştırma havuzu, "Baş Bilim İnsanı" sabit slotu
kartkur NtfSergeant Cavus     → Güvenlik havuzu, "Çavuş" sabit slotu
kartkur FacilityGuard         → Güvenlik havuzu, normal Güvenlik Personeli slotu
kartkur Tutorial Joker1       → Serbest havuz (Joker), kota yok, her tur hep aktif
```

Komut size kaydedilen slotun numarasını (`#Id`), hangi havuza düştüğünü ve konumunu geri bildirir.

### `kartsil <slotId>`

`kartkur` ile kaydedilmiş bir slotu siler. Bir kartın **yerini değiştirmek** için: `kartsil` ile
eskisini silin, yeni konuma gidip tekrar `kartkur` çalıştırın.

### `kartlistele` (takma ad: `kartlar`)

Kayıtlı tüm slotları (Id, havuz, rol/etiket, konum) listeler — `kartsil` için Id'lere burada
bakabilirsiniz. (Bu komut açıkça istenmedi ama `kartsil`'in pratikte kullanılabilmesi için gerekliydi, bu yüzden ekledim.)

Tüm slotlar `EXILED/Configs/RoleSelector/card_slots.yml` dosyasında saklanır; sunucu yeniden
başlasa da kaybolmaz.

## 3. Havuzlar ve kota hesaplama (her tur kaç kart aktif olur?)

Kayıtlı slot sayısı sabit olsa da (ör. 15 tane `FacilityGuard` slotu kaydetmiş olabilirsiniz), **her
tur bunların hepsi spawnlanmaz** — o turki oyuncu sayısına (`N`) göre kaçının aktif olacağı
hesaplanır; geri kalan kayıtlı slotlar o tur boş kalır (bir sonraki tur tekrar aday olurlar).

| Havuz | Hedef (aktif olacak toplam kart) | Notlar |
|---|---|---|
| **Araştırma** (Scientist) | `max(ResearchMinimum=2, ceil(N / ResearchPlayersPerCard=9))` | 1 tanesi (eğer bir "BasBI" etiketli slot kayıtlıysa) Baş Bilim İnsanı, kalanı normal. |
| **Güvenlik** (FacilityGuard/NTF/Chaos) | `max(SecurityMinimum=2, ceil(N / SecurityPlayersPerCard=3))` | 1 tanesi (eğer bir "Cavus" etiketli slot kayıtlıysa) Çavuş, kalanı normal Güvenlik. |
| **SCP** | Araştırma hedefine göre kademeli: `≥4 → 3`, `==3 → 2`, `≤2 → 1` | Aynı RoleTypeId'den asla 2 tane aktive edilmez. |
| **D Sınıfı** | `max(DClassMinimum=3, N - (diğer TÜM havuzlarda aktif olan kart sayısı, Serbest dahil))` | Aşçı/Hademe (etiketli) her zaman ayrı sayılır, kalanı normal D Sınıfı doldurur. |
| **Serbest** (Joker / Arkaplan GameAdmin / İzleyici / vb.) | Kota yok — kayıtlı **her** slot her zaman aktif | Formülsüz; sadece kaç slot kaydettiyseniz o kadar spawnlanır. |

**Etiket (özel_isim) mantığı:** Bir havuzda aynı etikete (ör. "Asci") sahip birden fazla slot
kaydettiyseniz, o tur o etiketten **sadece 1 tanesi** rastgele seçilip aktive edilir (yedek konum
gibi düşünebilirsiniz). SCP havuzunda etiket yerine doğrudan `RoleTypeId` gruplanır (aynı SCP
türünden en fazla 1 aktif olur).

**⚠️ Dikkat edilmesi gereken bir nokta:** İstek metninde güvenlik oranı "1/4" olarak yazılmıştı,
fakat verdiğiniz 45 kişilik örnekte (15 güvenlik / 45 oyuncu) oran aslında **1/3**'e karşılık
geliyor (1/4 olsaydı 45 oyuncu için ~12 güvenlik çıkardı, 15 değil). Örnekle tam tutarlı olsun diye
`SecurityPlayersPerCard` varsayılanını **3** yaptım (yani N/3). Aynı örnekte Araştırma (1/9 → 5/45 ✓),
SCP (Araştırma≥4 → 3 ✓) ve D Sınıfı kalan formülü (45-5-15-3-12(Serbest)=10 ✓) tam olarak
uyuyor. Eğer gerçekten 1/4 istiyorsanız `config.yml`'de `security_players_per_card: 4` yapmanız
yeterli.

## 4. Kartın fiziksel görünümü

Spawnlanan her kart, `config.yml`'deki `card_item_type` ayarında belirttiğiniz gerçek bir SCP:SL
item'ıdır (varsayılan: `KeycardJanitor`, sadece görsel/etkileşim amaçlı bir "kart" — hangi rolü
verdiğiyle bir ilgisi yoktur, o bilgi plugin'in kendi hafızasında tutulur). Tüm kartlar aynı görsele
sahip olur; farklı roller için farklı bir görsel istiyorsanız (ör. SCP-173 kartının yanına 173
heykeli) bunu Project Mer ile ayrıca, kart konumunun yanına bir schematic/model olarak elle
yerleştirebilirsiniz — bu tamamen isteğe bağlı bir görsel zenginleştirmedir, plugin'in kart
mantığını etkilemez.

## 5. Görünmez bariyerler: `bariyerkur` / `bariyersil` / `bariyerlistele`

**Önemli:** Eski sürümde burada "objenin Unity sahne adını (`GameObject.name`) `RoleBarrier_`
önekiyle değiştirin" deniyordu. Bu yaklaşım Project Mer ile hiçbir zaman gerçekten çalışamazdı:
Project Mer'in spawnladığı her Primitive, bir prefab klonu olduğundan sahne adı hiçbir "mp"
komutuyla değiştirilemiyor. Ayrıca "mp save" ile kaydettiğiniz haritadaki **her** objenin
`MapName`'i haritanın kendi adı oluyor (hepsi birbirinin aynı) — yani isimden ayırt etmek zaten
mümkün değildi. Bunun yerine artık Project Mer'in HER objeye verdiği, harita içinde **tekil kalan**
ayrı bir **Id**'si kullanılıyor.

Oyuncuların toplanma/geri sayım sırasında kartlara erişememesi için, kartlarla oyuncuların
doğduğu alan arasına bir **Primitive Object** (küp) koyun, sonra:

1. Rengi tamamen şeffaf yapın: `mp mod color 0:0:0:0` (A değeri 0 = görünmez).
2. Çarpışmanın açık olduğundan emin olun (varsayılan olarak Primitive'ler çarpışır).
3. Objenin **Mer Id**'sini öğrenin: objeye toolgun ile bakıp `mp select` yazın (seçmek için),
   ardından `mp modify` yazın (argümansız) — yanıtta göreceğiniz `ID: ...` değeri budur.
4. RoleSelector'a bu Id'yi bariyer olarak tanıtın: **`bariyerkur <MerId>`**.

İstenildiği kadar bariyer ekleyebilirsiniz (her biri için ayrı ayrı `bariyerkur` çalıştırın).
Seçim açılınca (bölüm 1, adım 4) plugin, `bariyerkur` ile kayıtlı Id'lere sahip objelerin
**`PrimitiveFlags`'ini `None` (0) yapar** — harita dosyasında hiçbir değişiklik gerekmez, sadece o
an hem görünmez hem çarpışmasız hale gelirler (tam olarak elle `mp mod primitiveflags 0`
yazmanın yaptığı şey). **Not:** İlk sürümde sadece Unity'nin `Collider` bileşeni kapatılıyordu ama
bu işe yaramadı — SCP:SL'de Primitive çarpışması, Unity fiziğinden değil ağa senkronize edilen
`PrimitiveFlags` değerinden okunuyor; bu yüzden koda `Collider.enabled` yerine doğrudan
`PrimitiveFlags.None` ataması yapıldı.

- `bariyersil <MerId>`: kayıtlı bir bariyeri kaydırdıysanız/kaldırdıysanız kaydını siler.
- `bariyerlistele`: kayıtlı tüm bariyer Id'lerini, o an sahnede bulunup bulunmadıklarıyla listeler.

Tüm bariyer Id'leri `EXILED/Configs/RoleSelector/barrier_ids.yml` dosyasında saklanır.

## 6. Oyuncuların lobiye ışınlanacağı nokta(lar): `spawnkur` / `spawnsil` / `spawnlistele`

Aynı sebepten (bölüm 5), lobi spawn noktaları da artık isimle değil **Mer Id** ile kaydediliyor.
Oyuncuların round başında nereye ışınlanacağını belirtmek için en az bir **Primitive Object**
daha koyun (görünmez/küçük olabilir), Id'sini `mp select` + `mp modify` ile öğrenin, sonra:

**`spawnkur <MerId>`**

İstenildiği kadar spawn noktası ekleyebilirsiniz; oyuncular eklenen tüm spawnlar arasında sırayla
dağıtılır (kalabalık olmasın diye). Hiç spawn kaydetmezseniz oyuncular bulundukları yerde kalır
(uyarı loglanır).

- `spawnsil <MerId>`: kayıtlı bir spawn noktasının kaydını siler.
- `spawnlistele`: kayıtlı tüm spawn Id'lerini, o an sahnede bulunup bulunmadıklarıyla listeler.

Tüm spawn Id'leri `EXILED/Configs/RoleSelector/spawn_ids.yml` dosyasında saklanır.

## 7. Haritayı kaydetme

Bariyerleri, lobi spawn noktalarını ve (varsa) dekoratif modelleri yerleştirdikten sonra
(kartların kendisini artık elle yerleştirmenize gerek yok, bkz. bölüm 2):

```
mp save role_selection_lobby
```

(İsim `Config.LobbyMapName` ile birebir aynı olmalı — varsayılan `role_selection_lobby`.)

## 8. Config (`config.yml` içinde `role_selector` başlığı)

| Ayar | Açıklama | Varsayılan |
|---|---|---|
| `is_enabled` | Eklenti aktif mi | `true` |
| `config_mode` | `true` ise eklenti aktif kalır ama tur başında OTOMATİK rol seçim akışı hiç başlamaz (harita yüklenmez, kart/bariyer/spawn spawnlanmaz, roller atanmaz) — "kartkur"/"bariyerkur"/"spawnkur" ve diğer tüm setup komutları normal çalışmaya devam eder. Haritayı/kart/bariyer/spawn slotlarını round ortasında sürekli kesintiye uğramadan rahatça kurmak için açın, kurulum bitince tekrar `false` yapın. | `false` |
| `lobby_map_name` | Yüklenecek Project Mer haritası | `role_selection_lobby` |
| `unload_map_after_selection` | Seçim bitince harita kaldırılsın mı | `true` |
| `lobby_role` | Seçim öncesi oyuncuların rolü | `Tutorial` |
| `waiting_role` | Kart alan oyuncunun bekleme rolü | `Spectator` |
| `fallback_role` | Kart almayanların alacağı rol | `ClassD` |
| `gather_duration` | Toplanma süresi (sn) | `35` |
| `pre_selection_countdown` | Kartlar açılmadan önceki geri sayım (sn) | `15` |
| `selection_duration` | Kartların açık kaldığı toplam süre (sn) | `60` |
| `card_name_prefix` | Spawnlanan kart objelerinin isim ön eki (sadece log/debug) | `RoleCard_` |
| `card_item_type` | Kartın fiziksel item türü | `KeycardJanitor` |
| `research_players_per_card` | Araştırma: kaç oyuncuya 1 kart | `9` |
| `research_minimum` | Araştırma havuzu minimum toplam | `2` |
| `security_players_per_card` | Güvenlik: kaç oyuncuya 1 kart (bkz. bölüm 3'teki not) | `3` |
| `security_minimum` | Güvenlik havuzu minimum toplam | `2` |
| `d_class_minimum` | D Sınıfı havuzu minimum toplam | `3` |
| `scp_count_tier4_plus` | Araştırma hedefi ≥4 ise SCP sayısı | `3` |
| `scp_count_tier3` | Araştırma hedefi ==3 ise SCP sayısı | `2` |
| `scp_count_tier2_or_less` | Araştırma hedefi ≤2 ise SCP sayısı | `1` |
| `destroy_card_on_pick` | Alınan kart yok edilsin mi | `true` |
| `allow_changing_card` | *(şu an kullanılmıyor — v1'den kalan, rezerve alan)* | `false` |

## 9. Derleme

Bu proje EXILED ve Project Mer'in DLL'lerini **bu bilgisayara indirmez** — CLAUDE.md'deki karara
uygun olarak sadece bu iki projenin GitHub reposundaki güncel kaynak kodu okunarak (API isimleri,
event'ler, komutlar doğrulanarak) yazılmıştır. Derlemek için DLL'lerin **sizin sunucunuzda/
makinenizde** olması gerekir:

1. `EXILED_REFERENCES` adında bir ortam değişkeni tanımlayın; içinde şu DLL'ler bulunmalı:
   - Sunucunun `SCP Secret Laboratory_Data\Managed\` klasöründen: `Assembly-CSharp-Publicized.dll`
     (publicizer ile üretilir — EXILED'ın kendi derleme talimatlarına bakın), `Assembly-CSharp-firstpass.dll`,
     `Mirror.dll`, `UnityEngine.dll`, `UnityEngine.CoreModule.dll`, `UnityEngine.PhysicsModule.dll`,
     `NorthwoodLib.dll`, `CommandSystem.Core.dll`.
   - EXILED'ın derlenmiş çıktısından (`EXILED/Plugins/dependencies/` veya sunucudaki
     `EXILED/Plugins/` klasörü): `Exiled.API.dll`, `Exiled.Events.dll`, `YamlDotNet.dll`.
     (MEC gerekmiyor — zamanlama saf UnityEngine coroutine'leriyle yapılıyor.)
2. Visual Studio / `dotnet build` ile `RoleSelector.csproj`'u derleyin.
3. Çıkan `RoleSelector.dll`'i sunucunuzdaki `EXILED/Plugins/` klasörüne kopyalayın (Project Mer
   pluginiyle birlikte, plugin sırası önemli değildir — Project Mer ayrı bir plugin olarak
   yüklenmeye devam eder). Bu plugin, Project Mer'in DLL'ine derleme zamanında **hiçbir zaman**
   referans vermez; onunla çalışma zamanında (reflection ile, bkz. `Core/ProjectMerBridge.cs`)
   konuşur — harita yükleme/kaldırma için `ProjectMER.Features.MapUtils.LoadMap`/`UnloadMap`
   metotlarını doğrudan çağırır (Project Mer'in "mp load/unload" komutlarının RA izin kontrolünü
   atlamak için — bkz. bölüm 10), bariyer/spawn nesnelerini ise onların Project Mer tarafından
   verilmiş `Id`'sinden bulur.

## 10. Bilinen sınırlamalar / kontrol edilmesi gerekenler

- Kod, EXILED'ın güncel (9.14.2) reposundan doğrulanan API'lere göre yazıldı: `Player.Role.Set
  (RoleTypeId, SpawnReason, RoleSpawnFlags)`, `RoleTypeId.GetTeam()`, `Exiled.Events.Handlers.
  Player.PickingUpItem`, `Exiled.Events.Handlers.Server.RoundStarted/RestartingRound/RoundEnded`,
  `Pickup.CreateAndSpawn`. Tüm RA komutları (`kartkur`/`kartsil`/`kartlistele`/`bariyerkur`/
  `bariyersil`/`bariyerlistele`/`spawnkur`/`spawnsil`/`spawnlistele`) `CommandSystem.ICommand` +
  `[CommandHandler(typeof(RemoteAdminCommandHandler))]` desenini kullanıyor — bu, topluluktaki
  EXILED pluginlerinde standart RA komutu kayıt yöntemi, ama sizin EXILED sürümünüzle birebir
  derleyip test etmenizi öneririm.
- **Harita yükleme/kaldırma artık "mp" RA komutunu ÇALIŞTIRMIYOR, doğrudan Project Mer'in kendi
  static metoduna reflection ile ulaşıyor** (bkz. `Core/ProjectMerBridge.cs`). Sebep: Project
  Mer'in GÜNCEL "mp load"/"mp unload" komutları (Commands/Map/Load.cs, Unload.cs — GitHub'dan
  doğrulandı), çalıştırılmadan önce `sender.HasAnyPermission("mpr.load")` kontrolü yapıyor
  (LabApi.Features.Permissions). Bu kontrol gerçek bir RA oyuncusu için (yetki grubu varsa) geçer,
  ama plugin kendi kodundan (eski sürümde `Server.Host?.Sender` ile) çalıştırdığında BAŞARISIZ
  oluyordu — "Host" EXILED'ın sahte bir oyuncusu olduğundan `Player.Get(sender)` onu gerçek bir
  Player'a çözüyor ama bu sahte oyuncunun hiçbir RA yetki grubu yok, bu yüzden izin reddediliyordu
  (sadece elle, gerçek bir RA panelinden yazıldığında çalışıyordu — bildirdiğiniz sorun buydu).
  Çözüm: izin kontrolünü İÇERMEYEN `ProjectMER.Features.MapUtils.LoadMap`/`UnloadMap` static
  metotlarına doğrudan ulaşmak; bu, komutların içeriden çağırdığı TEK gerçek iş mantığı olduğundan
  hiçbir işlevsellik kaybı olmaz. Bu köprü çalışma zamanında sunucuda yüklü olan ProjectMER
  assembly'sini arar; Project Mer sunucuda yüklü/etkin değilse ya da sürümü çok farklıysa
  (beklenen tip/metot bulunamazsa) net bir hata loglanır, plugin çökmez.
- **Bariyer/lobi-spawn objeleri artık isimle DEĞİL, Project Mer'in verdiği `MapEditorObject.Id`
  ile bulunuyor** (bkz. bölüm 5-6, `bariyerkur`/`spawnkur`). Eski isim-ön-eki yaklaşımı kaldırıldı
  çünkü (a) Project Mer'in spawnladığı Primitive'lerin Unity sahne adı hiçbir "mp" komutuyla
  değiştirilemiyor (yani hiç çalışamazdı), ve (b) "mp save" ile kaydedilen bir haritadaki HER
  objenin `MapName`'i haritanın kendi adı oluyor (hepsi aynı) — bildirdiğiniz ikinci sorun.
  Kartlar zaten isimle değil, plugin'in kendi ürettiği `Pickup` referansıyla takip ediliyordu
  (bkz. bölüm 2-3), bu yüzden kartkur/kartsil/kartlistele hiç değişmedi.
- `RoleTypeId` string eşleşmesi büyük/küçük harfe duyarsızdır (`Scp173` / `scp173` / `SCP173` hepsi çalışır).
- Havuz otomatik tespiti `RoleTypeId.GetTeam()`'e dayanıyor: ClassD/Scientist/SCP rolleri net;
  Facility Guard + tüm NTF/Chaos rolleri "Güvenlik" sayılıyor. Sizin sunucunuzda "Güvenlik"
  kavramına girmesini istemediğiniz bir rol varsa (ör. Chaos'u tamamen ayrı tutmak isterseniz),
  haber verin, havuz tespitini elle seçilebilir hale getirebilirim.
- Round ortasında katılan (late-join) oyuncular şu an bu akışa dahil değildir; vanilla/EXILED'ın
  kendi late-join mantığına bırakılmıştır. İsterseniz sonradan katılanlara da fallback rolü
  atayacak bir `Player.Verified` kancası eklenebilir.
- Kart konumu kaydedilirken sadece **pozisyon** alınıyor, rotasyon alınmıyor (kart döner bir pickup
  olduğundan pratikte önemli değil).
- net48'te `ArraySegment<string>` üzerinde `[]` indeksleyici yok (bu netstandard2.1+ ile geldi),
  bu yüzden komut argümanlarına `KartKurCommand.ArgAt(arguments, i)` (Array/Offset üzerinden) ile
  erişiliyor. `RoleTypeId.GetTeam()` çağrısı da hem Exiled'ın hem oyunun kendi (`PlayerRolesUtils`)
  aynı isimli extension'ını sağladığından, `CardSlot.ResolvePool` içinde tam nitelenmiş
  (`Exiled.API.Extensions.RoleExtensions.GetTeam`) olarak çağrılıyor.
