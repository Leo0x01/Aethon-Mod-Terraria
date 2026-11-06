import ZAI from 'z-ai-web-dev-sdk';
import fs from 'fs';
const b64 = fs.readFileSync('/tmp/estandarte_qa.png').toString('base64');
const zai = await ZAI.create();
const r = await zai.chat.completions.createVision({
  messages: [{ role: 'user', content: [
    { type: 'text', text: 'Texturas de VFX para un mod de Terraria (el efecto: el aura de un estandarte que quema a los enemigos con energia morada — la receta del calentamiento aerodinamico de los meteoritos). De izquierda a derecha: (1) EL CAMPO TERMICO ×2 (anillo radial: centro transparente, banda brillante con el borde interno mas caliente, frontera externa ondulada), (2) EL CAMPO TINTADO de morado (como se ve en juego), (3) EL FRENTE DE COMPRESION ×2 (estela horizontal: densa a la izquierda, dispersa a la derecha, con vetas de turbulencia), (4) EL FRENTE ESTIRADO a 300 px (como lo estira el motor entre el jugador y cada enemigo). Responde en espanol, max 5 lineas: (1) ¿el campo se lee como un anillo/escudo de energia con el centro limpio? (2) ¿el borde interno de la banda arde mas que el externo? (3) ¿el frente se lee como una estela de energia densa en el origen que se dispera? (4) ¿las vetas de turbulencia se ven a lo largo de la estela? (5) VEREDICTO: SHIP o FIX (que).' },
    { type: 'image_url', image_url: { url: `data:image/png;base64,${b64}` } }
  ]}],
  thinking: { type: 'disabled' }
});
console.log(r.choices[0]?.message?.content);
