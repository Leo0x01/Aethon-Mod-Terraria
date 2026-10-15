import ZAI from 'z-ai-web-dev-sdk';
import fs from 'fs';

const BASE = '/home/z/my-project/download/Aethon-Mod-Terraria/tools/tools/';
const hoja = 'v650124_icono_montaje.png';

const prompt = `Hoja de contacto de 7 celdas (cada una un icono de item de Terraria 24x33 ampliado 5x, estilo pixel-art): un LIBRO magico oscuro (tapa negra-violeta con bordes morados) con un gran OJO BLANCO ovalado en el centro de la portada. Las celdas muestran el mismo libro con la PUPILA (iris violeta-oscuro con destello blanco) en distintas posiciones del ojo: izquierda, centro, derecha, arriba, abajo, y las dos ultimas celdas con el ojo CERRADO (una linea de parpado en vez del ojo blanco). Responde en 4 lineas: (1) ¿en las primeras 5 celdas se ve la pupila DENTRO del ojo blanco y en posiciones distintas? (2) ¿la pupila se ve como un iris violeta legible a este tamano o se pierde? (3) ¿en las 2 ultimas celdas el ojo se ve claramente CERRADO (parpado) y distinto de las otras? (4) VEREDICTO: SHIP o FIX (con que).`;

const zai = await ZAI.create();
const b64 = fs.readFileSync(BASE + hoja).toString('base64');
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
console.log(r.choices[0]?.message?.content);
