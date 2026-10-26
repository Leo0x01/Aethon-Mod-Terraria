import ZAI from 'z-ai-web-dev-sdk';
import fs from 'fs';
const b64 = fs.readFileSync('/tmp/iconos_inv.png').toString('base64');
const zai = await ZAI.create();
const r = await zai.chat.completions.createVision({
  messages: [{ role: 'user', content: [
    { type: 'text', text: 'Iconos 30x30 (aqui 4x) de items de un mod de Terraria. Filas: 1-2) cuatro ARMAS (un haz/rayo grueso diagonal, un rayo zigzag, un charco con puntos, un cetro con estrella y lineas), 3) dos COSMETICOS (un anillo eliptico, una gota), 4-6) QUINCE ALAS (abanicos de plumas de colores distintos). Responde en espanol, max 5 lineas: (1) ¿todos los iconos se ven y ninguno esta vacio o corrupto? (2) ¿las 15 alas se distinguen entre si por color? (3) ¿los 6 primeros se leen como lo descrito? (4) VEREDICTO: SHIP o FIX (que).' },
    { type: 'image_url', image_url: { url: `data:image/png;base64,${b64}` } }
  ]}],
  thinking: { type: 'disabled' }
});
console.log(r.choices[0]?.message?.content);
