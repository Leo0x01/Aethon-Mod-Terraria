import ZAI from 'z-ai-web-dev-sdk';
import fs from 'fs';

const BASE = '/home/z/my-project/download/Aethon-Mod-Terraria/tools/tools/';
const hoja = 'v650124_fauce_mock.png';

const prompt = `Mock 2D (vista lateral) de un cráter de impacto en un juego tipo Terraria tras caer un libro mágico desde el cielo. Debe verse: (1) un POZO excavado en la roca gris, con el borde roto/irregular; (2) DOS RAMPAS de bloques oscuros violeta (obsidiana) en pendiente descendente, una desde cada borde, hasta el fondo del pozo — son escaleras caminables; (3) en el fondo del pozo, PICOS/colmillos de obsidiana puntiagudos apuntando HACIA ARRIBA (dos grandes a los lados del centro y dos más chicos, como fauces/dientes); (4) un LIBRO morado flotando en el centro arriba del pozo con el ojo cerrado y tres Z de sueño; (5) una LÍNEA VERDE que baja por la rampa izquierda hasta el fondo y otra por la derecha (el camino transitable); (6) bloques violeta esparcidos por las paredes del pozo. Responde en 5 líneas: (1) ¿se ven las dos rampas en pendiente descendente hacia el fondo? (2) ¿se ven los picos puntiagudos de obsidiana apuntando arriba en el fondo, como una boca con dientes? (3) ¿el libro flotante con Z se ve en el centro sobre el pozo? (4) ¿las líneas verdes bajan sin atravesar bloques? (5) VEREDICTO: SHIP o FIX (con qué).`;

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
