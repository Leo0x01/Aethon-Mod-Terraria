# BANDA SONORA — VERSIÓN 1 (el archivo de la casa)

La letra: «las viejas no las borres, simplemente dejalas en una carpeta
que se llame algo asi como version 1 de banda sonora».

Aquí viven, intactas byte a byte, las cinco bandas sonoras originales
del mod anteriores a la v6.121 (la cura del zumbido + el remix de
calidad). Esta carpeta está en la RAÍZ del repo — fuera de
`AethonMod/` — para que el `.tmod` no las empaquete ni engorde.

| Archivo | md5 | Qué eran |
|---|---|---|
| `MundoSuenoVacio.ogg`  | `c8187cbd17c827f144c2f2490116e7f1` | «EL SUEÑO DEL VACÍO» 58 BPM (v6.120, con el zumbido del dron) |
| `MundoGarganta.ogg`    | `343ad89184362bd0f08c0ab875dd26cb` | «LA GARGANTA DE PÁGINAS» 92 BPM (v6.120, con el zumbido del dron) |
| `MundoCorazon.ogg`     | `635bec8864220864c2d4cebf04291461` | «EL CORAZÓN COLOSAL» 120 BPM (v6.120) |
| `OleadaHambre.ogg`     | `aa22d4d7e58c9992f49a2d47f0298c63` | «EL FESTÍN DESATADO» 152 BPM (v6.87) |
| `OleadaHambreEpica.ogg`| `8591de7872b41e143b49ff875cb0eb1b` | «LA OLEADA FINAL» 164 BPM (v6.88) |

## Cómo volver a la versión 1 (si algún día las extrañas)

```bash
cp BandaSonoraV1/*.ogg AethonMod/Music/
```

y recompilar el mod. Nada más.

## Qué medía mal en la v1 (el detector, `tools/qc_musica_v6121.py`)

- **MundoSuenoVacio / MundoGarganta**: el dron de fondo era un `saw()`
  sostenido de hasta 40 armónicos SIN filtrar toda la pieza — 8 tonos
  fijos (41–646 Hz) al 100 % de la pista; el peor medido a
  **−3,6 dB rel. RMS** (¡casi tan fuerte como la canción entera!) e
  inmóvil. Ese era el «sumbido constante muy molesto».
- **Las cinco**: componente continua (DC) de 0,12 % a 0,54 %.
- **OleadaHambre / OleadaHambreEpica**: el vorbis q5 con techo 0,89
  reconstruía POR ENCIMA del techo al decodificar — hasta 161 muestras
  clippeadas y miles de saltos de >1,0 por muestra.
