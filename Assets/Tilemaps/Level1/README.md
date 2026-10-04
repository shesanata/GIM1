# Level 1 - Tilemap Assets Guide

## Tema: Ground / Earth (Grid 9 x 17)

### 1. Tile Jalan / Jalur (Background_Path)
- **Nama yang Disarankan**: `Tile_Earth_Path.png`
- **Visual**: Tanah padat berwarna cokelat hangat (*packed brown earth*).
- **Fungsi**: Sebagai lantai/jalur walkable maze tempat pemain bernavigasi.

### 2. Tile Dinding / Penghalang (Walls_Obstacles)
- **Nama yang Disarankan**: `Tile_Stone_Wall.png`, `Tile_Rock_Border.png`
- **Visual**: Tumpukan batu cadas/tembok batu kasar alami bersudut tegas.
- **Fungsi**: Sebagai rintangan/dinding labirin (dilengkapi `TilemapCollider2D`).

### 3. Tile Dekorasi / Aksen (Overlay_Decor)
- **Nama yang Disarankan**: `Tile_Earth_Pebbles.png`, `Tile_Small_Crack.png`
- **Visual**: Kerikil kecil, retakan halus, atau bercak tanah unik untuk variasi.

---
### Rekomendasi Format & Pengaturan Unity:
- **Format**: PNG dengan transparansi (RGBA 32-bit).
- **Resolusi Tile**: 64x64 px atau 128x128 px (persegi).
- **Pixels Per Unit (PPU)**: Sesuaikan dengan resolusi (misal 64 jika sprite 64x64 px) agar 1 tile = 1 unit cell.
- **Filter Mode**: Point (no filter) jika pixel art, atau Bilinear jika smooth hand-drawn/vector.
