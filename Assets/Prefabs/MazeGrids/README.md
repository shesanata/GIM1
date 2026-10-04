# Prefabs - MazeGrids

Folder ini digunakan untuk menyimpan Prefab template Grid Tilemap untuk masing-masing level:

1. `Level1_MazeGrid.prefab` (Grid 9 x 17 - Ground / Earth)
2. `Level2_MazeGrid.prefab` (Grid 9 x 17 - Ground / Earth)
3. `Level3_MazeGrid.prefab` (Grid 11 x 21 - Grass)
4. `Level4_MazeGrid.prefab` (Grid 13 x 25 - Snow / Ice)
5. `Level5_MazeGrid.prefab` (Grid 15 x 29 - Lava / Volcano)

Struktur GameObject di dalam masing-masing Prefab:
```
[LevelX_MazeGrid] (Komponen: Grid + MazeGridBounds)
  ├── Background_Path (Komponen: Tilemap + TilemapRenderer, Sorting Order 0)
  ├── Walls_Obstacles (Komponen: Tilemap + TilemapRenderer, Sorting Order 1 + TilemapCollider2D)
  └── Overlay_Decor   (Komponen: Tilemap + TilemapRenderer, Sorting Order 2)
```

Anda juga dapat me-refresh atau membuat ulang prefab kapan saja melalui menu Unity:
**MazeSpell > Generate Level Assets and Prefabs**.
