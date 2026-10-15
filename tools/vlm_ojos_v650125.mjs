import ZAI from 'z-ai-web-dev-sdk';
import fs from 'fs';

const BASE = '/home/z/my-project/download/Aethon-Mod-Terraria/tools/tools/';
const hoja = 'v650125_ojos_montaje.png';

const prompt = `Hoja de verificacion de un mod de Terraria: 2 filas (ARRIBA fondo de DIA, ABAJO fondo de NOCHE oscuro) de 4 celdas cada una. Los personajes: ojos fantasticos ambar-marrones con iris dorado (el sprite de un "ojo eterno"). CELDA 1 de cada fila: un ojo GRANDE (referencia del arma). CELDA 2: un libro magico oscuro pequeno con un ojo grande en la portada + un ojo PEQUENO a su derecha. CELDA 3: el libro con TRES ojos pequenos orbitandolo. CELDA 4: tres ojos pequenos lado a lado: abierto / entreabierto / cerrado. Responde en 5 lineas: (1) ¿los ojos pequenos de las celdas 2-4 se ven como la MISMA criatura que el ojo grande de referencia (mismo arte, mas pequeno)? (2) ¿en la fila de NOCHE los ojos pequenos siguen siendo visibles (aunque mas oscuros) o desaparecen en el fondo? (3) ¿en la celda 4 se distinguen los tres estados abierto/entreabierto/cerrado? (4) ¿el iris dorado se lee dentro de los ojos pequenos o es un punto ilegible? (5) VEREDICTO: SHIP o FIX (con que).`;

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
