# Folder Sound Effects (SFX) - MazeSpell

Folder ini digunakan untuk menyimpan file asset audio Sound Effects gameplay (format didukung: `.mp3`, `.wav`, `.ogg`).

## Daftar Efek Suara Gameplay:
1. `SFX_CollectLetter.wav` - Suara lonceng/chime jernih saat pemain mengoleksi huruf kata target.
2. `SFX_HazardImpact.wav` - Suara benturan berat/impact saat pemain menabrak hazard.
3. `SFX_DamageTaken.wav` - Suara damage/kehilangan nyawa pemain.
4. `SFX_GoalLocked.wav` - Suara peringatan/warning saat pemain mendekati goal sebelum kata target selesai.
5. `SFX_GoalUnlocked.wav` - Suara pembukaan portal/unlock saat semua huruf terkumpul.
6. `SFX_WordComplete.wav` - Suara fanfare kemenangan penyelesaian kata target.
7. `SFX_LevelTransition.wav` - Suara whoosh/transisi perpindahan antar level.
8. `SFX_GameOver.wav` - Suara somber/kalah saat 3 nyawa habis.
9. `SFX_GameComplete.wav` - Suara fanfare kemenangan akbar saat seluruh 5 level tamat.

> **Catatan Sistem Dual-Mode:**
> Jika file audio eksternal belum dimasukkan ke folder ini, `AudioManager` secara otomatis memutar efek suara prosedural (*synthesized procedural audio*) yang responsif secara langsung saat runtime.
