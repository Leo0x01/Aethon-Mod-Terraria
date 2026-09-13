
# ⚠️ PROCEDIMIENTO DE RECUPERACIÓN — LEER PRIMERO

> Si el sandbox se borra/resetea, TODA la memoria del proyecto se recupera desde aquí (GitHub).

**Repo**: `https://github.com/Leo0x01/Aethon-Mod-Terraria` (rama `main`)
**Clone**: `git clone https://github.com/Leo0x01/Aethon-Mod-Terraria.git AethonMod` (si pide credenciales para push: el PAT lo entrega el usuario directamente)
**Este worklog = la memoria completa**: histórico antiguo (Task 0 → V5.63) + sandbox 2024-2025 (Task 1 → 17) + entradas futuras.

## Estado actual
- Versión: ver `AethonMod/build.txt` (en este commit: **6.07**) y `AethonMod/CHANGES.md` (historial de versiones)
- Últimas versiones: v6.05 (agujero negro carmesí = copia exacta del funcional + parámetros), v6.06 (10 alas end-game entregadas al jugador), v6.07 (fix `[AutoloadEquip(EquipType.Wings)]` ×8 — el mod VUELVE A CARGAR)
- PENDIENTE de verificación en juego por el usuario: v6.05 (agujero carmesí) + las 10 alas (vuelo, frames, muelles de las 2 de luz, dusts, sonidos)

## Entorno de compilación (reconstruir tras un reset — detalles en Task ID 17)
- .NET 8 SDK + runtime .NET 6 (para ilspycmd 8.2) en `/home/z/.dotnet`
- tModLoader **v2026.07.3.0** (GitHub releases, el MISMO release del usuario) descomprimido en `/tmp/tml`
- Referencias: `tModLoader.dll` (raíz del zip, ahí vive la API desde 2025) + `FNA.dll` + `ReLogic.dll` (`Libraries/ReLogic/1.0.0/`) + `TerrariaHooks.dll` (`Libraries/TerrariaHooks/0.0.0.0/`) + `Steamworks.NET.dll`
- Proyecto de verificación: `/home/z/.verify/verify.csproj` (se crea con las referencias anteriores)
- Compilar: `cd /home/z/.verify && dotnet build` → debe dar **0 errores 0 warnings**

## Reglas de la sesión
- **Responder SIEMPRE en español**
- Worklog con formato: `Task ID / Agent / Task / Work Log / Test / Next`
- El usuario prueba en SU máquina: tModLoader → Develop Mods → Build → revisar `client.log`
- Sandbox solo para desarrollo/compilación, nunca para ejecutar el juego
