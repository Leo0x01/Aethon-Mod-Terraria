import ZAI from 'z-ai-web-dev-sdk';
import fs from 'fs';

const prompt = `Dos simulaciones de RAYOS de energía (beams) para un mod de Terraria, fondo negro.
La PRIMERA imagen muestra DOS rayos: arriba un rayo DELGADO (el continuo) y abajo un rayo GRUESO (el grande). La SEGUNDA imagen muestra un barrido tipo faro con tres posiciones del giro. Responde en 7 líneas:
(1) ¿El rayo DELGADO de arriba se ve como UN rayo continuo de energía con cuerpo brillante y bordes suaves (no como una línea plana)?
(2) ¿El rayo GRUESO de abajo se ve como un PLASMA DENSO: se distinguen una envolvente amplia tenue, un cuerpo texturizado en el centro y un núcleo brillante?
(3) ¿Los EXTREMOS de los rayos se cierran suavemente (redondeados/difuminándose) o terminan CORTADOS en seco (borde recto duro)?
(4) ¿Se ven ONDULACIONES o vaivenes en los BORDES del rayo grande (el borde vivo, no recto perfecto)?
(5) ¿La textura del cuerpo del rayo grande se ve como FLUJO/plasma en movimiento (vetas o manchas) o como un relleno plano de color?
(6) En la SEGUNDA imagen: ¿los tres barridos se leen como un haz de luz tipo FARO giratorio, con brillo en el origen?
(7) VEREDICTO final: ¿estos rayos parecen un ataque de energía de calidad profesional tipo Last Prism (8-9), aceptable (6-7), o feo y plano (<6)? NOTA 1-10.`;

const zai = await ZAI.create();
for (const n of ['/tmp/sim_rayo_v6178.png', '/tmp/sim_faro_v6178.png']) {
  const b64 = fs.readFileSync(n).toString('base64');
  const r = await zai.chat.completions.createVision({
    messages: [{ role: 'user', content: [
      { type: 'text', text: prompt },
      { type: 'image_url', image_url: { url: `data:image/png;base64,${b64}` } }
    ]}],
    thinking: { type: 'disabled' }
  });
  console.log(`=== ${n.split('/').pop()} ===`);
  console.log(r.choices[0]?.message?.content);
}
