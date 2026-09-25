# Vid2Sprite

Alat berbasis browser (tanpa server, tanpa build tool) untuk mengubah **video** atau **satu gambar karakter** menjadi **sprite sheet** siap pakai di game 2D, lengkap dengan demo battle untuk mencoba hasilnya.

Semua diproses langsung di browser — tidak ada file yang diunggah ke mana pun.

## Fitur

- **Dari video**: pilih rentang waktu, FPS/jumlah frame, dan ukuran output — hasilkan sprite sheet + PNG per-frame.
- **Dari gambar**: upload satu gambar karakter (idle/walk/death) dan animasi dibuat otomatis secara prosedural (image warping per strip), tanpa perlu asset tambahan.
- **Hapus background (chroma key)**: mendukung hijau, biru, magenta, warna custom, atau deteksi otomatis dari pojok gambar — dengan kontrol toleransi, kehalusan tepi, dan despill.
- **21 jenis animasi RPG** siap pakai (idle, walk, run, attack, skill, critical, death, victory, dll) dengan preset jumlah frame & mode loop.
- **Hitbox otomatis**: dihitung dari bounding box piksel non-transparan tiap frame, bisa diperkecil sesuai kebutuhan.
- **Preset ekspor**: simpan kombinasi ukuran, kolom, FPS, dan pengaturan chroma key sebagai preset bernama (tersimpan di `localStorage`), lalu terapkan kembali untuk animasi lain tanpa mengatur ulang dari nol.
- **Koleksi karakter**: kumpulkan beberapa animasi per karakter (autosave ke IndexedDB), lalu unduh semuanya sekaligus sebagai satu paket ZIP berisi sheet, frame, JSON per-animasi, dan `manifest.json`.
- **Demo battle** (`battle.html`): battle turn-based sederhana (serang/skill/bertahan/item) yang langsung memuat pasangan PNG + JSON hasil generator, untuk mengecek apakah sprite sudah pas dipakai di game.

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

## Struktur proyek

```
index.html    → generator sprite (mode video & mode gambar)
battle.html   → demo battle yang memuat hasil generator
README.md     → dokumen ini
```

Tidak ada dependency build — cukup dibuka langsung di browser modern. Satu-satunya library eksternal adalah [JSZip](https://stuk.github.io/jszip/) (dimuat dari CDN) untuk membuat file ZIP.

## Catatan teknis

- **Chroma key** menormalkan warna piksel terhadap kecerahan sebelum dibandingkan dengan warna kunci, sehingga bagian background yang gelap (bayangan) tetap terhapus, bukan hanya area yang benar-benar terang.
- **Animasi dari gambar** membagi gambar jadi 48 strip horizontal lalu menggeser/merotasi tiap strip berbeda sesuai fase animasi (napas untuk idle, ayunan kaki untuk walk, hentakan+jatuh+pantulan untuk death) — jadi tidak butuh rig/tulang seperti animasi skeletal biasa.
- **Preset ekspor** disimpan per-perangkat via `localStorage` (terpisah untuk mode video dan mode gambar), tidak ikut terunduh bersama ZIP koleksi.
- Batas aman: sprite sheet maksimum ±16000px per sisi, cache frame mentah (untuk fitur "terapkan ulang chroma key") dibatasi ±300MB agar tidak membebani memori browser pada video panjang.
