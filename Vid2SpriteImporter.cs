// Vid2SpriteImporter.cs
//
// Editor script untuk Unity. Taruh file ini di dalam folder "Assets/Editor/" pada project Unity
// kamu (buat foldernya kalau belum ada — nama "Editor" wajib persis itu, itu konvensi Unity untuk
// kode yang hanya jalan di Editor, bukan ikut ke build).
//
// Cara pakai:
//   1. Ekspor animasi dari Vid2Sprite seperti biasa (tombol "Unduh sheet PNG" + "Unduh data JSON"),
//      atau pakai isi folder per-animasi dari "Unduh koleksi (ZIP)".
//   2. Taruh pasangan "<nama>_sheet.png" + "<nama>.json" di folder yang sama di dalam Assets/
//      project Unity kamu (nama file JSON boleh "<nama>.json" atau "<nama>_sheet.json").
//   3. Klik kanan file PNG-nya di Project window → Vid2Sprite → Import Sprite Sheet + Animation.
//
// Yang dilakukan script ini:
//   - Mengatur Texture Import Settings PNG itu jadi Sprite Mode = Multiple, lalu mengiris grid-nya
//     persis sesuai frameWidth/frameHeight/columns/rows/padding yang ada di JSON (tidak perlu buka
//     Sprite Editor manual).
//   - Membuat AnimationClip (.anim) di folder yang sama, dengan satu keyframe per frame pada
//     SpriteRenderer.sprite, mengikuti fps & loop dari JSON.
//
// Catatan:
//   - Pivot tiap sprite di-set ke (0.5, 0) alias bottom-center, karena itu titik pijak paling umum
//     untuk karakter 2D. Ubah manual di Sprite Editor kalau proyekmu butuh pivot lain.
//   - "Sprite Pixels Per Unit" otomatis di-set sama dengan frameHeight, jadi satu karakter tingginya
//     kira-kira 1 unit Unity. Sesuaikan lagi di Inspector kalau skalanya belum pas dengan tile/asset
//     lain di scene kamu.
//   - filterMode di-set ke Point (cocok untuk pixel art / sprite tanpa anti-alias). Ganti ke
//     Bilinear di Inspector kalau sumbernya bukan pixel art.
//   - Hitbox dari JSON tidak dipakai di sini (itu urusan collider Unity kamu sendiri) — script ini
//     cuma menangani slicing sprite + animasi.

using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class Vid2SpriteImporter
{
    [MenuItem("Assets/Vid2Sprite/Import Sprite Sheet + Animation", true)]
    private static bool ValidateImport()
    {
        Texture2D tex = Selection.activeObject as Texture2D;
        if (tex == null) return false;
        string texPath = AssetDatabase.GetAssetPath(tex);
        return FindJsonPath(texPath) != null;
    }

    [MenuItem("Assets/Vid2Sprite/Import Sprite Sheet + Animation")]
    private static void Import()
    {
        Texture2D tex = Selection.activeObject as Texture2D;
        string texPath = AssetDatabase.GetAssetPath(tex);
        string jsonPath = FindJsonPath(texPath);
        if (jsonPath == null)
        {
            Debug.LogError("Vid2Sprite: tidak ketemu file JSON pasangannya di folder yang sama.");
            return;
        }

        SheetMeta meta;
        try
        {
            meta = JsonUtility.FromJson<SheetMeta>(File.ReadAllText(jsonPath));
        }
        catch (System.Exception e)
        {
            Debug.LogError("Vid2Sprite: gagal membaca JSON — " + e.Message);
            return;
        }
        if (meta == null || meta.frames == null || meta.frames.Length == 0)
        {
            Debug.LogError("Vid2Sprite: JSON ini tidak punya data frames. Pastikan ini file JSON hasil ekspor Vid2Sprite.");
            return;
        }

        SliceTexture(texPath, meta);
        AssetDatabase.ImportAsset(texPath, ImportAssetOptions.ForceUpdate);
        BuildAnimationClip(texPath, meta);
        AssetDatabase.SaveAssets();
        Debug.Log($"Vid2Sprite: selesai import \"{meta.animation}\" ({meta.frames.Length} frame, {meta.fps} fps, loop={meta.loop}).");
    }

    // <nama>_sheet.png -> coba "<nama>.json" dulu (format ekspor tunggal Vid2Sprite),
    // lalu "<nama>_sheet.json" (kalau file di-rename manual jadi ikut nama PNG-nya)
    private static string FindJsonPath(string texPath)
    {
        if (string.IsNullOrEmpty(texPath)) return null;
        string dir = Path.GetDirectoryName(texPath);
        string baseName = Path.GetFileNameWithoutExtension(texPath); // "<nama>_sheet"
        string strippedName = baseName.EndsWith("_sheet") ? baseName.Substring(0, baseName.Length - "_sheet".Length) : baseName;

        string candidateA = Path.Combine(dir, strippedName + ".json").Replace('\\', '/');
        string candidateB = Path.Combine(dir, baseName + ".json").Replace('\\', '/');
        if (File.Exists(candidateA)) return candidateA;
        if (File.Exists(candidateB)) return candidateB;
        return null;
    }

    private static void SliceTexture(string texPath, SheetMeta meta)
    {
        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(texPath);
        if (importer == null)
        {
            Debug.LogError("Vid2Sprite: gagal ambil TextureImporter untuk " + texPath);
            return;
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.filterMode = FilterMode.Point;
        importer.spritePixelsPerUnit = meta.frameHeight > 0 ? meta.frameHeight : 100;

        int pad = meta.padding;
        int cols = meta.columns > 0 ? meta.columns : 1;
        int rows = meta.rows > 0 ? meta.rows : Mathf.CeilToInt(meta.frames.Length / (float)cols);
        int textureHeight = rows * meta.frameHeight + pad * (rows + 1);

        SpriteMetaData[] slices = new SpriteMetaData[meta.frames.Length];
        for (int i = 0; i < meta.frames.Length; i++)
        {
            FrameMeta f = meta.frames[i];
            // JSON Vid2Sprite pakai origin kiri-atas (khas canvas web), Unity pakai origin kiri-bawah
            float rectY = textureHeight - f.y - f.h;
            slices[i] = new SpriteMetaData
            {
                name = $"{meta.animation}_{i}",
                rect = new Rect(f.x, rectY, f.w, f.h),
                pivot = new Vector2(0.5f, 0f),
                alignment = (int)SpriteAlignment.BottomCenter
            };
        }

#pragma warning disable 618 // TextureImporter.spritesheet ditandai deprecated di Unity versi baru, tapi masih berfungsi dan belum ada API pengganti yang lebih simpel untuk slicing lewat skrip
        importer.spritesheet = slices;
#pragma warning restore 618

        EditorUtility.SetDirty(importer);
        importer.SaveAndReimport();
    }

    private static void BuildAnimationClip(string texPath, SheetMeta meta)
    {
        string dir = Path.GetDirectoryName(texPath);
        float fps = meta.fps > 0 ? meta.fps : 10f;

        Sprite[] sprites = AssetDatabase.LoadAllAssetsAtPath(texPath)
            .OfType<Sprite>()
            .Where(s => s.name.StartsWith(meta.animation + "_"))
            .OrderBy(s => int.Parse(s.name.Substring((meta.animation + "_").Length)))
            .ToArray();

        if (sprites.Length == 0)
        {
            Debug.LogError("Vid2Sprite: slicing selesai tapi tidak ada sprite yang bisa dibaca kembali — cek Sprite Editor pada texture ini.");
            return;
        }

        AnimationClip clip = new AnimationClip { frameRate = fps };
        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = meta.loop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        EditorCurveBinding binding = new EditorCurveBinding
        {
            type = typeof(SpriteRenderer),
            path = "",
            propertyName = "m_Sprite"
        };

        ObjectReferenceKeyframe[] keyframes = new ObjectReferenceKeyframe[sprites.Length];
        for (int i = 0; i < sprites.Length; i++)
        {
            keyframes[i] = new ObjectReferenceKeyframe { time = i / fps, value = sprites[i] };
        }
        AnimationUtility.SetObjectReferenceCurve(clip, binding, keyframes);

        string clipPath = Path.Combine(dir, meta.animation + ".anim").Replace('\\', '/');
        AnimationClip existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
        if (existing != null) AssetDatabase.DeleteAsset(clipPath);
        AssetDatabase.CreateAsset(clip, clipPath);
    }

    [System.Serializable]
    private class FrameMeta
    {
        public int index, x, y, w, h;
    }

    [System.Serializable]
    private class SheetMeta
    {
        public string image;
        public string animation;
        public int frameWidth, frameHeight, columns, rows, frameCount, padding;
        public float fps;
        public bool loop;
        public FrameMeta[] frames;
        // "hitbox" sengaja tidak dideklarasikan di sini — JsonUtility mengabaikan field JSON yang
        // tidak dipetakan ke properti C#, jadi aman walau strukturnya tidak dipakai.
    }
}
