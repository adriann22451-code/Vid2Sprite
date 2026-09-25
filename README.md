# Vid2Sprite

Alat berbasis browser (tanpa server, tanpa build tool) untuk mengubah **video** atau **satu gambar karakter** menjadi **sprite sheet** siap pakai di game 2D, lengkap dengan demo battle untuk mencoba hasilnya.

Semua diproses langsung di browser — tidak ada file yang diunggah ke mana pun.

## Fitur

- **Dari video**: pilih rentang waktu, FPS/jumlah frame, dan ukuran output — hasilkan sprite sheet + PNG per-frame.
- **Dari gambar**: upload satu gambar karakter (idle/walk/death) dan animasi dibuat otomatis secara prosedural (image warping per strip), tanpa perlu asset tambahan.
- **Hapus background (chroma key)**: mendukung hijau, biru, magenta, warna custom, atau deteksi otomatis dari pojok gambar — dengan kontrol toleransi, kehalusan tepi, dan despill.
- **21 jenis animasi RPG** siap pakai (idle, walk, run, attack, skill, critical, death, victory, dll) dengan preset jumlah frame & mode loop.
- **Hitbox otomatis**: dihitung dari bounding box piksel non-transparan tiap frame, bisa diperkecil sesuai kebutuhan.
- **Edit frame**: hapus frame yang tidak perlu atau pindahkan urutannya sebelum diunduh — sheet, hitbox, dan pratinjau otomatis dibuat ulang mengikuti perubahan.
- **Onion skinning**: jeda pratinjau animasi dan lihat frame sebelum (biru transparan) & sesudah (merah transparan) di belakang frame yang sedang aktif, untuk mengecek gerakan yang "meloncat" sebelum diekspor.
- **Padding antar frame**: tambahkan jarak kosong (px) di sekitar tiap frame pada sprite sheet, supaya tidak ada "bleeding" warna dari frame sebelah saat sheet di-scale/di-filter oleh game engine. Bisa dicek langsung lewat opsi **tampilkan grid** di pratinjau (garis putus-putus, murni visual — tidak ikut ke file yang diunduh).
- **Preset ekspor**: simpan kombinasi ukuran, kolom, FPS, dan pengaturan chroma key sebagai preset bernama (tersimpan di `localStorage`), lalu terapkan kembali untuk animasi lain tanpa mengatur ulang dari nol.
- **Koleksi karakter**: kumpulkan beberapa animasi per karakter (autosave ke IndexedDB), lalu unduh semuanya sekaligus sebagai satu paket ZIP berisi sheet, frame, JSON per-animasi (format bawaan, Aseprite, dan Godot `.tres` versi 3 & 4), dan `manifest.json`. Menghapus animasi (satu per satu atau sekaligus) bisa diurungkan selama beberapa detik lewat tombol "Urungkan" yang muncul.
- **Demo battle** (`battle.html`): battle turn-based sederhana (serang/skill/bertahan/item) yang langsung memuat pasangan PNG + JSON hasil generator, untuk mengecek apakah sprite sudah pas dipakai di game.
- **Test report** di demo battle: tiap slot PNG+JSON yang dipilih otomatis divalidasi (field JSON wajib, ukuran PNG cocok dengan grid, koordinat frame, hitbox) dan ditandai lolos/peringatan/gagal beserta alasannya.
- **Import Unity** (`unity/Vid2SpriteImporter.cs`): skrip Editor sekali-pasang yang meng-slice sheet PNG jadi sprite dan bikin `AnimationClip` otomatis dari JSON hasil ekspor tool ini — lihat bagian [Unity](#unity) di bawah.

## Cara pakai

1. Buka `index.html` di browser.
2. Pilih tab **Dari video** atau **Dari gambar**.
3. Atur jenis animasi, rentang waktu/parameter, ukuran, dan (opsional) hapus background.
4. Tekan **Pratinjau frame dalam rentang ini** untuk mengecek hasil sebelum proses penuh.
5. Tekan **Buat sprite sheet** / **Buat animasi**.
6. Unduh hasilnya:
   - **Unduh semua frame (ZIP)** — PNG per-frame.
   - **Unduh sheet PNG** — satu file sprite sheet.
   - **Unduh data JSON** — metadata (ukuran frame, grid, FPS, loop, hitbox).
   - **Unduh JSON (format Aseprite)** — metadata yang sama, ditulis ulang ke format Aseprite JSON (hash) supaya bisa dibaca tool/engine lain yang sudah mendukung format itu (mis. Phaser, importer pihak ketiga di Unity/Godot).
   - **Unduh SpriteFrames (Godot 4 .tres)** / **Unduh SpriteFrames (Godot 3 .tres)** — resource native Godot (`SpriteFrames`), tiap frame jadi `AtlasTexture` yang me-region sheet PNG yang sama. Dua versi disediakan karena format resource teks Godot 3 dan 4 tidak kompatibel. Taruh file `.tres` satu folder dengan PNG sheet-nya, lalu pakai langsung di node `AnimatedSprite2D` (Godot 4) / `AnimatedSprite` (Godot 3) tanpa plugin tambahan.
7. Untuk membuat satu karakter dengan banyak animasi: ulangi proses di atas, tekan **Tambah animasi ini ke koleksi** tiap selesai, lalu **Unduh koleksi (ZIP)** di akhir.
8. Buka `battle.html` untuk mencoba sprite yang sudah diunduh di battle demo (pilih file PNG + JSON per slot animasi).

## Format output

### `<nama>_<animasi>.json` (metadata per animasi)

```json
{
  "image": "walk_sheet.png",
  "animation": "walk",
  "frameWidth": 128,
  "frameHeight": 128,
  "columns": 4,
  "rows": 2,
  "frameCount": 8,
  "fps": 12,
  "loop": true,
  "padding": 0,
  "frames": [{ "index": 0, "x": 0, "y": 0, "w": 128, "h": 128, "hitbox": {...} }, ...],
  "hitbox": { "x": 38, "y": 12, "w": 51, "h": 102 }
}
```

### `manifest.json` (koleksi karakter, ZIP)

```json
{
  "character": "karakter",
  "manifestVersion": 2,
  "generator": "Vid2Sprite",
  "generatedAt": "2026-09-25T10:00:00.000Z",
  "animations": {
    "walk": {
      "sheet": "walk/walk_sheet.png",
      "data": "walk/walk.json",
      "frames": 8,
      "fps": 12,
      "loop": true,
      "frameWidth": 128,
      "frameHeight": 128,
      "columns": 4,
      "rows": 2,
      "hitbox": {...},
      "addedAt": "2026-09-25T09:58:00.000Z"
    }
  }
}
```

`manifestVersion` naik setiap kali struktur manifest berubah secara tidak kompatibel, supaya tool/loader di sisi game bisa mengecek kompatibilitas sebelum membaca paket. `addedAt` per animasi mencatat kapan animasi itu ditambahkan ke koleksi.

## Unity

Berbeda dari Godot, Unity tidak punya format resource teks sederhana yang bisa ditulis langsung dari browser — slicing sprite & pembuatan `AnimationClip` di Unity itu aksi Editor yang butuh konteks project (asset database, GUID, dsb), bukan cuma data. Karena itu integrasinya bukan tombol ekspor tambahan di `index.html`, tapi satu skrip Editor C# (`unity/Vid2SpriteImporter.cs`) yang **memakai langsung PNG + JSON native** yang sudah dihasilkan tool ini — tidak perlu format ekspor baru.

Cara pakai:

1. Taruh `Vid2SpriteImporter.cs` di dalam folder `Assets/Editor/` pada project Unity kamu (buat foldernya kalau belum ada; nama `Editor` wajib persis itu — konvensi Unity supaya skrip ini tidak ikut ke build).
2. Taruh pasangan `<nama>_sheet.png` + `<nama>.json` (hasil **Unduh sheet PNG** + **Unduh data JSON**, atau dari folder per-animasi di **Unduh koleksi (ZIP)**) di folder yang sama di dalam `Assets/`.
3. Di Project window, klik kanan file PNG-nya → **Vid2Sprite → Import Sprite Sheet + Animation**.

Yang otomatis dikerjakan skrip itu:
- Set Texture Import Settings PNG jadi **Sprite Mode = Multiple**, lalu iris grid-nya persis sesuai `frameWidth`/`frameHeight`/`columns`/`rows`/`padding` dari JSON (tidak perlu buka Sprite Editor manual).
- Buat `AnimationClip` (`.anim`) di folder yang sama, satu keyframe `SpriteRenderer.sprite` per frame, mengikuti `fps` dan `loop` dari JSON.

Catatan: pivot tiap sprite di-set bottom-center (titik pijak umum karakter 2D) dan filter mode di-set Point (cocok pixel art) — keduanya bisa diubah manual di Inspector kalau proyekmu butuh setting lain. Skrip ini tidak memakai field `hitbox` dari JSON; itu tetap urusan collider Unity kamu sendiri.

## Struktur proyek

```
index.html              → generator sprite (mode video & mode gambar)
battle.html              → demo battle yang memuat hasil generator, plus test report per slot
parallax.html            → susun beberapa gambar jadi layer parallax background, atau potong 1 gambar jadi beberapa layer
depth-parallax.html      → lukis depth map manual dari 1 gambar → animasi parallax 2.5D (WebGL) + export video/model 3D (.glb/.obj)
unity/Vid2SpriteImporter.cs → skrip Editor Unity untuk import otomatis dari PNG+JSON native tool ini
README.md                → dokumen ini
```

Tidak ada dependency build — cukup dibuka langsung di browser modern. `index.html`, `battle.html`, dan `parallax.html` sepenuhnya offline (satu-satunya library eksternal adalah [JSZip](https://stuk.github.io/jszip/) dari CDN, untuk `index.html`). `depth-parallax.html` butuh koneksi internet saat dibuka karena memuat [Three.js](https://threejs.org/) dari CDN untuk fitur Model 3D.

## Catatan teknis

- **Chroma key** menormalkan warna piksel terhadap kecerahan sebelum dibandingkan dengan warna kunci, sehingga bagian background yang gelap (bayangan) tetap terhapus, bukan hanya area yang benar-benar terang.
- **Animasi dari gambar** membagi gambar jadi 48 strip horizontal lalu menggeser/merotasi tiap strip berbeda sesuai fase animasi (napas untuk idle, ayunan kaki untuk walk, hentakan+jatuh+pantulan untuk death) — jadi tidak butuh rig/tulang seperti animasi skeletal biasa.
- **Preset ekspor** disimpan per-perangkat via `localStorage` (terpisah untuk mode video dan mode gambar), tidak ikut terunduh bersama ZIP koleksi.
- Batas aman: sprite sheet maksimum ±16000px per sisi, cache frame mentah (untuk fitur "terapkan ulang chroma key") dibatasi ±300MB agar tidak membebani memori browser pada video panjang.
