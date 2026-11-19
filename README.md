# Gezegen Atlası AR

**Talip Bingöl** · 2021555006  
Çukurova Üniversitesi · Bilgisayar Mühendisliği · Birinci Öğretim

Unity ile geliştirilmiş etkileşimli bir gezegen atlası. Proje, [Planets AR ders örneğinden](https://www.youtube.com/watch?v=FJAO6jDYljs&t=105s) yola çıkarak gezegen yerleştirme ve hareketli Güneş Sistemi sunar.

## Özellikler

- Sekiz gezegen arasından seçim; dokunarak yerleştirme, sürükleyerek taşıma ve iki parmakla boyut değiştirme.
- Seçili nesneyi silme, yerleştirilenleri kameranın önüne ortalama.
- Hareketli yörünge modu ve ayarlanabilir yörünge hızı.
- AR desteklenmeyen cihazlarda döndürülebilen 3B önizleme.
- Uyumlu cihazlarda ARCore derinliğiyle gerçek nesnelerin gezegenleri örtebilmesi.

## Çalıştırma

1. Projeyi **Unity 6.3 (`6000.3.28f1`)** ile aç. Android için **Build Support, SDK/NDK ve OpenJDK** kurulu olmalı.
2. `Assets/Scenes/Main.unity` sahnesini aç. Sahne yoksa **Tools → Gezegen Atlası → 1 • Projeyi Hazırla** seç.
3. Bilgisayarda **Play** ile 3B önizlemeyi çalıştır.
4. APK için **Build Profiles → Android → Switch Platform** yap; **XR Plug-in Management → Android → ARCore** seçili olsun.
5. **Tools → Gezegen Atlası → 2 • Android APK Oluştur** menüsünü kullan. Çıktı: `Build/GezegenAtlasi-Android.apk`.

**Teknik not:** Yerleştirme, kameradan çıkan ışın üzerinde sabit uzaklıktadır; gerçek yüzey mesafesini ölçmez. El/engel örtmesi yalnızca cihaz ARCore Depth destekliyorsa çalışabilir. Gezegen dokularının kaynakları ve lisansları `Assets/Resources/PlanetTextures/ATTRIBUTION.txt` dosyasındadır.
