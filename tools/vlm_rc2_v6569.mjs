import ZAI from 'z-ai-web-dev-sdk';
import fs from 'fs';

const img = process.argv[2] || '/home/z/my-project/upload/pasted_image_1793403211251.png';

const prompt = `Captura de pantalla del juego Terraria con un mod activo. El jugador dispara un "rayo concentrado" (beam/haz láser horizontal morado-violeta) con un portal/círculo rúnico en la boca del arma. Analiza CON DETALLE PRECISO la calidad visual del haz y responde en español, punto por punto:
(1) ¿Cuántas CAPAS visibles tiene el haz? (¿se distingue un núcleo interior blanco/brillante separado de una envoltura exterior difusa, o es UNA sola banda plana de color?)
(2) ¿El haz tiene GLOW (resplandor/halo luminoso alrededor)? ¿Cuánto se extiende el halo respecto al ancho del núcleo?
(3) ¿El haz tiene DESENFOQUE/bordes difuminados, o los bordes son NÍTIDOS/duros tipo rectángulo plano?
(4) ¿Hay GRADIENTE de color a lo ancho (centro blanco → violeta → magenta/borde) o es un color plano uniforme?
(5) ¿Qué efectos ves: partículas alrededor, destellos de soles/estrellas de 4 puntas, anillos rúnicos, ondas, distorsión? Descríbelos.
(6) ¿Los BORDES del haz son rectos y lisos, u ondulados/orgánicos? ¿Hay "picos" triangulares o dientes de sierra en los bordes?
(7) En una escala 1-10 de "juice"/calidad visual de un mod AAA (siendo 1 = rectángulo plano de programador y 10 = el Final Flash de Dragon Ball con capas de glow cinematográfico), ¿qué nota tiene este haz y POR QUÉ?
(8) VEREDICTO HONESTO: enumera qué le FALTA para verse como un haz épico multi-capa con glow y desenfoque.`;

const zai = await ZAI.create();
const b64 = fs.readFileSync(img).toString('base64');
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
