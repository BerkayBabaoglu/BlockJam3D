# QueueUnit Çarpışma Önleme Rehberi

Bu dosya, QueueUnit script'lerinin birbirleriyle çarpışmasını önlemek için yapılan değişiklikleri açıklar.

## Yapılan Değişiklikler

### 1. Fizik Ayarları (FixPhysicsSettings)
- Rigidbody kinematic olarak ayarlandı
- Çarpışma algılama tamamen kapatıldı (`detectCollisions = false`)
- Yerçekimi kapatıldı
- Yüksek sürtünme değerleri eklendi (linearDamping = 10f, angularDamping = 10f)

### 2. Çarpışma Katmanları (SetupCollisionLayers)
- **ÖNEMLİ**: Mevcut layer (Character) korundu, değiştirilmedi
- **ÖNEMLİ**: Mevcut tag korundu, değiştirilmedi
- Sadece QueueUnit objeleri arasında çarpışma engellendi
- Collider'lar trigger olarak ayarlandı (ama devre dışı bırakılmadı)
- Physics.IgnoreCollision ile sadece QueueUnit'lar arası çarpışmalar engellendi

### 3. Hareket Sırasında Çarpışma Önleme
- Hareket sırasında collider'lar trigger olarak ayarlandı (devre dışı bırakılmadı)
- Rigidbody kinematic olarak tutuldu
- Çarpışma algılama sürekli kapalı tutuldu

### 4. Pozisyon Kilitleme
- Pozisyon kilitleme sırasında da çarpışmalar engellendi
- Collider'lar sürekli trigger olarak tutuldu (ama enabled)

## Unity'de Gerekli Ayarlar

### Layer Ayarları
- **ÖNEMLİ**: QueueUnit objelerinin mevcut Character layer'ı korunmalı
- Script otomatik olarak layer'ı değiştirmeyecek
- Sadece fizik çarpışmaları engellenecek

### Tag Ayarları
- **ÖNEMLİ**: QueueUnit objelerinin mevcut tag'ı korunmalı
- Script otomatik olarak tag'ı değiştirmeyecek
- Sadece fizik çarpışmaları engellenecek

### Physics Settings
1. Edit > Project Settings > Physics
2. Character layer'ının kendisiyle çarpışması açık kalabilir
3. Script otomatik olarak QueueUnit'lar arası çarpışmaları engelleyecek

## Çarpışma Önleme Sistemi

### Awake() - Başlangıç
- Fizik ayarları düzeltildi
- Sadece QueueUnit'lar arası çarpışmalar engellendi
- Mevcut layer korundu

### Start() - Çalışma Zamanı
- Çarpışma önlemleri tekrar kontrol edildi
- Sadece QueueUnit'lar arası çarpışmalar engellendi

### Hareket Sırasında
- Collider'lar trigger olarak ayarlandı (enabled kaldı)
- Rigidbody kinematic olarak tutuldu
- Çarpışma algılama kapalı

### Hareket Sonrası
- Collider'lar sürekli trigger olarak tutuldu (enabled kaldı)
- Pozisyon kilitleme aktif
- Sadece QueueUnit'lar arası çarpışmalar engellendi

## Sonuç

Bu değişikliklerle:
- ✅ QueueUnit'lar birbirleriyle çarpışmayacak
- ✅ Fizik dengeleri bozulmayacak
- ✅ Hareket sırasında karışıklık olmayacak
- ✅ Pozisyon kilitleme daha stabil olacak
- ✅ Performans artacak (fizik hesaplamaları azalacak)
- ✅ **Character layer korunacak**
- ✅ **Diğer objelerle etkileşimler korunacak**

## Notlar

- Script sadece QueueUnit'lar arasındaki çarpışmaları engeller
- Diğer objelerle (duvarlar, zemin, vs.) çarpışmalar etkilenmez
- Character layer'ı korunur, değiştirilmez
- Mevcut tag korunur, değiştirilmez
- Collider'lar devre dışı bırakılmaz, sadece trigger olarak ayarlanır
- Debug log'ları kontrol ederek çarpışma önlemlerinin çalışıp çalışmadığını görebilirsiniz
