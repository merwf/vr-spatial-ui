# VR Spatial UI — Lunar Interface

**Kocaeli Üniversitesi · Bilişim Sistemleri Mühendisliği · Sanal Gerçeklik Dersi · Grup 2**

VR'da **İnsan–Bilgisayar Etkileşimi ve Kullanıcı Deneyimi** konusunu anlatmak için Unity üzerinde geliştirilmiş, Ay yüzeyi temalı bir **world-space (3B uzaya yerleştirilmiş) arayüz** prototipidir. Tek bir *Lunar Interface* paneli üzerinde çevre ayarları, farklı etkileşim modelleri ve kullanıcıya verilen görsel geri bildirim gösterilir.

## İçindekiler

- [Kapsam](#kapsam)
- [Teknik altyapı](#teknik-altyapı)
- [Sahne ve işlevler](#sahne-ve-işlevler)
- [VR Etkileşim Modelleri](#vr-etkileşim-modelleri)
- [Kurulum ve çalıştırma](#kurulum-ve-çalıştırma)
- [Test senaryoları](#test-senaryoları)
- [Dosya yapısı](#dosya-yapısı)
- [Kapsam dışı ve dikkat edilmesi gerekenler](#kapsam-dışı-ve-dikkat-edilmesi-gerekenler)
- [Ekip ve üçüncü taraf kaynaklar](#ekip-ve-üçüncü-taraf-kaynaklar)

## Kapsam

Projede VR'ın temelleri, etkileşim yöntemleri, spatial UI ve kullanıcı ergonomisi anlatılır. **Uzamsal bir menünün ışın, yakın etkileşim ve bakış ile nasıl kullanılabildiğini** göstermektir.

Mevcut panel şu çevre kontrollerini içerir:

- **Gravity: MOON / EARTH:** Unity fizik yerçekimini Ay ve Dünya değerleri arasında değiştirir.
- **Snap Turn: ON / OFF:** Hazır XR dönüş yöntemlerini değiştirir.
- **Light Intensity:** Sahnenin yönlü ışık şiddetini slider üzerinden ayarlar.

## Teknik altyapı

| Bileşen | Kullanım |
| --- | --- |
| **Unity 6000.6.7f1** | Proje editörü |
| **Universal Render Pipeline (URP)** | Render altyapısı |
| **XR Interaction Toolkit 3.6.1** | XR interactor ve etkileşim bileşenleri |
| **XR Origin / XR Device Simulator** | Sanal kamera, kontrolcüler ve masaüstü simülasyonu |
| **Unity Input System** | Kontrolcü tetik, grip ve thumbstick girişleri |
| **World Space Canvas + uGUI + TextMeshPro** | 3B menü ve yazılar |
| **Tracked Device Graphic Raycaster / XR UI input** | XR kontrolcü ışınıyla UI kullanımı |

XR Device Simulator, fiziksel VR gözlüğü olmadan **geliştirme ve masaüstü demonstrasyonu** içindir. Gerçek cihazda OpenXR/cihaz yapılandırması ayrıca doğrulanmalıdır.

## Sahne ve işlevler

Ana sahne: `Assets/Scenes/SampleScene.unity`.

Öne çıkan sahne nesneleri:

| Nesne | Görevi |
| --- | --- |
| `XR Origin (XR Rig)` | HMD ve kontrolcü etkileşimleri |
| `Canvas` / `Panel` | Lunar Interface world-space menüsü |
| `LunarPanelController` | Gravity, Snap Turn, Light Intensity |
| `VRPanelInteractionController` | Grab, zoom, gaze reset, aç/kapat, durum geri bildirimi |
| `VR Tutamac - Grip` | Panelin yakalanması için XR etkileşim alanı |
| `VR Gaze RESET (2 sn)` | Bakışla veya ray tıklamasıyla menüyü sıfırlama |
| `VR Panel Kapat` | Ana paneli gizleme |
| `VR Menüyü Aç (Kalici)` | Panel gizliyken tekrar açma |
| `VR Etkilesim Durumu` | Hover, grab ve reset gibi işlemleri metinle gösterme |

## VR Etkileşim Modelleri

| Model | Gerçekleştirilen davranış | Geri bildirim |
| --- | --- | --- |
| **Ray Casting** | XR kontrolcü ışınıyla Gravity / Snap Turn butonları, Light Intensity slider ve eklenen UI butonlarına erişim | UI hover/press davranışları |
| **Direct / Grab Interaction** | Panel üzerindeki tutamaç, XR `Select`/Grip ile yakalanır; kontrolcü hareketiyle panel taşınır | Tutamaç hazır/hover/tutuluyor durumlarında renk değiştirir |
| **Thumbstick ile mesafe ayarı** | Panel tutuluyorken joystick yukarı/aşağı ile kamera-panel mesafesi değiştirilir | Hareket anında durum metni; mesafe sınırı uygulanır |
| **Head-Gaze / Dwell** | Baş/kamera yönü RESET butonu üzerinde **2 saniye** tutulursa panel varsayılan konuma döner | Butonun merkezinden dışa doğru büyüyen ilerleme dairesi |
| **Button / Toggle Interaction** | `MENÜYÜ KAPAT` ana paneli gizler; panel dışındaki `MENÜYÜ AÇ` kontrolüyle geri getirir | Panel görünürlüğü değişir |
| **Visual Feedback** | Gaze efekti, hover renkleri ve durum yazıları etkileşimi görünür kılar | Kullanıcı hedeflemeyi ve işlem sonucunu anlayabilir |

**Teknik notlar:**

- `VRPanelInteractionController.cs`, var olan Canvas'a bağlanır; eski `LunarPanelController.cs` işlevlerine müdahale etmez.
- Gaze hedefleme, XR kameranın ileri doğrultusundan `Physics.Raycast` kullanır; **gerçek eye tracking değildir**.
- Gaze süresi dolmadan hedef dışına çıkıldığında ilerleme sıfırlanır. RESET butonuna normal UI tıklamasıyla da sıfırlama yapılabilir.
- Gaze ilerleme dairesi çalışma sırasında sprite olarak üretilir. Daire `RectMask2D` ile buton alanında tutulur; buton koyu zemin, beyaz yazı ve turkuaz kontur kullanır.
- Joystick ile mesafe ayarı **tutamaç tutuluyorken** yapılır. Panel mesafesi sınırlandırılır ve tutma boyunca hareket/dönüş sağlayıcıları geçici olarak devre dışı bırakılır.
- Panel kapatıldığında tüm Canvas kapatılmaz. `MENÜYÜ AÇ` kontrolünün erişilebilir kalması hedeflenir.

## Kurulum ve çalıştırma

1. Depoyu klonlayın:

   ```bash
   git clone https://github.com/merwf/vr-spatial-ui.git
   cd vr-spatial-ui
   ```

2. Unity Hub'dan **6000.6.7f1** sürümüyle projeyi açın.
3. `Packages/manifest.json` içindeki XR Interaction Toolkit ve diğer paketlerin çözümlendiğini kontrol edin.
4. `Assets/Scenes/SampleScene.unity` sahnesini açın.
5. Play Mode'a girin ve XR Device Simulator panelindeki aktif cihaz/tuş ipuçlarını kullanın.

**Masaüstü demo kullanım sırası:**

1. XR kontrolcüsüyle Gravity / Snap Turn butonlarına basın veya slider'ı kaydırın.
2. Kontrolcüyü panelin üstündeki **TUTAMAÇ** alanına yöneltin; Grip/Select ile tutun ve kontrolcüyü hareket ettirin.
3. Panel tutulurken thumbstick dikey hareketiyle menüyü yaklaştırın/uzaklaştırın. Editör testinde alternatif giriş: **PageUp / PageDown**.
4. HMD'nin bakış yönünü **RESET** butonunda 2 saniye tutun; merkezden büyüyen daireyi ve konum sıfırlamasını gözlemleyin.
5. `MENÜYÜ KAPAT` ve ardından `MENÜYÜ AÇ` düğmelerini kullanın.

> Simülatörde giriş eşlemeleri seçili HMD/kontrolcü moduna göre değişebilir. Ekrandaki *XR Device Simulator* kısayol ipuçlarını esas alın. `G` tuşu Grip olarak atanmış olabilir; her kurulumda garanti edilmez.

## Test senaryoları

Aşağıdakiler, merge öncesi **Unity Play Mode'da tek tek doğrulanması gereken** kabul kontrolleridir.

- [ ] Gravity / Snap Turn / Light Intensity kontrolleri ray ile çalışıyor.
- [ ] Tutamaç hover durumunda renk değiştiriyor; Grip ile tutulup taşınabiliyor.
- [ ] Joystick ve PageUp/PageDown, **yalnızca grab sırasında** mesafe ayarlıyor.
- [ ] RESET'e 2 saniye bakınca merkezden büyüyen efekt tamamlanıyor ve panel sıfırlanıyor.
- [ ] Bakış hedef dışına çıkınca ilerleme sıfırlanıyor; RESET'e ray ile tıklamak da çalışıyor.
- [ ] Menü kapatıldıktan sonra `MENÜYÜ AÇ` erişilebilir ve çalışıyor.
- [ ] Console'da yeni kırmızı hata yok; sahne Play Mode dışında kaydedilmiş.

## Dosya yapısı

Aşağıda Bölüm 2 açısından ilgili olan dosyalar gösterilmiştir; tam `Assets/` yapısı daha kapsamlıdır.

```text
Assets/
├── Scenes/
│   └── SampleScene.unity                    # Ana sahne; veya ayrı entegrasyon sahnesi
├── Scripts/
│   ├── LunarPanelController.cs              # Var olan çevre kontrolleri
│   ├── ButtonHoverEffect.cs                 # Var olan UI hover desteği
│   ├── LazyFollowYawOnly.cs                 # Önceki panel takip davranışı
│   └── InteractionModels/
│       └── VRPanelInteractionController.cs  # Bölüm 2 runtime etkileşimleri
├── Samples/XR Interaction Toolkit/3.6.1/    # Starter Assets / XR Device Simulator
└── ...                                     # Ay yüzeyi, gökyüzü, fontlar, materyaller
Packages/
└── manifest.json
ProjectSettings/
README.md
```

**Sorumluluk sınırı:** Bu branch'te panelin mevcut Gravity, Snap Turn, ışık kontrolü ve diğer ekip üyelerinin çalışmaları korunur. Esas değişiklikler `VRPanelInteractionController.cs`, sahne nesneleri ve bu README'dir.

## Kapsam dışı ve dikkat edilmesi gerekenler

- **Hand Tracking & Gestures:** XR Hands üzerinden gerçek el takibi ve jest tanıma **uygulanmadı**. Sunumda teorik karşılaştırma yapılabilir.
- **Eye Tracking:** Gaze örneği gerçek göz izleme değil, **head-gaze** yöntemidir.
- **Near / Far:** Tutamaç XR `XRSimpleInteractable` ile seçilir; aktif interactor yapılandırmasına göre uzaktan da seçilebilir. *Direct interaction* örneğini göstermek için kontrolcüyü fiziksel olarak yakına getirerek test edin.
- **Yayına hazır VR build:** Fiziksel cihazlarda doğrulama ve rahatlık/erişilebilirlik testleri ayrıca gerekir.

## Ekip ve üçüncü taraf kaynaklar

**VR Dersi — Grup 2**

- Aboubacar Sow
- Emre Yasin Yıldan
- Merve Gazioğlu
- Ömer Faruk Sarı
- Eren Dağlı

**VR Etkileşim Modelleri:** Ömer Faruk Sarı.

**Kullanılan paket ve varlıklar:** Unity XR Interaction Toolkit 3.6.1 (Starter Assets / XR Device Simulator), Lunar Landscape 3D, Vibrant 4K Starfield Skybox Pack (ParallelCascades), Orbitron yazı tipi, TextMesh Pro ve URP.
