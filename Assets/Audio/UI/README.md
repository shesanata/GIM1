# Folder User Interface Audio (UI) - MazeSpell

Folder ini digunakan untuk menyimpan file asset audio UI (format didukung: `.mp3`, `.wav`, `.ogg`).

## Daftar Efek Suara UI:
1. `UI_ButtonHover.wav` - Suara tick/blip ringan saat tombol disorot (D-Pad, Analog Stick, Keyboard panah/WASD, atau kursor mouse).
2. `UI_ButtonClick.wav` - Suara konfirmasi/klik saat tombol ditekan (Gamepad Cross/A, Enter, Space, atau klik mouse).
3. `UI_Pause.wav` - Suara penjedaan permainan (blip nada turun).
4. `UI_Resume.wav` - Suara pelanjutan permainan (blip nada naik).

> **Catatan Sistem Dual-Mode:**
> Jika file audio eksternal belum dimasukkan ke folder ini, `AudioManager` secara otomatis memutar efek suara UI prosedural yang jernih secara langsung saat runtime.
