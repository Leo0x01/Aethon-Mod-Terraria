# v6.146 — LA SESIÓN DE LOS GESTOS

## Los 12 puntos

1. **Vestido de la Forma**: visible al evolucionar — con el libro en la **hotbar** (no en la mano).
2. **Scroll curado**: `PlayerInput.LockVanillaMouseScroll` (el cerrojo oficial de tML, patrón del UIScrollbar) — la rueda mueve SOLO el libro, el inventario NI SE ENTERA.
3. **Sombra de Tinta**: sin ojos ni colmillos (masa lisa + bruma ×1.35) + el ataque de la Muñeca Maldita (rebotes ×5 ×1,12 + vuelta a casa).
4. **Ira de un Ser Exterior**: **click izquierdo mantenido + click derecho** (nada de Shift) · buff contador desde el nivel 100 (nunca se va) · icono SFORGA (ojo-reloj entrecerrado) · el grimorio dice la verdad: «1 cada 10 min» (calculado del const).
5. **Agujero Rúnico Puro**: cero partículas orbitales — **EL ANILLO DE RUNAS VIVAS** (12 glifos contrarroto que se encienden en el frente). En la ruleta rúnica del escuadrón.
6. **Muñeco de Pruebas Negro**: eterno — `timeLeft` renovado, JAMÁS desaparece.
7. **Eco (1↔10) y Verdugo (10↔100)**: click derecho alterna la potencia, click izquierdo la aplica — como el selector de oleadas del hambre.
8. **Aura del Vacío**: la bruma residual que iba hacia el jugador ELIMINADA + **EL ESPECTRO ARDE** (su dontTakeDamage de la IA ya no lo salva: espíritus entran siempre).
9. **Icono «Información» animado**: ojo que parpadea (abierto→medio→cerrado) + iris dorado que sigue al cursor y respira.

## VERIFICACIÓN

| Gate | Resultado |
| --- | --- |
| Oráculo | 0/0 (465 .cs) |
| Build real | 0/0 · GUARDIÁN TODO VIVE |
| AUDIT de la letra | 12/12 |
| hjson ×3 | parsea |
| Headless | 0 excepciones |

**Instalación**: copia `AethonMod.tmod` a la carpeta `Mods` del tModLoader y actívalo.