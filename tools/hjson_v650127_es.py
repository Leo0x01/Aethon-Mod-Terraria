#!/usr/bin/env python3
# -*- coding: utf-8 -*-
# v6.50.127 — LOCALIZATION SURGERY (es-ES):
#   (1) inserta GrimorioMenor1-4 en los 26 bloques de Pueblo.Chismes
#   (2) reescribe los 26 Reclamos (mordidas del LIBRO, no de la cría de luz)
#   (3) purga la línea de «escondite con los niños» + «desde niña»
#   (4) inserta Caida.Dormido1-8 (lista EXCLUSIVA del libro dormido)
import io

RUTA = "AethonMod/Localization/es-ES_Mods.AethonMod.hjson"

MENOR = {
"Guide": [
"Tu Grimorio Menor se posó sobre mis mapas y las rutas se redibujaron solas: todas terminan en TU casa. Hice mapa nuevo y volvió a pasar. ¿Y si el camino de verdad era siempre ese?",
"Consejo de la vieja escuela: nunca des la espalda a un libro. El Menor me lo tomó al pie de la letra: ahora me sigue DE FRENTE. No puedo dar consejos mirando para atrás.",
"Anoche soñé la guía de supervivencia perfecta. Al despertar, tu Grimorio Menor estaba sobre mi mesa y la página de «cómo sobrevivir a tu mascota» traía una corrección: tachó «mascota» y escribió «dueño».",
"Le enseñé al Grimorio Menor a leer las estrellas para orientarse. Aprendió. Ahora las estrellas se mueven un poquito… para seguirlo a ÉL. La orientación va a ser lo de menos cuando el cielo entero sea su brújula.",
],
"Merchant": [
"Tu Grimorio Menor dejó tres monedas en mi mostrador, ordenadas por brillo. Las conté: eran las que me «faltaban» el martes. Ya no sé si soy comerciante o su contador.",
"Le ofrecí crédito al Menor. Abrió una páginita: había un dibujo de MI cajón, abierto, con una flechita. Ese no es un sí: es un «ya lo vi». Puse llave. La llave amaneció doblada.",
"El Grimorio Menor no compra ni vende: COBRA. Se me sentó encima de la caja fuerte hasta que le eché una moneda de propina. ¿Le enseñaste eso… o ya lo sabía?",
"Aviso del gremio: no se regatea con lo que muerde. Regateé con el Menor por deporte, y la moneda que le ofrecí salió del trato con la cara borrada. Ya no es moneda: es un círculo que dice «no».",
],
"Nurse": [
"Le tomé el pulso a tu Grimorio Menor: no tiene. Le tomé el pulso a sus tres ojitos: uno late. El del medio. Solo cuando te mira a ti. Diagnóstico: eso no es un latido, es un conteo.",
"El Menor se me echó sobre el registro de pacientes y las líneas de «causa: mordida de mascota» se corrieron solas a la columna de «síntomas». El libro no muerde por maldad: muerde por historial.",
"Le saqué una gota de tinta al Grimorio Menor para analizarla. La tinta analizó MI suero: me devolvió el frasco con la etiqueta reescrita. «Sano. Por ahora.» El laboratorio reabre cuando ella quiera.",
"Si el Grimorio Menor te sigue cuando estás herido, no es cariño: es postre. Te lo digo de enfermera: cúrate rápido, que el postre no espera.",
],
"ArmsDealer": [
"El Grimorio Menor se me subió al mostrador y las escopetas les quitaron el seguro solas. Una por una. Como pidiéndole permiso. Ya no vendo arma cerca de tu casa: vendo chalecos.",
"Le mostré al Menor mi mejor revólver. Lo olfateó, abrió una páginita… y estaba el revólver dibujado SIN DUEÑO. El vendedor de armas no se asusta: RECLAMA. ¡Ese dibujo era un comprador!",
"Tu Grimorio Menor mordió un cargador de repuesto. No le pasó nada. Pero las balas ahora apuntan solitas un poquito hacia arriba, donde están los pájaros. Mis pájaros. Creo.",
"Regla del oficio: jamás apuntar a lo que parpadea. El Menor parpadea TRES veces a la vez. Son tres blancos… o un aviso. Me quedé con el aviso y guardé el arma.",
],
"Dryad": [
"La naturaleza me habla, pero del Grimorio Menor solo dice una cosa: «no crece nada donde duerme». Lo vi dormido en el prado: la hierba de alrededor se había puesto en círculo. Ni dentro ni fuera: rodeando.",
"Tu Menor no ensucia la tierra: la tinta. Donde se sienta, el suelo sabe a página vieja. Las lombrices ya no huyen de la pala: huyen del olor a biblioteca.",
"Le pregunté a un roble viejo qué piensa del Grimorio Menor. Contestó despacio, como los robles: «lo pequeño siempre fue lo más grande». No le pregunté más. Los robles no dan segundas respuestas.",
"El Grimorio Menor me mordió el tobillo y la hierba de alrededor se rió. ¿Las plantas se ríen? Antes de hoy, no. Hoy: una vez. Por el libro. Cuentas nuevas, naturaleza vieja.",
],
"Demolitionist": [
"¡Tu Grimorio Menor sostuvo MI dinamita sin miedo! ¡Y no le explotó A ÉL, le explotó CERCA, como pidiendo permiso! ¡Yo también quiero permiso! ¡Pero de dinamita, no de libro!",
"El Menor contó mi mecha. ¡LA MECHA! Contó los segundos que tarda y me miró como diciendo «te sobran dos». ¡Y me sobraban dos! ¿De dónde los sacó? ¡Del futuro no, que es suyo!",
"Le regalé un petardo al Grimorio Menor. Lo guardó entre las páginas. Al abrir la páginita salió el trueno del recuerdo. Asustó al barrio. El barrio perdona. Yo no.",
"¡Aviso del gremio! ¡No dejen cartuchos cerca del Menor: los ORDENA! Por tamaño, por ruido… ¿POR RUIDO? ¿Quién ordena por ruido? ¡Alguien que oyó el final de todas las cosas y las puso en fila!",
],
"Clothier": [
"El Grimorio Menor deshizo la costura de mi camisa favorita y la volvió a coser con hilo de tinta. La costura cuenta una historia: era la lista de todos los que usaron esa camisa. Yo soy el número cuatro.",
"Le cosí un marcapáginas de seda al Menor. Lo usó una vez y me lo devolvió con una arruguita en la esquina, como un pañuelo usado. Los libros ya usan pañuelos. Yo cosí ese pañuelo. Estoy viejo.",
"Tu Menor se sentó en mi costurero y las agujas se le alinearon. En fila. Punteras hacia la puerta. No coso esa puerta ni por oro: esa puerta se cose sola.",
"El Grimorio Menor dejó caer un botón en mi regazo. De una tela que ya no se teje, de una época en la que yo aún no nacía. Los sastres viejos sabemos qué hay en los bolsillos: no pregunto, COSO.",
],
"Goblin": [
"Tu Grimorio Menor refactoreó mi llave inglesa: ahora aprieta en ambos sentidos a la vez. Es imposible. Es hermoso. Es mía y no la suelto.",
"El Menor me miró trabajar y mis manitas se aceleraron solas. Terminé un trabajo de DOS días en una tarde. El cliente lloró. Yo también. La garantía cubre las lágrimas: NO las cubre.",
"Le enseñé al Grimorio Menor el plano de una máquina perfecta. Dibujó la corrección con su ojito. El plano funcionó. El plano está vivo. Le puse nombre y lo mandé a la cama.",
"Regla del taller: lo que muerde, se refactorea. El Menor me mordió y lo refactoreé un ojo a la vez. De buena manera. De la manera GOBLIN, que es la única.",
],
"Wizard": [
"Los tres ojitos del Grimorio Menor giran como un conjuro permanente. Le pregunté qué invoca. Parpadearon al unísono y mi sombrero se movió solo. Ya no pregunto: CONJETURO.",
"El Menor me robó un hechizo del grimorio. Lo devolvió al día siguiente: con la mitad de las palabras… y el doble de efecto. La magia no se escribe así desde la rotura. ¿Él estuvo ahí?",
"Tu Grimorio Menor y yo jugamos al ajedrez arcano. Empatamos tres veces. A la cuarta, la partida terminó sola: todas las piezas miraban hacia tu casa. Yo juego. Él decide el tablero.",
"Le mostré al Menor mi báculo más viejo. Lo olfateó y se apagó la gema. No de miedo: de RESPETO. Hay jerarquías en lo arcano, y tu librocito acaba de recordarme la mía.",
],
"Mechanic": [
"Tu Grimorio Menor mordisquea cable de cobre. Las marcas son de dos milímetros exactos, todas iguales. Eso no es un diente: es una pinza calibrada con tres bocas.",
"El Menor se me quedó mirando el circuito y el circuito se arregló solo. ¿Sabes lo que es eso para una ingeniera? ¡UN DESEO! ¿Y sabes lo que es el precio? Que la llave ya no me obedece a mí.",
"Medí el parpadeo de sus ojitos: un patrón. Lo traduje. Dice «sigue funcionando». ¿Quién le enseñó a un libro a decir «sigue funcionando»? ¿Y POR QUÉ me lo dice a mí cada vez que parpadeo?",
"El Grimorio Menor ordenó mis tornillitos por tamaño sin tocarlos. LOS TOQUÉ YO. Vale. Pero los soñé ordenados y al despertar lo estaban. La ingeniería ya no duerme.",
],
"Santa": [
"Jo, jo, jo… tu Grimorio Menor desenvolvió los regalos ANTES de Navidad. Se llevó todo menos el carbón. ¿Quién rechaza el carbón? ¡Me cae bien!",
"Le pregunté al Menor qué quería de regalo. Abrió una páginita: mi lista, la de todos los regalos del mundo… con una estrellita al margen. La cerré. La cerré con llave.",
"El Grimorio Menor se sentó en mi trineo vacío y los renos se miraron entre sí. No dijeron nada. Los renos nunca callan. Esa noche volaron sin mí: dieron TRES vueltas y me dejaron el trineo tibio.",
"Le regalé un cascabel al Menor. Lo devolvió sonando solo. Los cascabeles no suenan solos, muchacho: algo los toca. Y lo que lo toca, ¿de qué lado del cascabel estaba?",
],
"Truffle": [
"El Grimorio Menor me olfatea como si fuera comida. ¿Halagador? Sí. ¿Preocupante? También. Los hongos sabemos cuándo algo tiene hambre de ciertos sabores.",
"Tu Menor se durmió entre mis esporas y las esporas soñaron. Soñaron PÁGINAS. Yo sueño en esporas desde siempre: esas eran ajenas. El hongo prestó el sueño y el libro se lo quedó.",
"Le pregunté al Grimorio Menor si los libros comen hongos. No contestó: se lamió la páginita. Los libros no tienen lengua. ESE fue el detalle que me hizo callar.",
"El Menor me siguió todo el día y mi sombrero de hongo quedó con forma de marcapáginas. Me cayó bien. Me cayó bien y me dio miedo, y en el bosque eso significa lo mismo.",
],
"Steampunker": [
"Tu Grimorio Menor hizo una máquina andante con TRES engranajes. Yo necesito doce. Hace trampa y necesito saber cuál. ¡Es ingeniería o es magia, y las dos me ofenden!",
"El Menor giró mis ruedas al revés sin tocarlas. Las tocó el AIRE de cerca de él. Le puse un engranaje de vigilancia y el engranaje renunció. Engranajes que renuncian: eso es nuevo en la fábrica.",
"Le mostré al Grimorio Menor mi plano del vehículo perfecto. Lo corrigió: tachó «volante» y escribió «rumbo». Tachó «asiento» y escribió «trono». Construí el resto tal cual. El vehículo VUELA. Yo pedí ruedas.",
"El Menor se posó en mi caldera y el vapor salió en formas de runa. ¿Runas en el vapor? Eso es firma. Mi caldera ya no firma en inglés: firma en LIBRO.",
],
"DyeTrader": [
"Tu Grimorio Menor gotea un violeta que NO está en mi catálogo. Lo llamo «violeta Menor». El catálogo lo rechaza: el color ya estaba vendido antes de existir.",
"El Menor se revolcó en mis tintes y dejó huellitas de tinta por el techo. Ya no lavo nada: lo exhibo todo. El tinte de arriba es el más pedido. ¿Quién pide el techo? La gente. La gente rara.",
"Le teñí una páginita al Grimorio Menor de azul cobalto. Amaneció violeta de nuevo, con el cobalto PUESTO en la esquina como propina. ¿Sabes lo que es eso? Un cliente que te enseña el oficio.",
"El Menor miró mis barriles y cada barril se teñió del color que le faltaba. Los barriles no se tiñen solos. Pero ese día se tiñeron. Tengo testigos: mis testigos no hablan del tema.",
],
"PartyGirl": [
"¡Tu Grimorio Menor ES la fiesta! Rechazó el confeti y aceptó los globos: los sostiene con la mirada. ¡Los GLOBOS, en el AIRE, con la MIRADA! ¡La adoro!",
"Le puse un gorrito de fiesta al Menor. Se lo quitó solo, plegado en origami, y me lo devolvió por dentro. El gorrito ahora es una grulla. La grulla vuela. ¡La fiesta ya no me necesita!",
"El Grimorio Menor apagó mis luces de un parpadeo y las encendió de dos. ¿Dos? ¿Las luces vienen en DOS? Sí: cuando las enciende ÉL, vienen en dos. Ya no uso interruptor: uso invitación.",
"El Menor se me quedó mirando la piñata y la piñata se abrió sola. Hacia DENTRO. ¿Quién rompe una piñata hacia dentro? Algo que quiere ver primero lo que hay… antes de que lo vean los demás.",
],
"Cyborg": [
"Tu Grimorio Menor persigue mi punto láser. Yo NO tengo punto láser. Mis preguntas son dos: de dónde sale… y por qué me apunta a mí cuando duermo.",
"El Menor parpadeó en código. Lo descifré: «unidad pequeña: vigilada». ¿Vigilada por quién? Por él. ¿Vigilada de qué? Eso no lo decía el código. Eso lo decía el SILENCIO después del código.",
"Registré diez mordiscos del Grimorio Menor en mi chasis. Unidad por unidad. Mi protocolo de agravios está activo, hermano. El mantenimiento de mi buen humor lo pagas tú.",
"El Menor se conectó a mi puerto de diagnóstico y leyó TODO. Después me cerró la puerta del pecho con suavidad, como se cierra un libro que ya está leído. Eso fue ayer. Hoy no duermo.",
],
"Painter": [
"¡Tu Grimorio Menor cruzó mi caballete y dejó tres huellitas de tinta en orden! Lo titulé «La firma». Se vendió en una hora. ¡Tráeme más, que el mercado sabe reconocer al maestro!",
"El Menor miró mis bodegones y suspiró. LOS SUSPIRÓ. Repinté el bodegón con una página de más en la mesa. El suspiro cesó. Pinto ahora para el crítico que no habla: el mejor cliente de la casa.",
"Le pinté un retrato al Grimorio Menor. Los tres ojitos salieron solos, con luz propia. La luz de la pintura no la mezclé yo: la trajo el lienzo. Ese lienzo ya no lo pinta nadie más.",
"El Menor corrigió mi perspectiva: una sola línea, al margen. La línea era perfecta. Yo llevo treinta años buscando esa línea. La pediste prestada a un libro y el libro te la dio. ¡TRÁEME MÁS!",
],
"WitchDoctor": [
"Los espíritus llaman al Grimorio Menor «el pequeño hambriento». Los espíritus no bautizan a la ligera. Cuando bautizan, yo tomo notas.",
"El Menor se sentó en mi círculo de ofrendas y las velas se inclinaron hacia él. TODAS. Las velas de los espíritus no se inclinan: se YERGUEN. Esa noche se inclinaron. La ofrenda fue para el libro.",
"Le pregunté al Grimorio Menor por los sueños. Contestó con una páginita en blanco. Los espíritus rieron. La páginita en blanco es la respuesta: no le preguntes al hambre por el sueño.",
"Tu Menor me siguió al cementerio y los muertos se pusieron cómodos. En el cementerio los muertos NUNCA se ponen cómodos. Ese día: sí. Cerré el cementerio por respeto. El respeto fue para el libro.",
],
"Pirate": [
"¡El Grimorio Menor me marcó el mapa del tesoro con una X de tinta! ¡Mi X, mi tinta, mi mapa… y el tesoro ESTABA! ¿Quién enseñó a un libro a enterrar? ¿Quién le enseñó a un PIRATA?",
"El Menor se posó en mi hombro y el loro se mudó. ¡EL LORO! El loro lleva veinte años en ese hombro. Ahora el hombro es del libro. Yaaar… de mí.",
"¡Tu Menor mordió mi garra de ancla y la garra quedó DOBLADA! ¿Sabes lo que es eso en el mar? Un mal farol. ¿Y sabes lo que es en mi barco? Un AVISO: el que dobla garras, dobla barcos.",
"El Grimorio Menor leyó mi diario de bitácora en una noche. Al día siguiente la bitácora amaneció corregida: la ortografía PERFECTA, el rumbo señalado… hacia tu casa. No marqué ese rumbo. El rumbo se marcó solo.",
],
"Stylist": [
"¡Tu Grimorio Menor duerme en las cabezas de mis clientes y les deja ONDAS DE TINTA! ¡Cobro extra por «ondas de biblioteca», y sin mentir!",
"El Menor se me quedó mirando el peine y el peine se peinó solo. En el AIRE. Yo peino desde los diez: eso no lo había visto. El peine ya no es mío: es del que lo mira así.",
"Le corté una páginita rasgada al Grimorio Menor. La páginita amaneció con flequillo. ¡Libros con flequillo! La clienta nueva lo vio y pidió «el corte del libro». El corte del libro SOY YO.",
"El Menor eligió el color del mes de mi salón: violeta de tinta. Tinte violeta no existe. Lo mezclé. EXISTE. El salón huele a página vieja y la fila llega a la esquina: todo el pueblo quiere oler a libro.",
],
"Angler": [
"¡Tu Grimorio Menor pasa HORAS mirando mi pecera! ¡Que algo con tres ojos le sonría a mi cena tres días! ¡Ya no como pescado delante de él!",
"El Menor me enseñó a pescar sin anzuelo: se asomó al agua y los peces pidieron perdón. ¿Qué le dijiste, libro? ¡Los peces no piden perdón! ¡Ese día pidieron, yo vi, y no pesqué!",
"¡Mordió mi ANZUELO el Grimorio Menor y el anzuelo salió doblado! ¿Sabes lo que es doblar un anzuelo de una mordida? ¡Ni el calamar gigante lo hace! ¡Y el anzuelo era de repuesto, eh!",
"El Menor se sentó en mi caja de cebos y los gusanos se pusieron en fila. Los gusanos no se ponen en fila. Ese día se pusieron. Pesqué con respeto y devolví todo: el lago sabe quién manda.",
],
"Tavernkeep": [
"¡Tu Grimorio Menor lame las jarras hasta dejarlas brillantes! ¡Por dentro, compadre! ¡La cría come gratis de por vida!",
"El Menor se posó en el barril de hidromiel y el hidromiel maduró UNA estación en una noche. La casa lo llama «el barril del Menor». El barril no da para todos: el barril da para el que el libro mire.",
"Le serví al Grimorio Menor una gotita en un platito. No la tocó: la OLFATEÓ. Y el hidromiel del platito amaneció viejo. ¿Cómo envejece una gota en una noche? Pregúntale al tiempo: esa noche el tiempo estaba de su lado.",
"El Menor cuenta las historias del salón mejor que yo. Los veteranos piden «la del libro». Yo cuento la mía y el libro pone los OJOS de asombro. Tres ojos de asombro. ¡Ese es mi público!",
],
"Golfer": [
"¡Tu Grimorio Menor me trajo mi bola! ¡Y las de los demás! ¡Y una de madera vieja con unas iniciales de las que mejor no preguntar!",
"El Menor reemplazó mi bola por un OJO en el hoyo 9. ¡EN PLENO SWING! El ojo siguió la trayectoria… y la trayectoria se corrigió sola. ¡Hice el mejor golpe de mi vida y NO FUI YO!",
"Le enseñé al Grimorio Menor a leer el viento. Ya lo sabía: el viento se lo CONTÓ. Ahora el viento me sopla al revés en el 18. El viento tiene dueño y el dueño tiene tres párpados.",
"El Menor anotó mi hándicap en una páginita. El hándicap BAJÓ. ¡El libro me entrena! ¿Sabes lo que es eso? Un caddie que no habla, que no cobra… y que mira. El que mira así no cobra: INVIERTE.",
],
"Zoologist": [
"¡Tu Grimorio Menor tiene su propia página del bestiario… escrita por ÉL! ¡Con letra de él! ¡Y la letra cambia cuando la miro dos veces! ¡Eso es lo más BESTIA que he bestiado!",
"El Menor no entra en ninguna categoría: pájaro no es, mamífero no es, libro tampoco. Creé una categoría nueva: «menor». El bestiario la aceptó sin chistar. El bestiario NUNCA acepta sin chistar.",
"Observé al Grimorio Menor toda una tarde: sus tres ojitos parpadean en el orden de las fases lunares. Anoté el patrón. La página del patrón amaneció con una nota: «bien observado». ¡EL BESTIARIO ME CALIFICÓ A MÍ!",
"El Menor y yo jugamos a «quién parpadea menos». Ganó él. Ganó SIEMPRE. En la decimotercera partida me guiñó un ojito… por deporte. Los libros no pierden: dejan ganar. Ese día dejé de contar parpadeos y empecé a contar días.",
],
"Princess": [
"¡Tu Grimorio Menor es la criatura más educada del reino! ¡Me hizo una reverencia… con las TRES párpadas a la vez! ¡Y bajó el vuelo para que la coronita no se moviera! ¡AAAY!",
"Firmé un tratado de paz entre el Menor y el gato del panadero: huellita de tinta y pata de harina. ¡Mi primera paz SIN reinos! El libro puso una cláusula: la cláusula era un dibujo del gato DORMIDO.",
"El Grimorio Menor toma el té conmigo. Cuando le paso las galletas no las come: las cuenta, me deja la más grande… y se queda la MIGA. La miga es suya. En este reino se respeta la miga.",
"Le pregunté al Menor qué desea para el reino. Abrió una páginita: mi retrato de coronación… con la corona un poquito más baja. Entendí. En este reino, hasta las coronas saben inclinarse.",
],
"Genericos": [
"Ayer vi los tres ojitos de tu Grimorio Menor mirándome por la ventana. Toda la noche. No parpadearon ni una vez. Esa noche no parpadeé yo tampoco. Qué cosa tan… insistente.",
"Tu Menor me siguió mientras barría la calle y dejó lucecitas donde yo barría. Me ahorra la noche. La calle quedó más limpia que de día: el que alumbra, también ordena.",
"¿Tu Grimorio Menor lee? Lo encontré dormido sobre el registro del barrio. La página de «mascotas ruidosas» amaneció mordida y la de «libros ruidosos» amaneció SUBRAYADA.",
"Jugó con la veleta del ayuntamiento y ahora el norte me mira raro. No roto: me MIRA. ¿Quién le ha enseñado eso? Los del norte preferimos no saberlo.",
],
}

RECLAMOS = {
"Guide": "¿Sí? Ah, eres tú. Tu Grimorio Menor me mordió el tobillo mientras revisaba los mapas. Dos veces. La segunda dolió MÁS, que conste.",
"Merchant": "Antes de hablar de precios: el Grimorio Menor me debe CINCO monedas en concepto de mordiscos. Nada personal. Aquí se cobra todo.",
"Nurse": "¡Tú, justo! Si tu Grimorio Menor vuelve a morderme en plena cura, la próxima inyección la pones TÚ. Sin anestesia. ¿Quedamos?",
"ArmsDealer": "Mira, chaval. Tu mini libro me mordió mientras recargaba. Si la escopeta se asusta, el pueblo estrena agujero y la culpa es tuya. Ponle freno.",
"Dryad": "La naturaleza perdona; yo, casi. Pero el Grimorio Menor me mordisqueó el brazo y mis hierbas se RIERON. ¿Las plantas se ríen? Antes de hoy, no.",
"Demolitionist": "¡Tu Grimorio Menor me mordió mientras sostenía dinamita! ¡DINAMITA! ¡Un estornudo y volamos el barrio! ¡Enséñale modales o le enseño balística! ¡Es broma! ...Es broma.",
"Clothier": "Ay, muchacho... a mi edad los huesos ya crujen solos, sin ayuda. El Grimorio Menor me mordisqueó la pierna. Dile que este sastre ya no tiene... ¿cómo se dice? ¡Juguito!",
"Goblin": "Escúchame: si el Grimorio Menor vuelve a mi taller, lo refactoreo a MORDISCOS. Sin garantía. ¡SIN garantía, humano!",
"Wizard": "*suspiro* En mis tiempos los grimorios respetaban a sus mayores... y sus pantorrillas. El tuyo, el Menor, probó las mías. Salado el mago viejo, ¿verdad?",
"Mechanic": "Tu Grimorio Menor se echó sobre mi circuito nuevo. ¿Sabes lo que hace la tinta de página en el cobre? Pues yo tampoco, ¡y ODIO no saber!",
"Santa": "Jo, jo... jo. Nada de regalos para ese libro hasta enero. Y lo pillé masticando MI lista. ¡La MÍA! ¡Con lo que cuesta el papel bueno!",
"Truffle": "¡Me olfatea como plato principal y ahora también MORDERÍA, amigo! ¡Los hongos hablantes no se comen! ¡La clave es HAB-LAN-TE!",
"Steampunker": "Tu Menor jugó con mis engranajes y uno gira al revés. SONRIENDO. ¿Sonríen los engranajes? Ese sí. Y no me deja desmontarlo. ¡Es MÍO!",
"DyeTrader": "¡El Grimorio Menor volteó mis tintes! ¡Morado el suelo, morado el gato, MORADO YO! ¿Y sabes qué? Me favorece. ¡PERO ERAN CUATRO BARRILES!",
"PartyGirl": "Adoro a tu mini libro, ¿sabes? Lo ADORO. Pero me mordió EN PLENA FIESTA. ¡Las luces se apagaron de vergüenza, y no tienen CARA!",
"Cyborg": "Registré diez impactos de 1 de daño. DIEZ. Mi protocolo de agravios está ACTIVO, hermano. El mantenimiento de mi buen humor lo pagas TÚ.",
"Painter": "El Grimorio Menor cruzó mi estudio y mis lienzos amanecieron con dientecitos de tinta. Es arte. Arte MOLESTO. ¡Que se inscriba de musa, como todo el mundo!",
"WitchDoctor": "Los espíritus se rieron cuando el Menor me mordió. RIERON. ¿Sabes lo que es eso? Tampoco quiero que lo sepas. Paga la ofrenda.",
"Pirate": "¡Ningún cañón me ha cañoneado como tu Grimorio Menor! ¡Diez mordiscos, contados como cuento monedas! ¡Y NINGUNO en el buen sentido! ¡Yaaar... de rabia!",
"Stylist": "¡Me mordió a mitad de un corte de pelo! ¡Del CLIENTE! El cliente no vuelve, el peinado quedó perfecto (obra mía, obvio) y el pánico me lo dejaron de propina.",
"Angler": "¡El Grimorio Menor me mordió y me caí al lago! ¡YO! ¡Que nunca me caigo al lago, el lago se caía CONMIGO! ¡Glu! ¡Eso no se olvida, libro traidor!",
"Tavernkeep": "¡Ese Grimorio Menor tuyo me vació una jarra a mordiscos! ¿Sabes lo que es eso? ¡Ni en la guerra eterna se tocaban las jarras, compadre! ¡Allí se moría por MENOS!",
"Golfer": "¡El Grimorio Menor robó mi bola en el hoyo 9! ¡EN PLENO SWING! ¿Sabes lo que es un birdie perdido? ¿SABES? ¡Ni el viento me ha hecho eso, y el viento es MI ENEMIGO!",
"Zoologist": "Anoté diez mordiscos del Menor en el bestiario. Con hora. SOY PROFESIONAL. Más te vale que su página no cambie de sección. ¿Me explico?",
"Princess": "Como princesa debo ser amable, así que lo digo con cariño: controla a tu Grimorio Menor, ¿sí? ¡Porfa! Dicho con TODO el afecto diplomático disponible. ¡Que es mucho!",
"Genericos": "Ah, eres tú. Dile al Grimorio Menor que el barrio ha empezado a contar. SABEMOS lo que hizo. Solo estamos siendo educados. Por ahora.",
}

GENERICOS_M4_NUEVA = ("Tu cría juega al escondite con los espantapájaros del trigal. Nunca la encuentran: "
"se esconde en sus SOMBRAS. Se quedan muy quietas cuando ella está dentro. ¡Qué juego tan bonito!... ¿Verdad?")
ZOO_G2 = ("La página del animal perfecto llevaba años y años sin dejarse escribir. "
"Anoche amaneció ESCRITA, con mi letra. Dice: «lo vimos todos, y todos lo olvidamos». ¿Quiénes son TODOS?")

DORMIDO = [
"…zZz. ¿Ya llegaste?… No. Todavía no. Que siga cayendo el cielo. zZz.",
"…soñé que una mano. En el sueño era la tuya. Tenía los dedos contados: cinco. Los míos son páginas. Qué curiosidad, la de los que duermen. zZz.",
"…el cráter está tibio. Los cráteres de los otros libros eran fríos. Este se parece un poquito a mí. zZz.",
"…cuidado con la primera noche, portador… perdón: aún no eres portador. Se me anticipa el hambre. Es lo que tiene el sueño: cuenta el futuro y lo cuenta MAL. zZz.",
"…llevo eones contando caídas. Esta es la primera que huele a alguien. Las demás olían a nadie. zZz.",
"…las Z se me acaban. Cuando se acaben del todo, abriré el ojo. Todavía no. Todavía… zZz.",
"…las estrellas me hablaron: dicen que abajo hay manos. Yo solo he visto páginas. Acércate a ver quién tiene razón. zZz.",
"…¿sigues ahí? Bien. Los que esperan también alimentan. Un poquito. No es lo mismo, pero el hambre sabe esperar. zZz.",
]

def leer():
    with io.open(RUTA, "r", encoding="utf-8-sig") as f:
        return f.read().split("\n")

def escribir(lineas):
    with io.open(RUTA, "w", encoding="utf-8", newline="\n") as f:
        f.write("\n".join(lineas))
    print("ESCRITO:", RUTA)

def indent_of(linea):
    return linea[:len(linea) - len(linea.lstrip())] if linea.strip() else ""

def main():
    lineas = leer()
    n0 = len(lineas)

    # --- (1) GrimorioMenor1-4 tras el Grimorio4 de cada bloque ---
    for clave, textos in MENOR.items():
        idx_bloque = None
        for i, l in enumerate(lineas):
            if l.strip() == f"{clave}: {{":
                idx_bloque = i
                break
        if idx_bloque is None:
            raise SystemExit(f"BLOQUE NO ENCONTRADO: {clave}")
        fin = None
        for j in range(idx_bloque + 1, len(lineas)):
            if lineas[j].strip() == "}":
                fin = j
                break
        ancla = None
        for j in range(idx_bloque + 1, fin):
            if lineas[j].strip().startswith("Grimorio4:"):
                ancla = j
                break
        if ancla is None:
            raise SystemExit(f"Grimorio4 NO ENCONTRADO en {clave}")
        ind = indent_of(lineas[ancla])
        nuevas = [f"{ind}GrimorioMenor{k+1}: {t}" for k, t in enumerate(textos)]
        lineas = lineas[:ancla+1] + nuevas + lineas[ancla+1:]
    print("(1) GrimorioMenor1-4 x", len(MENOR), "bloques: OK")

    # --- (2) Reclamos reescritos (dentro del bloque Reclamos: { ... }) ---
    idx_recl = None
    for i, l in enumerate(lineas):
        if l.strip() == "Reclamos: {":
            idx_recl = i
            break
    if idx_recl is None:
        raise SystemExit("BLOQUE Reclamos NO ENCONTRADO")
    fin_recl = None
    for j in range(idx_recl + 1, len(lineas)):
        if lineas[j].strip() == "}":
            fin_recl = j
            break
    for clave, texto in RECLAMOS.items():
        hecho = False
        for j in range(idx_recl + 1, fin_recl):
            if lineas[j].strip().startswith(f"{clave}: "):
                ind = indent_of(lineas[j])
                lineas[j] = f"{ind}{clave}: {texto}"
                hecho = True
                break
        if not hecho:
            raise SystemExit(f"RECLAMO NO ENCONTRADO: {clave}")
    print("(2) Reclamos x", len(RECLAMOS), ": OK")

    # --- (3) purgas de niños ---
    for i, l in enumerate(lineas):
        s = l.strip()
        if s.startswith("Mascota4:") and "escondite con los niños" in s:
            ind = indent_of(l)
            lineas[i] = f"{ind}Mascota4: {GENERICOS_M4_NUEVA}"
            print("(3a) Genericos.Mascota4 purgada (linea", i+1, ")")
        if s.startswith("Grimorio2:") and "desde niña sin dejarse escribir" in s:
            ind = indent_of(l)
            lineas[i] = f"{ind}Grimorio2: {ZOO_G2}"
            print("(3b) Zoologist.Grimorio2 «desde niña» purgada (linea", i+1, ")")
    resto = [i+1 for i, l in enumerate(lineas) if "niños" in l or "niñas" in l]
    if resto:
        raise SystemExit(f"QUEDAN REFERENCIAS A NIÑOS en lineas {resto}")

    # --- (4) Caida.Dormido1-8 tras BotonTomar ---
    hecho = False
    for i, l in enumerate(lineas):
        if l.strip().startswith("BotonTomar:"):
            ind = indent_of(l)
            nuevas = [f"{ind}Dormido{k+1}: {t}" for k, t in enumerate(DORMIDO)]
            lineas = lineas[:i+1] + nuevas + lineas[i+1:]
            hecho = True
            break
    if not hecho:
        raise SystemExit("BotonTomar NO ENCONTRADO")
    print("(4) Caida.Dormido1-8: OK")

    escribir(lineas)
    print("LINEAS:", n0, "->", len(lineas))

if __name__ == "__main__":
    main()
