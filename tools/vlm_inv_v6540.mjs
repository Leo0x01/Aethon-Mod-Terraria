import ZAI from 'z-ai-web-dev-sdk';
import fs from 'fs';

const zai = await ZAI.create();
for (const archivo of ['/tmp/collage_inv.png', '/tmp/collage_ruidos.png']) {
  const b64 = fs.readFileSync(archivo).toString('base64');
  const r = await zai.chat.completions.createVision({
    messages: [{
      role: 'user',
      content: [
        { type: 'text', text: 'Collage de texturas de efectos para un mod de Terraria (labels bajo cada una). Responde en espanol, max 6 lineas: (1) ¿alguna textura se ve VACIA, negra del todo, o con artefactos duros (cuadricula, bordes rectos donde no deberia)? nombralas. (2) ¿RayoConcentrado_0..3 se ven como bandas verticales suaves de brillo (la _1 con manchas/islas, la _2 lisa)? (3) ¿Los Ruido_N se ven variados (nubes, vetas, estrellas, celulas) y no identicos entre si? (4) VEREDICTO: SHIP o FIX (que).' },
        { type: 'image_url', image_url: { url: `data:image/png;base64,${b64}` } }
      ]
    }],
    thinking: { type: 'disabled' }
  });
  console.log(`\n===== ${archivo} =====`);
  console.log(r.choices[0]?.message?.content);
}
