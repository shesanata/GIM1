# Folder Background Music (BGM) - MazeSpell

Folder ini digunakan untuk menyimpan file asset audio Background Music (format didukung: `.mp3`, `.wav`, `.ogg`).

## Struktur File yang Direkomendasikan:
1. `BGM_MainMenu.mp3` / `.wav` / `.ogg`
   - Musik petualangan yang tenang dan rileks untuk Splash Screen, Main Menu, dan Level Select.
2. `BGM_Level1_2.mp3` / `.wav` / `.ogg`
   - Musik eksplorasi ringan untuk Level 1 (Ground / Earth) dan Level 2 (Stone / Earth).
3. `BGM_Level3_4.mp3` / `.wav` / `.ogg`
   - Musik petualangan lebih menegangkan untuk Level 3 (Grass / Nature) dan Level 4 (Snow / Frost).
4. `BGM_Level5.mp3` / `.wav` / `.ogg`
   - Musik petualangan intens & klimaks bertempo cepat untuk Level 5 (Lava / Volcano).

> **Catatan Sistem Dual-Mode:**
> Jika file audio eksternal belum dimasukkan ke folder ini, `AudioManager` secara otomatis memutar musik latar prosedural (*synthesized procedural loop*) yang merdu dan bebas hambatan secara langsung saat runtime.
