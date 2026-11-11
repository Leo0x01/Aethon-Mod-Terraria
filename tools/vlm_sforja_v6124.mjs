import ZAI from 'z-ai-web-dev-sdk';
import fs from 'fs';
const b64 = fs.readFileSync('/tmp/sforja_qa/montaje.png').toString('base64');
const zai = await ZAI.create();
const r = await zai.chat.completions.createVision({
  messages: [{ role: 'user', content: [
    { type: 'text', text: 'Sprites de un mod de Terraria (fondo gris oscuro). De izquierda a derecha: (1) EL SOL DE LUZ: un sol blanco-dorado con corona de rayos dorados y nucleo blanco; (2) EL SOL DE OSCURIDAD: un sol negro con borde/corona violeta ardiendo y nucleo casi negro; (3) EL SOL DE FUSION: un sol grande blanco con nucleo negro y doble corona (dorado+violeta) mas devastador; (4) EL TAJO DE OSCURIDAD: una hoja curva larga horizontal con centro negro, medios morados y bordes blancos, puntas de aguja en los extremos; (5) LA AGUJA DEL VACIO: una aguja delgada horizontal con punta blanca al frente (derecha), cuerpo violeta y un punto dorado (ojo de coser) atras. Responde en espanol, max 8 lineas: (1) ¿cada sprite se ve COMPLETO, legible y sin pixels corruptos? (2) ¿el sol 1 se lee blanco/dorado y el sol 2 se lee negro con borde violeta? (3) ¿el sol 3 se ve mas grande y devastador que los otros dos? (4) ¿el tajo 4 muestra centro negro + medios morados + bordes blancos con forma de hoja/filo curvo? (5) ¿la aguja 5 es delgada con punta blanca y punto dorado? (6) VEREDICTO: SHIP o FIX (que).' },
    { type: 'image_url', image_url: { url: `data:image/png;base64,${b64}` } }
  ]}],
  thinking: { type: 'disabled' }
});
console.log(r.choices[0]?.message?.content);
