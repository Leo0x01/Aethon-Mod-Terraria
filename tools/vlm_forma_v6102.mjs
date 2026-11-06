import ZAI from 'z-ai-web-dev-sdk';
import fs from 'fs';
const b64 = fs.readFileSync('/tmp/forma5_qa.png').toString('base64');
const zai = await ZAI.create();
const r = await zai.chat.completions.createVision({
  messages: [{ role: 'user', content: [
    { type: 'text', text: 'Dos iconos 30x30 (8x) de Formas Ascendidas de un mod de Terraria: a la IZQUIERDA la Forma 4 existente (estilo de la familia: sellos radiantes con nucleo luminoso y anillo perimetral), a la DERECHA la NUEVA Forma 5 — EL VIENTO DEL ESPIRITU (debe verse: nucleo de plata, el anillo ROTO en 2-3 arcos de viento teal->violeta con huecos y colas disueltas, 3-4 rayos cortos exteriores y chispas). Responde en espanol, max 4 lineas: (1) ¿el nuevo icono encaja con el estilo de la familia (sello radiante)? (2) ¿se lee su firma (el anillo roto en arcos de viento)? (3) ¿el nucleo y los rayos se ven? (4) VEREDICTO: SHIP o FIX (que).' },
    { type: 'image_url', image_url: { url: `data:image/png;base64,${b64}` } }
  ]}],
  thinking: { type: 'disabled' }
});
console.log(r.choices[0]?.message?.content);
