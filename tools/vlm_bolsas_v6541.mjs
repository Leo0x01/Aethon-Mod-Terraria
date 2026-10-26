import ZAI from 'z-ai-web-dev-sdk';
import fs from 'fs';
const b64 = fs.readFileSync('/tmp/bolsas_v6541.png').toString('base64');
const zai = await ZAI.create();
const r = await zai.chat.completions.createVision({
  messages: [{ role: 'user', content: [
    { type: 'text', text: 'Iconos 30x30 (aqui 4x, fondo gris) de items de un mod de Terraria, de arriba a abajo: (1) LA BOLSA DE LAS ALAS: un saco morado oscuro con cordón lila y DOS ALAS de plumas desplegadas a los lados, con un destello de estrella de 4 puntas en la ventana; (2) LA BOLSA DE LA INVESTIGACION: un saco purpura con cordón morado y en la ventana una CONSTELACION de estrellas unidas por lineas mas un rayo zigzag; (3) EL COFRE DEL ARSENAL: un COFRE DORADO abierto con tres mini-sacos asomando (dos violetas, uno dorado al centro), esquinas moradas y cerradura. Responde en espanol, max 6 lineas: (1) ¿los tres iconos se ven completos, sin pixels corruptos y con contorno oscuro limpio? (2) ¿el primero se lee como un saco con alas? (3) ¿el segundo muestra constelacion + rayo visibles en su ventana? (4) ¿el tercero se lee como cofre abierto con bolsas dentro? (5) VEREDICTO: SHIP o FIX (que).' },
    { type: 'image_url', image_url: { url: `data:image/png;base64,${b64}` } }
  ]}],
  thinking: { type: 'disabled' }
});
console.log(r.choices[0]?.message?.content);
