import ZAI from 'z-ai-web-dev-sdk';
import fs from 'fs';

const BASE = '/home/z/my-project/download/Aethon-Mod-Terraria/tools/tools/';
const iconos = [
  ['bolsa_emperatriz_v650100_mock.png',
   'Icono 30x30 (aqui a 8x) de un ITEM de un mod de Terraria estilo pixel-art: una BOLSA. Debe verse: una bolsa con cuerpo PERLA-ROSA claro, el lomo y el nudo del cordon DORADOS arriba, y en la ventana central una BANDA DIAGONAL ARCOIRIS (violeta arriba-izquierda a rojo abajo-derecha, con vena blanca central) y dos chispas blancas arriba. Responde en 3 lineas: (1) ¿se ven esos elementos y cuales faltan o se ven mal? (2) ¿se lee como una bolsa clara con un trazo arcoiris dentro a tamano pequeno? (3) VEREDICTO: SHIP o FIX (con que).'],
  ['estela_aethon_v650100_mock.png',
   'Sprite 30x30 (aqui a 8x) de un PROYECTIL de un mod de Terraria estilo pixel-art: un COMETA vertical. Debe verse: una CABEZA dorada brillante arriba con nucleo BLANCO caliente, dos alitas doradas/cian a los lados, y una COLA larga abajo que cambia de color de arriba a abajo: indigo, magenta, cian y oro. Responde en 3 lineas: (1) ¿se ven esos elementos y cuales faltan o se ven mal? (2) ¿se lee como un cometa/estela de colores a tamano pequeno? (3) VEREDICTO: SHIP o FIX (con que).'],
  ['trazo_emperatriz_v650100_mock.png',
   'Sprite 30x30 (aqui a 8x) de un PROYECTIL de un mod de Terraria estilo pixel-art: un TRAZO ARCOIRIS vertical. Debe verse: una BANDA VERTICAL GRUESA con los colores del arcoiris en franjas horizontales (violeta y azul arriba, cian y verde al medio, amarillo, naranja y rojo abajo), un NUCLEO/vena BLANCA central que la recorre, la PUNTA afilada blanca arriba y la cola deshaciendose en pixeles abajo. Responde en 3 lineas: (1) ¿se ven esos elementos y cuales faltan o se ven mal? (2) ¿se lee como un rayo/trazo arcoiris vertical a tamano pequeno? (3) VEREDICTO: SHIP o FIX (con que).'],
];

const zai = await ZAI.create();
for (const [archivo, prompt] of iconos) {
  const b64 = fs.readFileSync(BASE + archivo).toString('base64');
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
  console.log(`\n===== ${archivo} =====`);
  console.log(r.choices[0]?.message?.content);
}
