import ZAI from 'z-ai-web-dev-sdk';
import fs from 'fs';

const hoja = '/home/z/my-project/scripts/tmp/vastago_v6184_hoja.png';

const prompt = `Hoja de verificacion de un sprite de mascota para un mod de Terraria: 2 filas de 4 celdas. FILA DE ARRIBA (fondo de dia claro): los 4 frames IDLE de una mascota-libro violeta. FILA DE ABAJO (fondo de noche oscuro): los 4 frames de VUELO. La criatura: un librito violeta regordete con esquinas redondeadas, DOS OJOS grandes y brillantes con reflejo, rubor rosado, sonrisita, dos alitas blancas de pluma y una cinta separadora dorada colgando. Responde en 6 lineas: (1) ¿se ve ADORABLE y tierna como mascota (rate 0-10)? (2) ¿los dos ojos se ven grandes, brillantes y SIMETRICOS en cada frame? (3) ¿el cuerpo se lee como un libro cerrado (tapa violeta + canto de paginas claro)? (4) ¿las alitas se ven cosidas al cuerpo y simetricas, o flotan desconectadas? (5) ¿en la fila de noche la mascota sigue siendo legible (no desaparece)? (6) VEREDICTO: SHIP o FIX (con que arreglaria, siendo concreto).`;

const zai = await ZAI.create();
const b64 = fs.readFileSync(hoja).toString('base64');
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
console.log(r.choices[0].message.content);
