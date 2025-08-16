# Grid Pathfinding System

Bu sistem, karakterlerin grid tabanlı bir dünyada engelleri aşarak hareket etmesini sağlayan A* algoritması tabanlı bir pathfinding çözümüdür.

## Bileşenler

### 1. GridPathfinding.cs
Ana pathfinding sistemi. Grid'i oluşturur ve A* algoritması ile yol bulur.

**Özellikler:**
- Grid boyutları ayarlanabilir
- Hücre boyutu özelleştirilebilir
- Engelleri otomatik algılar
- Debug görselleştirme
- Grid walkability güncelleme

**Kullanım:**
```csharp
// Grid oluştur
GridPathfinding pathfinding = gameObject.AddComponent<GridPathfinding>();
pathfinding.gridWidth = 10;
pathfinding.gridHeight = 10;
pathfinding.cellSize = 1f;

// Yol bul
List<Vector3> path = pathfinding.FindPath(startPos, targetPos);

// Karakteri hareket ettir
pathfinding.StartPathfinding(character.transform, targetPos);
```

### 2. CharacterMovement.cs
Karakterlerin pathfinding sistemi ile hareket etmesini sağlar.

**Özellikler:**
- Mouse tıklama ile hedef belirleme
- WASD ile test hareketi
- Animasyon desteği
- Smooth rotation
- Path visualization

**Kullanım:**
```csharp
// Karaktere ekle
CharacterMovement movement = character.AddComponent<CharacterMovement>();
movement.pathfinding = pathfinding;
movement.moveSpeed = 3f;

// Hedef belirle
movement.SetTargetPosition(targetPos);
```

### 3. PathfindingTest.cs
Sistemi test etmek için hazır test scripti.

**Kontroller:**
- **Space**: Rastgele hedefe pathfinding testi
- **R**: Karakteri başlangıç pozisyonuna sıfırla
- **G**: Grid debug görselleştirmesini aç/kapat

## Kurulum

### 1. Temel Kurulum
1. Sahnede boş bir GameObject oluştur
2. `PathfindingTest` scriptini ekle
3. Test karakterini atayın
4. Grid ayarlarını yapılandırın
5. Play tuşuna basın

### 2. Manuel Kurulum
```csharp
// Pathfinding sistemi oluştur
GameObject pathfindingGO = new GameObject("GridPathfinding");
GridPathfinding pathfinding = pathfindingGO.AddComponent<GridPathfinding>();

// Grid ayarları
pathfinding.gridWidth = 10;
pathfinding.gridHeight = 10;
pathfinding.cellSize = 1f;
pathfinding.gridOrigin = Vector3.zero;

// Karakter hareketi ekle
CharacterMovement movement = character.AddComponent<CharacterMovement>();
movement.pathfinding = pathfinding;
```

## Grid Yapılandırması

### Grid Boyutları
- `gridWidth`: Grid genişliği (X ekseni)
- `gridHeight`: Grid yüksekliği (Z ekseni)
- `cellSize`: Her hücrenin boyutu
- `gridOrigin`: Grid'in dünya koordinatlarındaki başlangıç noktası

### Hücre Merkezleri
Karakterler her zaman hücre merkezlerinde hareket eder:
```csharp
// Hücre merkezi hesaplama
Vector3 cellCenter = gridOrigin + new Vector3(
    x * cellSize + cellSize * 0.5f,
    0,
    z * cellSize + cellSize * 0.5f
);
```

## Engeller ve Walkability

### Otomatik Algılama
Sistem `Physics.CheckSphere` kullanarak engelleri otomatik algılar:
```csharp
bool walkable = !Physics.CheckSphere(worldPoint, cellSize * 0.4f, obstacleLayer);
```

### Manuel Güncelleme
Engeller değiştiğinde grid'i güncelleyin:
```csharp
pathfinding.UpdateGridWalkability();
```

## A* Algoritması

### F Cost Hesaplama
```csharp
fCost = gCost + hCost
```
- **gCost**: Başlangıçtan bu noktaya olan maliyet
- **hCost**: Bu noktadan hedefe olan tahmini maliyet (heuristic)

### Komşu Hücreler
Her hücre 8 yönde komşu hücrelere hareket edebilir:
- 4 ana yön (kuzey, güney, doğu, batı)
- 4 çapraz yön

## Performans Optimizasyonu

### Grid Boyutu
- Küçük grid'ler (10x10) hızlı pathfinding sağlar
- Büyük grid'ler (100x100) daha yavaş olabilir
- Hücre boyutu performansı etkiler

### Layer Masks
Sadece gerekli layer'ları kontrol edin:
```csharp
pathfinding.obstacleLayer = LayerMask.GetMask("Obstacles", "Walls");
```

## Debug ve Gizmos

### Grid Görselleştirme
- Yeşil hücreler: Yürünebilir
- Kırmızı hücreler: Engelli
- Sarı çerçeve: Grid sınırları

### Path Görselleştirme
- Mavi çizgiler: Bulunan yol
- Yeşil küreler: Waypoint'ler
- Kırmızı küre: Hedef pozisyon

## Örnek Kullanım Senaryoları

### 1. Basit Hareket
```csharp
// Karakteri hedefe gönder
characterMovement.SetTargetPosition(targetPosition);
```

### 2. Dinamik Hedef
```csharp
// Oyuncu tıkladığında hareket et
if (Input.GetMouseButtonDown(0))
{
    Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
    if (Physics.Raycast(ray, out hit))
    {
        characterMovement.SetTargetPosition(hit.point);
    }
}
```

### 3. Çoklu Karakter
```csharp
// Her karakter için ayrı movement component
foreach (GameObject character in characters)
{
    CharacterMovement movement = character.GetComponent<CharacterMovement>();
    movement.pathfinding = pathfinding;
}
```

## Sorun Giderme

### Yol Bulunamıyor
1. Grid boyutlarını kontrol edin
2. Hücre boyutunu kontrol edin
3. Grid origin'i kontrol edin
4. Engellerin doğru layer'da olduğundan emin olun

### Karakter Hareket Etmiyor
1. Pathfinding sistemi atanmış mı?
2. Karakter pozisyonu grid içinde mi?
3. Hedef pozisyon walkable mı?
4. Movement component aktif mi?

### Performans Sorunları
1. Grid boyutunu küçültün
2. Hücre boyutunu artırın
3. Layer mask'i sınırlayın
4. Debug görselleştirmeyi kapatın

## Özelleştirme

### Yeni Hareket Türleri
```csharp
// Farklı hareket hızları
public enum MovementType
{
    Walk = 0,
    Run = 1,
    Crawl = 2
}

// Hareket türüne göre hız ayarla
switch (movementType)
{
    case MovementType.Walk:
        moveSpeed = 3f;
        break;
    case MovementType.Run:
        moveSpeed = 6f;
        break;
    case MovementType.Crawl:
        moveSpeed = 1f;
        break;
}
```

### Özel Heuristic Fonksiyonları
```csharp
// Manhattan distance
int GetManhattanDistance(Node a, Node b)
{
    return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.z - b.z);
}

// Euclidean distance
int GetEuclideanDistance(Node a, Node b)
{
    float dx = a.x - b.x;
    float dz = a.z - b.z;
    return Mathf.RoundToInt(Mathf.Sqrt(dx * dx + dz * dz));
}
```

## Lisans
Bu sistem Unity projelerinde ücretsiz kullanılabilir.
