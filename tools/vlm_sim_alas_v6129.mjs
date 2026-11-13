import ZAI from 'z-ai-web-dev-sdk';
import fs from 'fs';

const BASE = '/home/z/my-project/mod/tools/';

const prompt = `Dos simulaciones 1:1 del ala de un mod de Terraria (estilo "Alas Rúnicas del Vacío": alas de cristal violeta brillante sobre fondo negro, vistas desde atrás, el cuerpo del jugador está en el centro). La PRIMERA imagen es el ala CON PLUMAS (cinco tiras radiales por lado); la SEGUNDA es el ala LISA (la versión nueva: las tiras murieron, solo queda la vela continua con el borde curvo luminoso). Responde en 6 líneas:
(1) En la PRIMERA imagen: ¿cuántos RAYOS o TIRAS radiales distinguibles hay dentro de cada ala? ¿el borde del ala tiene PICOS o DIENTES DE SIERRA (pico-valle-pico)?
(2) En la SEGUNDA imagen: ¿cuántos rayos o tiras radiales distinguibles hay ahora dentro de cada ala? ¿el interior se lee como UNA SUPERFICIE CONTINUA?
(3) En la SEGUNDA imagen: ¿el BORDE del ala es UNA CURVA CONTINUA sin picos ni dientes de sierra?
(4) ¿La forma general del ala (abanico abierto hacia arriba y afuera) se CONSERVA en la segunda imagen respecto a la primera?
(5) ¿Se ven las RUNAS o destellos brillantes cerca del borde en ambas?
(6) VEREDICTO: ¿la SEGUNDA imagen se ve como un ala LISA de cristal violeta SIN dientes de sierra? NOTA 1-10.`;

const zai = await ZAI.create();
for (const n of ['sim_alas_v6129_con_plumas.png', 'sim_alas_v6129_lisa.png']) {
  const b64 = fs.readFileSync(BASE + n).toString('base64');
  const r = await zai.chat.completions.createVision({
    messages: [{ role: 'user', content: [
      { type: 'text', text: prompt },
      { type: 'image_url', image_url: { url: `data:image/png;base64,${b64}` } }
    ]}],
    thinking: { type: 'disabled' }
  });
  console.log(`=== ${n} ===`);
  console.log(r.choices[0]?.message?.content);
}
