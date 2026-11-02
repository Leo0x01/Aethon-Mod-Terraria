import ZAI from 'z-ai-web-dev-sdk';
import fs from 'fs';
const b64 = fs.readFileSync('/tmp/v686_hoja.png').toString('base64');
const zai = await ZAI.create();
const r = await zai.chat.completions.createVision({
  messages: [{ role: 'user', content: [
    { type: 'text', text: 'Assets pixel-art (4x, fondo gris) para un mod de Terraria, de izq. a derecha: (1) LIBRO CORRUPTO 22x30: un libro cerrado de tapa NEGRA con lomo violeta y runa morada en la tapa, bloque de hojas claro al lado derecho, punto dorado; (2) ICONO ALTAR 30x44: mesita de piedra de dos cuerpos (peana ancha abajo, mesa angosta arriba) con un OJO teal flotante arriba, cuenco y chispa teal en el nicho, tornillos dorados; (3) TILE ALTAR 36x54: la misma mesa en versión tile más grande; (4) CODICE 28x28: libro violeta con un ARBOL dorado en la tapa (tronco, 2 ramas, 3 nodos-bolita dorados); (5) CADENA 16x16: dos eslabones ovales grises engarzados en horizontal; (6) NODO 48x48: disco blanco con anillo gris claro, rim oscuro y 4 muescas. Responde en espanol, max 6 lineas: (1) ¿se ven completos, sin pixels corruptos y con contornos limpios? (2) ¿el libro se lee NEGRO con runa visible? (3) ¿el icono y el tile del altar se leen como mesa de piedra con ojo teal? (4) ¿el codice muestra el arbol dorado con nodos? (5) ¿la cadena son dos eslabones engarzados y el nodo un disco claro con anillo? (6) VEREDICTO: SHIP o FIX (que).' },
    { type: 'image_url', image_url: { url: `data:image/png;base64,${b64}` } }
  ]}],
  thinking: { type: 'disabled' }
});
console.log(r.choices[0]?.message?.content);
