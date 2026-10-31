import ZAI from 'z-ai-web-dev-sdk';
import fs from 'fs';

const BASE = '/home/z/my-project/download/Aethon-Mod-Terraria/tools/tools/';
const prompt = `Simulación 1:1 de un haz láser de un mod de Terraria (fondo negro-violeta). El haz tiene UNA PILA DE CAPAS: bruma ambiental exterior (violeta profundo difuso), envolvente de glow (violeta-magenta), cuerpo de plasma con textura, vena núcleo blanca central, y destellos de 4 puntas dorados en las márgenes. Responde en 5 líneas: (1) ¿cuántas CAPAS distinguibles tiene el haz? (2) ¿hay GLOW/resplandor difuso VISIBLE alrededor del cuerpo? (3) ¿hay DESENFOQUE/bordes difuminados? (4) ¿hay PICOS triangulares o dientes de sierra en los bordes? (5) NOTA 1-10 de calidad visual tipo AAA.`;

const zai = await ZAI.create();
for (const n of ['sim_pila_A_v6569.png', 'sim_pila_B_v6569.png', 'sim_pila_C_v6569.png']) {
  const b64 = fs.readFileSync(BASE + n).toString('base64');
  const r = await zai.chat.completions.createVision({
    messages: [{ role: 'user', content: [
      { type: 'text', text: prompt },
      { type: 'image_url', image_url: { url: `data:image/png;base64,${b64}` } }
    ]}],
    thinking: { type: 'disabled' }
  });
  console.log(`=== ${n} ===`);
  console.log(r.choices[0]?.message?.content);
}
