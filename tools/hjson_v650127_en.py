#!/usr/bin/env python3
# -*- coding: utf-8 -*-
# v6.50.127 — LOCALIZATION SURGERY (en-US): Lesser Grimoire pools (26x4),
# Reclamos rewritten (the BOOK's bites), child-reference purges, Dormido1-8.
import io

RUTA = "AethonMod/Localization/en-US_Mods.AethonMod.hjson"

MENOR = {
"Guide": [
"Your Lesser Grimoire sat on my maps and the routes redrew themselves: every single one now ends at YOUR house. I made a new map and it happened again. What if that was always the true path?",
"Old school advice: never turn your back on a book. The Menor took it literally: now it follows me FACE-ON. I can't give advice while walking backwards.",
"Last night I dreamed the perfect survival guide. I woke up, and your Lesser Grimoire was on my desk — the page on 'how to survive your pet' had a correction: it crossed out 'pet' and wrote 'owner'.",
"I taught the Lesser Grimoire to read the stars for direction. It learned. Now the stars drift a little… to follow IT. Navigation will be the least of our worries once the whole sky is its compass.",
],
"Merchant": [
"Your Lesser Grimoire left three coins on my counter, sorted by shine. I counted them: they were the ones that 'went missing' on Tuesday. I no longer know if I'm a merchant or its accountant.",
"I offered the Menor credit. It opened a tiny page: there was a drawing of MY drawer, open, with a little arrow. That's not a yes: that's an 'already saw it.' I locked it. The key woke up bent.",
"The Lesser Grimoire doesn't buy or sell: it COLLECTS. It sat on my safe until I tipped it a coin. Did you teach it that… or did it already know?",
"Guild notice: never haggle with something that bites. I haggled with the Menor for sport, and the coin I offered came back with the face rubbed off. It's not a coin anymore: it's a circle that says 'no'.",
],
"Nurse": [
"I took your Lesser Grimoire's pulse: it hasn't got one. I took the pulse of its three little eyes: one beats. The middle one. Only when it looks at you. Diagnosis: that's not a heartbeat, that's a count.",
"The Menor sprawled over my patient log, and every line reading 'cause: pet bite' slid on its own into the 'symptoms' column. The book doesn't bite out of malice: it bites for the record.",
"I drew one drop of ink from the Lesser Grimoire to analyze it. The ink analyzed MY serum instead: it handed the vial back with a rewritten label. 'Healthy. For now.' The lab reopens when it feels like it.",
"If your Lesser Grimoire follows you around while you're hurt, that's not affection: that's dessert. Nurse's orders: heal quickly — dessert doesn't like to wait.",
],
"ArmsDealer": [
"Your Lesser Grimoire climbed onto my counter and every shotgun took its own safety off. One by one. Like they were asking its permission. I don't sell guns near your house anymore: I sell vests.",
"I showed the Menor my finest revolver. It sniffed it, cracked open a tiny page… and there was the revolver, drawn OWNERLESS. Gun dealers don't scare: gun dealers COMPLAIN. That drawing was a customer!",
"Your Lesser Grimoire bit a spare magazine. Nothing happened. But the rounds now aim a tiny bit upward on their own — toward the birds. My birds. Probably.",
"Rule of the trade: never point at anything that blinks. The Menor blinks THREE times at once. That's three targets… or a warning. I chose the warning and holstered the gun.",
],
"Dryad": [
"Nature talks to me, but about your Lesser Grimoire it only says one thing: 'nothing grows where it sleeps.' I saw it napping in the meadow: the grass around it had arranged itself in a circle. Neither inside nor out: around.",
"Your Menor doesn't dirty the earth: it inks it. Wherever it sits, the ground smells like an old page. The worms no longer flee the spade: they flee the smell of the library.",
"I asked an old oak what it thinks of the Lesser Grimoire. It answered slowly, the way oaks do: 'the small was always the largest.' I asked nothing else. Oaks don't give second answers.",
"The Lesser Grimoire bit my ankle and the grass around me LAUGHED. Do plants laugh? Before today: no. Today: once. On behalf of the book. New ledgers, old nature.",
],
"Demolitionist": [
"Your Lesser Grimoire held MY dynamite without fear! And it didn't blow up ON him — it blew up NEARBY, like it was asking permission! I want permission too! From the dynamite, NOT from the book!",
"The Menor counted my fuse. THE FUSE! It counted the seconds it takes and looked at me like it was saying 'you've got two to spare.' AND I HAD TWO TO SPARE! Where did it get that? Not from the future — the future is ITS!",
"I gave the Lesser Grimoire a firecracker as a present. It kept it between the pages. When it opened the tiny page later, the THUNDER of the memory came out. Scared the whole block. The block forgives. I don't.",
"Guild notice! Do NOT leave cartridges near the Menor: it SORTS them! By size, by noise… BY NOISE? Who sorts by noise? Someone who has heard the end of everything and put it in a line!",
],
"Clothier": [
"Your Lesser Grimoire unpicked the seam of my favorite shirt and sewed it back with thread of ink. The seam tells a story: it was the list of everyone who's ever worn that shirt. I'm number FOUR.",
"I sewed the Menor a silk bookmark. It used it once and returned it with a little crease in the corner, like a used handkerchief. Books carry handkerchiefs now. I sewed that handkerchief. I'm old.",
"Your Menor sat in my sewing room and the needles lined up for it. In a row. Pointing at the door. I won't sew that door for any money: that door sews itself.",
"The Lesser Grimoire dropped a button in my lap. From a fabric no one weaves anymore, from an age before I was born. Old tailors know what lives in pockets: I don't ask, I SEW.",
],
"Goblin": [
"Your Lesser Grimoire reforged my wrench: now it tightens in both directions at once. It's impossible. It's beautiful. It's mine and I'm not letting go.",
"The Menor watched me work and my little hands sped up on their own. I finished a TWO-day job in one afternoon. The client cried. So did I. The warranty covers the tears: it does NOT.",
"I showed the Lesser Grimoire the blueprint of a perfect machine. It drew the correction with its little eye. The blueprint worked. The blueprint is alive. I named it and sent it to bed.",
"Workshop rule: whatever bites, gets reforged. The Menor bit me and I reforged it one eye at a time. Kindly. The GOBLIN way, which is the only way.",
],
"Wizard": [
"The three little eyes of your Lesser Grimoire turn like a permanent incantation. I asked what it summons. They blinked in unison and my hat moved on its own. I no longer ask: I THEORIZE.",
"The Menor stole a spell from my grimoire. It returned it the next day: with half the words… and twice the effect. Magic hasn't been written that way since the breaking. Was IT there?",
"Your Lesser Grimoire and I played arcane chess. We drew three times. On the fourth, the game ended itself: every piece was looking at your house. I play. IT sets the board.",
"I showed the Menor my oldest staff. It sniffed it and the gem went dark. Not from fear: from RESPECT. There are hierarchies in the arcane, and your little book just reminded me of mine.",
],
"Mechanic": [
"Your Lesser Grimoire nibbles copper wire. The marks are exactly two millimeters apart, all identical. That's not a tooth: that's a gauge with three mouths.",
"The Menor stared at my circuit and the circuit fixed itself. Do you know what that means to an engineer? A WISH! And do you know the price? The wrench no longer obeys me.",
"I measured the blinking of its little eyes: a pattern. I translated it. It says 'still functioning.' Who taught a book to say 'still functioning'? And WHY does it say it to me every time I blink?",
"The Lesser Grimoire sorted my screws by size without touching them. I TOUCHED THEM. Fine. But I dreamed them sorted and woke up to them sorted. Engineering doesn't sleep anymore.",
],
"Santa": [
"Ho, ho, ho… your Lesser Grimoire unwrapped the presents BEFORE Christmas. It took everything except the coal. Who refuses coal? I like it!",
"I asked the Menor what it wanted for a present. It opened a tiny page: my list — the list of every present in the world… with a little star in the margin. I closed it. I closed it and locked it.",
"Your Lesser Grimoire sat in my empty sleigh and the reindeer looked at each other. They said nothing. Reindeer never shut up. That night they flew without me: THREE laps, and they left the sleigh warm.",
"I gave the Menor a jingle bell. It gave it back ringing on its own. Bells don't ring by themselves, friend: something rings them. And whatever rings it — which side of the bell was it standing on?",
],
"Truffle": [
"Your Lesser Grimoire sniffs me like I'm food. Flattering? Yes. Concerning? Also. We mushrooms know when something is hungry for certain flavors.",
"Your Menor fell asleep among my spores and the spores dreamed. They dreamed PAGES. I've dreamed in spores forever: those were someone else's. The mushroom lent the dream and the book kept it.",
"I asked the Lesser Grimoire whether books eat mushrooms. It didn't answer: it licked its little page. Books don't have tongues. THAT was the detail that shut me up.",
"The Menor followed me all day and my mushroom hat came out shaped like a bookmark. I liked it. I liked it and it scared me, and in the forest those are the same thing.",
],
"Steampunker": [
"Your Lesser Grimoire built a walking machine with THREE cogs. I need twelve. It cheats and I need to know how. Is it engineering or is it magic? Both offend me!",
"The Menor spun my wheels backwards without touching them. The AIR near it touched them. I set a guard-cog on it and the cog resigned. Resigning cogs: that's new for the factory.",
"I showed the Lesser Grimoire my blueprint of the perfect vehicle. It corrected it: crossed out 'steering wheel' and wrote 'heading.' Crossed out 'seat' and wrote 'throne.' I built the rest as told. The vehicle FLIES. I ordered wheels.",
"The Menor perched on my boiler and the steam came out in rune shapes. Runes in the steam? That's a signature. My boiler no longer signs in English: it signs in BOOK.",
],
"DyeTrader": [
"Your Lesser Grimoire drips a violet that is NOT in my catalog. I call it 'Menor violet.' The catalog refuses it: the color was already sold before it existed.",
"The Menor rolled around in my dyes and left little inky footprints across the ceiling. I don't wash anything anymore: I exhibit it. The top dye is the most requested. Who orders the ceiling? People. Strange people.",
"I dyed a tiny page of the Lesser Grimoire cobalt blue. It woke up violet again, with the cobalt set in the corner like a tip. Do you know what that is? A customer teaching you the trade.",
"The Menor looked at my barrels and each barrel dyed itself whichever color it was missing. Barrels don't dye themselves. That day they did. I have witnesses: my witnesses don't discuss the matter.",
],
"PartyGirl": [
"Your Lesser Grimoire IS the party! It refused the confetti and took the balloons: it holds them up with its stare. The BALLOONS, in the AIR, with a STARE! I adore it!",
"I put a party hat on the Menor. It took it off by itself, folded into origami, and returned it inside-out. The hat is a crane now. The crane flies. The party doesn't need me anymore!",
"The Lesser Grimoire dimmed my lights with one blink and lit them with two. Two? Lights come in TWOS? They do now: when IT lights them, they come in twos. I don't use switches anymore: I use invitations.",
"The Menor stared at my piñata and the piñata opened itself. INWARD. Who breaks a piñata inward? Something that wants to see what's inside… before everyone else sees it.",
],
"Cyborg": [
"Your Lesser Grimoire chases my laser dot. I do NOT have a laser dot. My questions are two: where does it come from… and why does it aim at ME when I sleep?",
"The Menor blinked in code. I decoded it: 'small unit: watched.' Watched by whom? By it. Watched for what? The code didn't say. The SILENCE after the code said it.",
"I logged ten bites from the Lesser Grimoire on my chassis. Unit by unit. My grievance protocol is ACTIVE, brother. The maintenance fee on my good mood is billed to YOU.",
"The Menor plugged into my diagnostic port and read EVERYTHING. Then it closed my chest plate softly, the way you close a book that's already been read. That was yesterday. Today I don't sleep.",
],
"Painter": [
"Your Lesser Grimoire crossed my easel and left three little inked footprints in a row! I titled it 'The Signature.' Sold within the hour. Bring me more — the market knows a master when it sees one!",
"The Menor looked at my still lifes and sighed. IT SIGHED. I repainted the still life with one extra page on the table. The sighing stopped. I now paint for the critic who never speaks: the best client in the house.",
"I painted the Lesser Grimoire's portrait. The three little eyes came out on their own, with their own light. I didn't mix that light: the canvas brought it. No one else paints on that canvas now.",
"The Menor corrected my perspective: one line, in the margin. The line was perfect. I've spent thirty years looking for that line. You borrowed it from a book, and the book just gave it away. BRING ME MORE!",
],
"WitchDoctor": [
"The spirits call your Lesser Grimoire 'the small hungry one.' Spirits do not baptize lightly. When they baptize, I take notes.",
"The Menor sat in my offering circle and the candles leaned toward it. ALL of them. Spirit candles don't lean: they TOWER. That night they leaned. The offering went to the book.",
"I asked the Lesser Grimoire about dreams. It answered with a blank page. The spirits laughed. The blank page is the answer: never ask hunger about sleep.",
"Your Menor followed me to the graveyard and the dead got comfortable. In graveyards the dead are NEVER comfortable. That day: they were. I closed the graveyard out of respect. The respect was for the book.",
],
"Pirate": [
"Your Lesser Grimoire marked my treasure map with an ink X! My X, my ink, my map… and the treasure WAS THERE! Who taught a book to bury? Who taught a PIRATE?",
"The Menor perched on my shoulder and the parrot moved out. THE PARROT! Twenty years on that shoulder. The shoulder belongs to the book now. Yaaar… apparently.",
"Your Menor bit my anchor claw and the claw came out BENT! You know what that means at sea? A bad omen. And you know what it means on my ship? A WARNING: whatever bends claws, bends ships.",
"The Lesser Grimoire read my logbook in one night. The next morning the log came back corrected: PERFECT spelling, the heading marked… toward your house. I didn't mark that heading. The heading marked itself.",
],
"Stylist": [
"Your Lesser Grimoire sleeps on my clients' heads and leaves them INK-WAVES! I charge extra for 'library waves,' and it's not even a lie!",
"The Menor stared at my comb and the comb combed itself. In MID-AIR. I've been styling hair since I was ten: never seen that. The comb isn't mine anymore: it belongs to whatever looks at it like that.",
"I trimmed a torn page for the Lesser Grimoire. The page woke up with bangs. Books with bangs! A new client saw it and asked for 'the book cut.' The book cut is ME.",
"The Menor picked my salon's color of the month: ink violet. Violet dye doesn't exist. I mixed it. It DOES now. The salon smells like an old page and the queue rounds the corner: the whole town wants to smell like a book.",
],
"Angler": [
"Your Lesser Grimoire spends HOURS staring at my fish tank! Something with three eyes smiling at my dinner for three days straight! I don't eat fish in front of it anymore!",
"The Menor taught me to fish without a hook: it leaned over the water and the fish apologized. What did you say to them, book? Fish don't apologize! That day they did — I saw it, and I didn't catch a thing!",
"The Lesser Grimoire bit MY HOOK and the hook came out bent! You know what bending a hook in one bite takes? Not even the giant squid manages that! And the hook was a spare, alright!",
"The Menor sat on my bait box and the worms lined up. Worms don't line up. That day they did. I fished with respect and threw everything back: the lake knows who's in charge.",
],
"Tavernkeep": [
"Your Lesser Grimoire licks the mugs until they shine! On the INSIDE, friend! The kid eats free for life!",
"The Menor perched on my mead barrel and the mead aged a whole season in one night. The house calls it 'the Menor barrel.' The barrel doesn't pour for everyone: the barrel pours for whoever the book looks at.",
"I served the Lesser Grimoire a single drop in a saucer. It didn't touch it: it SNIFFED it. And by morning the drop had gone old. How does a drop age overnight? Ask time itself: that night, time was on its side.",
"The Menor tells the saloon's stories better than I do. The regulars ask for 'the book's version.' I tell mine and the book puts on its ASTONISHED eyes. Three astonished eyes. THAT'S my audience!",
],
"Golfer": [
"Your Lesser Grimoire brought me my ball! And everyone else's! And one made of OLD WOOD with initials it's better not to ask about!",
"The Menor swapped my ball for an EYE on hole 9. MID-SWING! The eye tracked the trajectory… and the trajectory corrected itself. The best shot of my life, and it WASN'T ME!",
"I taught the Lesser Grimoire to read the wind. It already knew: the wind TOLD it. Now the wind blows the wrong way for me on 18. The wind has an owner, and the owner has three eyelids.",
"The Menor wrote my handicap on a tiny page. The handicap went DOWN. The book is TRAINING me! You know what that is? A caddie that never talks, never charges… and watches. Whatever watches like that isn't charging: it's INVESTING.",
],
"Zoologist": [
"Your Lesser Grimoire has its own bestiary page… written by ITSELF! In its own hand! And the handwriting changes when I look twice! That is the most BESTIARY thing I have ever bestiaried!",
"The Menor fits no category: not a bird, not a mammal, not even a book. I made a new category: 'lesser.' The bestiary accepted it without a murmur. The bestiary NEVER accepts without a murmur.",
"I watched the Lesser Grimoire all afternoon: its three little eyes blink in the order of the lunar phases. I noted the pattern. The next morning the page had a note: 'well observed.' THE BESTIARY GRADED ME!",
"The Menor and I played 'who blinks less.' It won. It ALWAYS wins. On the thirteenth match it winked one little eye at me… for sport. Books don't lose: they LET you win. That day I stopped counting blinks and started counting days.",
],
"Princess": [
"Your Lesser Grimoire is the most courteous creature in the realm! It bowed to me… with all THREE eyelids at once! And dipped its flight so my crown wouldn't slip! AAAY!",
"I signed a peace treaty between the Menor and the baker's cat: one inky paw-print and one floury paw-print! My first peace WITHOUT kingdoms! The book added a clause: the clause was a drawing of the cat ASLEEP.",
"The Lesser Grimoire takes tea with me. When I pass the cookies it doesn't eat them: it counts them, leaves me the biggest one… and keeps the CRUMB. The crumb is its. In this realm we respect the crumb.",
"I asked the Menor what it wishes for the realm. It opened a tiny page: my coronation portrait… with the crown just a little lower. I understood. In this realm, even crowns know how to bow.",
],
"Genericos": [
"Yesterday I saw the three little eyes of your Lesser Grimoire watching me through my window. All night. They didn't blink once. Neither did I, that night. What an… insistent little thing.",
"Your Menor followed me while I swept the street and left little lights wherever I'd swept. It saves me the night. The street came out cleaner than in daylight: whoever lights the way, also tidies it.",
"Does your Lesser Grimoire read? I found it asleep on the district ledger. The page on 'noisy pets' woke up bitten, and the one on 'noisy books' woke up UNDERLINED.",
"It played with the town hall weathervane and now the north stares at me funny. Not broken: it STARES. Who taught it that? Those of us from the north prefer not to know.",
],
}

RECLAMOS = {
"Guide": "Yes? Oh. It's you. Your Lesser Grimoire bit my ankle while I was checking my maps. Twice. The second one hurt MORE, for the record. What a... determined little thing.",
"Merchant": "Before we discuss prices: the Lesser Grimoire owes me FIVE coins in bite fees. Nothing personal. Everything costs something here. Even your book's ink gets invoiced.",
"Nurse": "You! Exactly you! If your Lesser Grimoire bites me mid-treatment one more time, YOU are giving the next injection. Under my supervision. Without anesthesia. Are we clear?",
"ArmsDealer": "Look here, friend. Your mini book bit me while I was reloading. If my shotgun had gotten spooked, this town would have a brand-new hole in it, and you'd be paying for it. Rein it in.",
"Dryad": "Nature forgives; I almost do. But your Lesser Grimoire gnawed my arm, and my herbs LAUGHED. Do plants laugh? Before today: no.",
"Demolitionist": "The Lesser Grimoire bit me while I was holding dynamite! DYNAMITE! One sneeze and the whole block goes sky-high! Teach it manners or I'll teach it ballistics, ha! Joking! ...JOKING!",
"Clothier": "Ah, friend... at my age, bones creak on their own. No help needed. The Lesser Grimoire nibbled on my leg. Tell it this tailor is all out of... what's the word? Ah, yes! Juice.",
"Goblin": "Listen well, human: if the Lesser Grimoire enters my workshop again, I will reforge it into BITE MARKS. With warranty. Without warranty. NO warranty, do you read my anger?!",
"Wizard": "*sigh* When I was an apprentice, grimoires respected their elders... and their calves. Yours, the Menor, sampled mine. Old wizard tastes salty, doesn't it, you shameless little book?",
"Mechanic": "The Lesser Grimoire lay down on my workbench. On TOP of the new circuit. Do you know what page-ink does to copper? Neither do I, AND I HATE NOT KNOWING! I HATE IT!",
"Santa": "Ho, ho... ho. No. No presents for that book until January. And I caught it chewing MY list, too. MINE. Do you have any idea what good paper costs?!",
"Truffle": "It sniffs me like a main course, and now it BITES TOO, friend! Talking mushrooms are NOT food! The key word is TAL-KING! Teach it that!",
"Steampunker": "Your Menor played with my cogs, and now one spins backwards. BACKWARDS, and smiling. Do cogs smile? THAT one does. And it won't let me take it apart. It's MINE, from MY toolbox!",
"DyeTrader": "The Lesser Grimoire knocked over my dyes! Purple floor, purple cat, PURPLE ME! And you know what? It brings out my eyes. THAT IS NOT THE POINT: there were FOUR barrels!",
"PartyGirl": "I love your mini book, you know? LOVE it. But it bit me MID-PARTY. The lights dimmed with embarrassment — the LIGHTS, and they don't even HAVE faces! No idea how it pulled that off!",
"Cyborg": "I logged ten impacts of 1 unit of damage. TEN. Data-precise: ten. My grievance protocol is ACTIVE, brother. The maintenance fee on my good mood is billed to YOU.",
"Painter": "The Lesser Grimoire crossed my studio, and my canvases woke up with... teeth marks of ink? It's art. ANNOYING art, but art. Next time, it can ENROLL as a muse like everybody else!",
"WitchDoctor": "The spirits laughed when the Menor bit me. LAUGHED, friend. Do you know what it is when the DEAD laugh at you, to your face? I don't want you to find out either. Pay the offering.",
"Pirate": "No cannon ever cannonaded me like your Lesser Grimoire! Ten bites, an' I counted 'em the way I count me coins! An' NOT ONE of 'em in the good way! Yaaar... with rage!",
"Stylist": "It bit me in the middle of a haircut! The CLIENT'S haircut! The client's never coming back, the haircut came out perfect (my work, obviously), and the panic got left behind as a tip. FOR ME!",
"Angler": "The Lesser Grimoire bit me and I fell in the lake! ME! I never fall in the lake — the lake falls in with ME! Glub! That doesn't wash out, you traitorous book!",
"Tavernkeep": "That Lesser Grimoire of yours emptied a whole mug with its teeth! Do you know what that IS, friend? Not even in the eternal war did anyone touch the mugs! Men DIED for less there!",
"Golfer": "The Lesser Grimoire stole my ball on hole 9! MID-SWING! Do you know what a lost birdie IS? DO YOU? Even the wind has never done that to me, and the wind is my NEMESIS!",
"Zoologist": "I logged ten bites from the Menor in my bestiary. TEN. Timestamped and everything — I'm a PROFESSIONAL! You'd better pray its page doesn't change sections. Do I make myself clear?",
"Princess": "As a princess I must be kind, so I say this with a great deal of love: kindly control your Lesser Grimoire, yes? PLEEEASE! ...Said with ALL available diplomatic affection. Which is a lot!",
"Genericos": "Ah, it's you. Look, about the Lesser Grimoire... tell it the neighbors have started counting. We KNOW what it did. We're just being polite about it. For now.",
}

GEN_M4_EN = ("Your little light plays hide-and-seek with the scarecrows of the wheat field. They never find "
"her: she hides in their SHADOWS, you know? The shadows go very, very still when she's inside. "
"What a lovely game!... Right? ...Right?")

DORMIDO = [
"…zZz. Are you here already?… No. Not yet. Let the sky keep falling. zZz.",
"…I dreamed of a hand. In the dream it was yours. Its fingers came counted: five. Mine are pages. What a curious thing, those who sleep. zZz.",
"…the crater is warm. The craters of the other books were cold. This one looks a little bit like me. zZz.",
"…careful with the first night, bearer… pardon: you are not a bearer yet. My hunger gets ahead of me. That's the thing about sleep: it tells the future, and tells it WRONGLY. zZz.",
"…I've spent eons counting falls. This is the first one that smelled of someone. The rest smelled of no one. zZz.",
"…my Z's are running out. When they run out entirely, I will open my eye. Not yet. Not yet… zZz.",
"…the stars spoke to me: they say there are hands down below. I've only ever seen pages. Come closer, and we'll see who was right. zZz.",
"…are you still there? Good. Those who wait also feed me. A little. It's not the same, but hunger knows how to wait. zZz.",
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

    # purgas de referencias a children
    for i, l in enumerate(lineas):
        s = l.strip()
        if s.startswith("Mascota4:") and "hide-and-seek with the town's children" in s:
            ind = indent_of(l)
            lineas[i] = f"{ind}Mascota4: {GEN_M4_EN}"
            print("(3a) Genericos.Mascota4 purged (line", i+1, ")")
        if s.startswith("Grimorio2:") and "since I was a little girl" in s:
            nueva = l.replace("has refused me since I was a little girl", "has refused me for years and years")
            lineas[i] = nueva
            print("(3b) Zoologist.Grimorio2 'little girl' purged (line", i+1, ")")
    resto = [i+1 for i, l in enumerate(lineas)
             if "children" in l or " kids " in l or "kids look" in l]
    if resto:
        raise SystemExit(f"QUEDAN REFERENCIAS A CHILDREN en lineas {resto}")

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
