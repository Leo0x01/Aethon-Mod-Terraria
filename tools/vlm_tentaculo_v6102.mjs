import ZAI from 'z-ai-web-dev-sdk';
import fs from 'fs';
const b64 = fs.readFileSync('/tmp/tentaculo_qa.png').toString('base64');
const zai = await ZAI.create();
const r = await zai.chat.completions.createVision({
  messages: [{ role: 'user', content: [
    { type: 'text', text: 'Textura de un mod de Terraria. De izquierda a derecha: (1) LA PIEL del tentáculo del abismo (128×512: una tira vertical de carne violeta-negra; la MITAD INFERIOR de la tira es la superior ESPEJADA — es un truco deliberado de enlosado: si enrollas la tira en un cilindro, el final empalma con el principio sin salto), (2-4) TRES BANDAS de 64 px (lo que el motor muestrea), (5) LA COLUMNA VIVA (bandas solapadas: el cuerpo continuo del tentáculo). Responde en espanol, max 5 lineas: (1) ¿la tira se lee como piel de tentáculo abisal violeta-negra con ventosas brillantes y venas carmesí? (2) ¿los bordes izquierdo y derecho se desvanecen suaves hacia el transparente? (3) ¿la mitad inferior es el espejo de la superior (mismo dibujo invertido)? (4) ¿la columna solapada se lee como UN cuerpo continuo de tentáculo? (5) VEREDICTO: SHIP o FIX (que).' },
    { type: 'image_url', image_url: { url: `data:image/png;base64,${b64}` } }
  ]}],
  thinking: { type: 'disabled' }
});
console.log(r.choices[0]?.message?.content);
