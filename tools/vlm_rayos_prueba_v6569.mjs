import ZAI from 'z-ai-web-dev-sdk';
import fs from 'fs';

const BASE = '/home/z/my-project/download/Aethon-Mod-Terraria/AethonMod/Content/Weapons/Definitivas/';

const prompt = `Tres iconos de armas (44×44, pixel art) para un mod de Terraria. Cada uno debe mostrar: (1) un RAYO/HAZ diagonal violeta-magenta con VENA BLANCA central brillante que cruza de abajo-izquierda a arriba-derecha, con resplandor difuso alrededor (la bruma); (2) una CORONA RÚNICA (arco naranja-rojo con glifos dorados) en la boca del rayo (abajo-izquierda); (3) pequeños DESTELLOS de 4 puntas en las márgenes del haz; (4) una LETRA dorada grande (A, B o C respectivamente) en la esquina inferior derecha. Responde en 4 líneas: (1) ¿se lee el haz multicolor con vena blanca y bruma difusa? (2) ¿se lee la corona rúnica en la boca? (3) ¿se lee la letra dorada claramente (A/B/C)? (4) VEREDICTO por icono: SHIP o FIX (con qué), y nota 0-10 de legibilidad+atractivo.`;

const zai = await ZAI.create();
for (const n of ['RayoPruebaA.png', 'RayoPruebaB.png', 'RayoPruebaC.png']) {
  const b64 = fs.readFileSync(BASE + n).toString('base64');
  const r = await zai.chat.completions.createVision({
    messages: [{
      role: 'user',
      content: [
        { type: 'text', text: prompt },
        { type: 'image_url', image_url: { url: `data:image/png;base64,${b64}` } }
      ]
    }],
    thinking: { type: 'disabled' }
  });
  console.log(`=== ${n} ===`);
  console.log(r.choices[0]?.message?.content);
}
