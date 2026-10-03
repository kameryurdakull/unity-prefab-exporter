# Prefab Package Exporter

**Prefablarını topla → Bağımlılıklarını incele → Tek bir paket olarak dışa aktar.**

Farklı klasörlerdeki prefabları ve referans verdikleri assetleri başka Unity projelerine taşımak için hazırlanmış bir Editor aracı. Karakter, UI, ortam ve efekt prefablarıyla çalışır; ParticleSystem gerektirmez.

## Özellikler

- Çoklu prefab export ve farklı klasörlerden koleksiyon oluşturma.
- Yinelenen kayıtların otomatik elenmesi.
- Bağımlılık önizlemesi, dosya yolları ve paket gereksinimleri.
- Project penceresinde sağ tık ile hızlı export.
- Eksik script kontrolü ve export anında yenilenen bağımlılık listesi.
- Açık/koyu Unity temalarına uyumlu, bölümlere ayrılmış pencere.
- Ayrı Editor assembly; araç kodu oyuncu build'ine eklenmez.

## Kurulum

**Gereksinimler:** Unity 6000.3 veya üzeri, PATH'te erişilebilir Git.
Unity 6000.3.15f1 üzerinde doğrulanmıştır. Aracın ek runtime kütüphane veya render pipeline bağımlılığı yoktur.

1. **Window → Package Manager** penceresini aç.
2. **+ → Install package from git URL…** seç. Bazı sürümlerde **Add package from git URL…** olarak görünür.
3. Aşağıdaki URL'yi yapıştır:

```text
https://github.com/kameryurdakull/ParticleExporter.git?path=/Packages/com.kameryurdakull.prefab-exporter
```

Alternatif olarak projenin `Packages/manifest.json` dosyasındaki `dependencies` nesnesine ekle:

```json
"com.kameryurdakull.prefab-exporter": "https://github.com/kameryurdakull/ParticleExporter.git?path=/Packages/com.kameryurdakull.prefab-exporter"
```

Eski kurulumu `Assets/ParticleExporter/Editor` altına kopyaladıysan UPM kurulumundan önce bu eski **Editor** klasörünü kaldır. İki kurulum aynı anda tutulmamalıdır; kendi prefablarını silmen gerekmez.

## Hızlı başlangıç

1. Project penceresinde bir veya birkaç `.prefab` asseti seç.
2. Sağ tıkla: **Prefab Exporter → Export Selected Prefabs…**
3. Varsa hedef projede gerekli Unity paketlerinin listesini onayla.
4. Dosya konumunu seç. Tek bir `.unitypackage` oluşturulur.
5. Hedef projede **Assets → Import Package → Custom Package…** ile dosyayı içe aktar.

`Ctrl/Cmd` ve `Shift` ile çoklu seçim desteklenir. Karışık seçimlerde yalnızca desteklenen prefablar alınır. Klasör seçmek içindeki prefabları otomatik taramaz.

## Export penceresi

**Tools → Prefab Package Exporter** menüsünden açılır.

| Bölüm | Kullanım |
| --- | --- |
| **01 / Add prefabs** | Object alanından prefab seçip **Add** ile ekle veya **Add selected prefabs** ile Project seçimini topla. |
| **02 / Collection** | Prefabları ve dosya yollarını incele. **Remove** tek kaydı, **Clear** koleksiyonu kaldırır; assetleri silmez. |
| **03 / Package preview** | **Analyze dependencies** ile paket içeriğini, eksik scriptleri ve gerekli Unity paketlerini gör. |
| **Export .unitypackage** | Koleksiyonun tamamını dışa aktar. Önceden analiz yapmak zorunlu değildir. |

Farklı klasörlerden toplamak için sağ tık menüsündeki **Prefab Exporter → Add to Export Window** seçeneğini kullan. Aynı prefab tekrar eklenmez. Export sonrası koleksiyon korunur. Pencere mevcutken script derleme/domain reload sırasında liste saklanır; kalıcı bir koleksiyon preset'i değildir.

## Bağımlılıkların kapsamı

Unity'nin `AssetDatabase.GetDependencies` ile bildirdiği recursive serialized bağımlılıklar dahil edilir:

| Dahil edilen | Açıklama |
| --- | --- |
| Prefablar | Seçilen kökler ve referans verilen nested prefablar. |
| Görsel assetler | Referans verilen materyaller, texture'lar, shader'lar ve animasyon assetleri. |
| Scriptler | Component scriptleri ve Unity'nin bildirdiği diğer script bağımlılıkları. |
| Assembly dosyaları | Script klasöründen yukarı doğru bulunabilen yerel `.asmdef` / `.asmref` dosyaları. |

Export kökleri `Assets/` altında gerçek `.prefab` assetleri olmalıdır. Sahne objeleri, model import prefabları ve `Packages/` altındaki prefablar kök olarak kabul edilmez.

Package Manager assetleri pakete gömülmez. Paket adları önizlemede ve export öncesinde gösterilir; hedef projede ayrıca kurulmalıdır. Eksik script bulunan seçili prefab hiyerarşileri export'u engeller.

Kod üzerinden erişilen `Resources.Load` yolları, Addressables anahtarları, başka scriptlerin assembly bağımlılıkları ve Project Settings otomatik taşınmaz. Hedef projenin render pipeline ve paket sürümlerini ayrıca eşleştir. Bağımlılıklar export anında yeniden hesaplanır.

## Test prefabları

Package Manager'da **Prefab Package Exporter → Samples → Export Tests → Import** seç.

Örnekler `Assets/Samples/Prefab Package Exporter/1.0.0/Export Tests` altına alınır:

| Prefab | Senaryo |
| --- | --- |
| `TestBurst` | Tek seferlik parçacık patlaması. |
| `TestLoop` | Sürekli parçacık üretimi. |
| `TestHierarchy` | Alt objede ParticleSystem; kökte test scripti ve alt objeye serialized referans. |

Üç prefab ortak `TestParticle.mat` materyalini ve `PrefabExportTest` component'ini kullanır. Component menüsünden **Test → Play** ve **Test → Stop And Clear** ile dene.

Örnek materyal **Universal Render Pipeline** gerektirir. Diğer pipeline'larda uyumlu bir materyal ata; bu gereksinim yalnızca görsel örnekler içindir.

Üç prefabı birlikte analiz ederek ortak script, materyal ve sample assembly dosyasının yalnızca bir kez dahil edildiğini kontrol edebilirsin.

## Sorun giderme

| Durum | Çözüm |
| --- | --- |
| Git URL yüklenmiyor | Git'in PATH'te olduğundan, repo erişiminden ve `?path=` bölümünün korunduğundan emin ol. Git yeni kurulduysa Unity ve Hub'ı yeniden başlat. |
| Export menüsü pasif | Project penceresinde `Assets/` altında `.prefab` asseti seç. |
| Missing script uyarısı | Prefab ve alt objelerindeki eksik component'leri düzelt; yeniden analiz et. |
| Hedef projede pembe materyal | Doğru render pipeline'ı ve önizlemede listelenen paketleri kur. |
| Git güncellemeleri görünmüyor | Package Manager'da Git bağımlılığını güncelle; Unity çözümlenen commit'i lock dosyasında saklar. |

## Geliştirme ve yayınlama

```text
Packages/com.kameryurdakull.prefab-exporter/
├── package.json
├── Editor/
│   ├── PrefabExporter.Editor.asmdef
│   └── ...
├── Samples~/ExportTests/
│   ├── Materials/
│   ├── Prefabs/
│   └── Scripts/
├── CHANGELOG.md
└── README.md
```

Bu repoyu Unity Hub'dan proje olarak açtığında araç embedded package olarak yüklenir. Başka bir projede yerel kurulum için **Install package from disk…** ile bu klasördeki `package.json` dosyasını seç.

Git URL'nin bu yapıyı kullanabilmesi için paket dosyalarını GitHub'da erişilen branch'e commit edip push et. Sürüm yayınlarken `package.json` ve changelog'u güncelle; ilgili commit'e sürüm tag'i ekle. Örneğin `v1.0.0` tag'i yayınlandıktan sonra sabit sürüm için URL sonuna `#v1.0.0` eklenebilir.

Editor dosyaları ve `.meta` dosyaları birlikte version control'de tutulur. `Samples~` içeriği kullanıcı **Import** seçeneğini kullanana kadar projeye derlenen kod eklemez. Örnekleri değiştirdiğinde kaynak `Samples~/ExportTests` içeriğini de güncelle.

## Unity dokümantasyonu

- [Git dependencies ve alt klasör URL sözdizimi](https://docs.unity3d.com/6000.0/Documentation/Manual/upm-git.html)
- [UPM package layout](https://docs.unity3d.com/6000.0/Documentation/Manual/cus-layout.html)
- [Package samples](https://docs.unity3d.com/6000.0/Documentation/Manual/cus-samples.html)
