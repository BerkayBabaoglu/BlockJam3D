# Grid System Test Talimatları

## Sorun Çözüldü! 🎉

### Yapılan Düzeltmeler:

1. **GridData.cs** - Bounds checking ve null safety eklendi
2. **GridDataIO.cs** - JsonUtility 2D array sorunu çözüldü
3. **GridGenerator.cs** - Debug bilgileri eklendi ve spawn mantığı güncellendi
4. **level1.json** - Yeni format ile güncellendi
5. **GridEditorWindow.cs** - Null safety eklendi ve renk kodları güncellendi

### Spawn Mantığı:

- **0**: Boş hücre - Hiçbir şey spawn edilmez (Beyaz)
- **1**: Grass - `grassPrefab` spawn edilir (Yeşil)
- **2**: Kırmızı karakter - `characterPrefabs[0]` spawn edilir (Kırmızı)
- **3**: Yeşil karakter - `characterPrefabs[1]` spawn edilir (Yeşil)
- **4**: Mavi karakter - `characterPrefabs[2]` spawn edilir (Mavi)
- **5**: Sarı karakter - `characterPrefabs[3]` spawn edilir (Sarı)

### Test Etmek İçin:

1. **Unity'de sahneyi aç**
2. **GridGenerator GameObject'i bul** (MeshRenderer component'i olan bir GameObject)
3. **Inspector'da prefab'ları ata:**
   - `grassPrefab`: Grass prefab (örn: GroundCube)
   - `characterPrefabs`: 4 farklı karakter prefab (örn: character5, Whiteman, barrel, key)
4. **Play tuşuna bas**
5. **Console'da debug mesajlarını kontrol et**

### Beklenen Sonuç:

- Console'da detaylı debug bilgileri görünmeli
- Grid oluşturulmalı ve objeler spawn edilmeli
- Toplam spawn edilen obje sayısı görünmeli
- 0 değerli hücrelerde hiçbir şey spawn edilmemeli

### Eğer Hala Çalışmıyorsa:

1. **Console'da hata mesajlarını kontrol et**
2. **GridGenerator GameObject'inin MeshRenderer'ı olduğundan emin ol**
3. **Prefab'ların doğru atandığından emin ol**
4. **characterPrefabs array'inin 4 eleman olduğundan emin ol**
5. **level1.json dosyasının doğru konumda olduğundan emin ol**

### Debug Bilgileri:

Tüm script'lerde detaylı debug bilgileri eklendi. Console'da şunları göreceksin:
- Grid boyutları
- Hücre değerleri
- Prefab bulunma durumu
- Spawn edilen obje sayısı
- Hata durumları

### Test Script:

`GridTest.cs` script'i de eklendi. Bu script'i herhangi bir GameObject'e ekleyerek test edebilirsin.

### Grid Editor:

Tools > Grid Level Editor menüsünden grid'i düzenleyebilirsin. Hücre renkleri:
- 0: Beyaz (boş)
- 1: Yeşil (grass)
- 2: Kırmızı (karakter)
- 3: Yeşil (karakter)
- 4: Mavi (karakter)
- 5: Sarı (karakter)
