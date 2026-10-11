import ZAI from 'z-ai-web-dev-sdk';
import fs from 'fs';

const BASE = '/home/z/my-project/download/Aethon-Mod-Terraria/tools/tools/';
const iconos = [
  ['emperatriz_grimoriosellado_v65066_mock.png',
   'Icono 30x30 (aqui a 8x) de un ARMA de un mod de Terraria estilo pixel-art. Debe verse: UN LIBRO oscuro vertical con borde violeta, un SELLO DE LACRE DORADO circular grande en el centro de la tapa (con una estrella clara en relieve), CUATRO GEMAS de colores (dorado, cian, magenta, indigo) en las esquinas de la tapa y dos chispas arriba. Responde en 3 lineas: (1) ¿se ven esos elementos y cuales faltan o se ven mal? (2) ¿se lee como un libro sellado con gemas a tamano pequeno? (3) VEREDICTO: SHIP o FIX (con que).'],
  ['emperatriz_ecoemperatriz_v65066_mock.png',
   'Icono 30x30 (aqui a 8x) de un ARMA de un mod de Terraria estilo pixel-art. Debe verse: UN LIBRO ABIERTO abajo (dos hojas grises con lomo central oscuro) y CUATRO ORBES ARCOIRIS subiendo en abanico hacia arriba (rojo, amarillo, verde y azul, cada uno con centro blanco y estela). Responde en 3 lineas: (1) ¿se ven esos elementos y cuales faltan o se ven mal? (2) ¿se lee como un libro exhalando orbes de colores a tamano pequeno? (3) VEREDICTO: SHIP o FIX (con que).'],
  ['emperatriz_veredictoemperatriz_v65066_mock.png',
   'Icono 30x30 (aqui a 8x) de un ARMA de un mod de Terraria estilo pixel-art. Debe verse: UNA BANDA DIAGONAL GRUESA ARCOIRIS (rojo-naranja-amarillo-verde-cian-azul-violeta por segmentos, con nucleo blanco brillante) cruzando de arriba-izquierda a abajo-derecha, y UN LIBRO pequeno oscuro abajo. Responde en 3 lineas: (1) ¿se ven esos elementos y cuales faltan o se ven mal? (2) ¿se lee como un trazo arcoiris sobre un libro a tamano pequeno? (3) VEREDICTO: SHIP o FIX (con que).'],
  ['emperatriz_equinoccio_v65066_mock.png',
   'Icono 30x30 (aqui a 8x) de un ARMA de un mod de Terraria estilo pixel-art. Debe verse: UN LIBRO PARTIDO EN DOS MITADES VERTICALES — la mitad izquierda DORADA (con un sol pequeno) y la derecha NEGRA-VIOLETA (con un creciente) — y arriba dos orbes: uno DORADO con rayos a la izquierda y uno NEGRO con rim violeta y bruma a la derecha. Responde en 3 lineas: (1) ¿se ven esos elementos y cuales faltan o se ven mal? (2) ¿se lee como un libro mitad dorado mitad nocturno con dos orbes a tamano pequeno? (3) VEREDICTO: SHIP o FIX (con que).'],
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
