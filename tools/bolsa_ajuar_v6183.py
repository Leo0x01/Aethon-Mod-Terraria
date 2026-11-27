#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""v6.183 — LA CIRUGÍA DEL AJUAR DEL PORTADOR (la letra: «a partir de
ahora ya no es necesario que el jugador reciba lo siguiente en vez de
eso ponlo en una bolsa y esa bolsa ponla dentro de la Bolsa Maestra
junto con todas las demas bolsas: Forma Ascendida 2/3/4/8 · Pequeño
Recuerdo de Aethon · Halo Arcoíris · Brillo de los Soles Rúnicos ·
Coro de los Profundos · Pluma del Escriba Santo · La Regla del Cielo ·
Corona de Plumas · Derrame de Tinta · La Última Palabra»).

LOS TRECE REGALOS (2/3/4/8 + 9) MUEREN como entrega directa al entrar
al mundo y nacen en EL AJUAR DEL PORTADOR — una bolsa de la casa (30×30
por gen_bolsa_ajuar_v6183.py) que vive DENTRO del Cofre del Arsenal
(BolsaMaestra), junto con las demás.
Cirugía con asserts: si el patrón no está, MUERE.
"""
import io

BASE = '/home/z/my-project/mod/AethonMod'


def parche(ruta, viejo, nuevo, nota):
    p = f'{BASE}/{ruta}'
    with io.open(p, encoding='utf-8') as f:
        s = f.read()
    assert viejo in s, f'{ruta}: no encuentro «{nota}»'
    assert s.count(viejo) == 1, f'{ruta}: «{nota}» ×{s.count(viejo)}'
    s = s.replace(viejo, nuevo, 1)
    with io.open(p, 'w', encoding='utf-8') as f:
        f.write(s)
    print(f'  ✔ {ruta}: {nota}')


# ── 1. LA CLASE NUEVA (antes de BolsaMaestra) ──────────────────────
parche(
    'Content/Items/Bolsas/BolsasCategorias.cs',
    '    public class BolsaMaestra : BolsaCategoria',
    '''    // ======================================================================
    //  v6.183 — EL AJUAR DEL PORTADOR (la letra: «a partir de ahora ya
    //  no es necesario que el jugador reciba lo siguiente en vez de eso
    //  ponlo en una bolsa y esa bolsa ponla dentro de la Bolsa Maestra
    //  junto con todas las demas bolsas»): los TRECE REGALOS de la
    //  casa — las cuatro LUCES del portador (Forma Ascendida 2/3/4/8),
    //  su PEQUEÑO RECUERDO y los OCHO TESOROS del oficio — mueren como
    //  entrega directa al entrar al mundo y viven aquí, cosidos en
    //  oro, DENTRO del Cofre del Arsenal.
    // ======================================================================
    public class BolsaAjuarPortador : BolsaCategoria
    {
        protected override string Titulo => "El Ajuar del Portador — Los Regalos de la Casa";
        protected override string NombreCorto => "El Ajuar del Portador";
        protected override Color ColorFiesta => new(255, 214, 130);
        protected override string Nota => "Los trece regalos que la casa cosió en oro para su portador";

        protected override List<(int tipo, int pila)> Contenido()
        {
            var l = new List<(int, int)>();
            // LAS CUATRO LUCES (Forma Ascendida 2 · 3 · 4 · 8)
            l.Add((ModContent.ItemType<Items.Cosmetics.FormaAscendidaDosItem>(), 1));
            l.Add((ModContent.ItemType<Items.Cosmetics.FormaAscendidaTresItem>(), 1));
            l.Add((ModContent.ItemType<Items.Cosmetics.FormaAscendidaCuatroItem>(), 1));
            l.Add((ModContent.ItemType<Items.Cosmetics.FormaAscendidaOchoItem>(), 1));
            // EL PEQUEÑO RECUERDO DE AETHON
            l.Add((ModContent.ItemType<Items.Llamados.AethonMenorItem>(), 1));
            // LOS ADORNOS (el arcoíris de la Forma 3 · los soles rúnicos)
            l.Add((ModContent.ItemType<Items.Cosmetics.HaloArcoirisItem>(), 1));
            l.Add((ModContent.ItemType<Items.Cosmetics.BrilloSolarItem>(), 1));
            // LOS TESOROS DEL OFICIO
            l.Add((ModContent.ItemType<Weapons.TresCasas.CoroDeLosProfundos>(), 1));
            l.Add((ModContent.ItemType<Weapons.TresCasas.PlumaDelEscribaSanto>(), 1));
            l.Add((ModContent.ItemType<Weapons.Regla.LaReglaDelCielo>(), 1));
            l.Add((ModContent.ItemType<Weapons.Auras.CoronaDePlumas>(), 1));
            l.Add((ModContent.ItemType<Weapons.Auras.DerrameDeTinta>(), 1));
            l.Add((ModContent.ItemType<Weapons.Definitivas.LaUltimaPalabra>(), 1));
            return l;
        }
    }

    public class BolsaMaestra : BolsaCategoria''',
    'la clase BolsaAjuarPortador')

# ── 2. DENTRO DEL COFRE (BolsaMaestra.Contenido) ───────────────────
parche(
    'Content/Items/Bolsas/BolsasCategorias.cs',
    '''            l.Add((ModContent.ItemType<BolsaSuperforja>(), 1));
            // v6.100 — LA BOLSA DEL TALLER MURIÓ con el taller entero (la''',
    '''            l.Add((ModContent.ItemType<BolsaSuperforja>(), 1));
            // v6.183 — EL AJUAR DEL PORTADOR (bolsa 29 del cofre): los
            // trece regalos que antes caían DIRECTO al entrar al mundo
            // (la letra: «ponlo en una bolsa y esa bolsa ponla dentro de
            // la Bolsa Maestra junto con todas las demas bolsas»).
            l.Add((ModContent.ItemType<BolsaAjuarPortador>(), 1));
            // v6.100 — LA BOLSA DEL TALLER MURIÓ con el taller entero (la''',
    'el ajuar entra al cofre')

# ── 3. LAS ENTREGAS DIRECTAS MUEREN (ShardPlayer) ─────────────────
parche(
    'Content/Players/ShardPlayer.cs',
    '''                entregados += EntregarSiNoTiene(ModContent.ItemType
                    <global::AethonMod.Content.Items.Cosmetics.FormaAscendidaDosItem>());
                entregados += EntregarSiNoTiene(ModContent.ItemType
                    <global::AethonMod.Content.Items.Cosmetics.FormaAscendidaTresItem>());
                // v6.50.53 — LA FORMA ASCENDIDA 4 (EL SERAFÍN — la cuarta luz).
                entregados += EntregarSiNoTiene(ModContent.ItemType
                    <global::AethonMod.Content.Items.Cosmetics.FormaAscendidaCuatroItem>());''',
    '''                // v6.183 — LAS FORMAS ASCENDIDAS 2/3/4/8 MURIERON como
                // entrega directa (la letra: «ponlo en una bolsa y esa
                // bolsa ponla dentro de la Bolsa Maestra»): viven en EL
                // AJUAR DEL PORTADOR (Bolsas.BolsaAjuarPortador), dentro
                // del Cofre del Arsenal — como las demás bolsas.''',
    'las formas 2/3/4 mueren directas')

parche(
    'Content/Players/ShardPlayer.cs',
    '''                entregados += EntregarSiNoTiene(ModContent.ItemType
                    <global::AethonMod.Content.Items.Llamados.AethonMenorItem>());
                // v6.50.54 — EL HALO ARCOÍRIS (el arcoíris de la Forma 3
                // hecho adorno: pequeño, alrededor de la cabeza).
                entregados += EntregarSiNoTiene(ModContent.ItemType
                    <global::AethonMod.Content.Items.Cosmetics.HaloArcoirisItem>());
                // v6.51.3 — EL BRILLO DE LOS SOLES RÚNICOS (los destellos
                // de 4 puntas de los soles de 3 anillos+, hechos adorno).
                entregados += EntregarSiNoTiene(ModContent.ItemType
                    <global::AethonMod.Content.Items.Cosmetics.BrilloSolarItem>());''',
    '''                // v6.183 — EL RECUERDO, EL HALO Y EL BRILLO murieron
                // directos (misma letra): viven en EL AJUAR DEL PORTADOR
                // junto con las formas y los tesoros del oficio.''',
    'recuerdo+halo+brillo mueren directos')

parche(
    'Content/Players/ShardPlayer.cs',
    '''                entregados += EntregarSiNoTiene(ModContent.ItemType
                    <global::AethonMod.Content.Weapons.TresCasas.CoroDeLosProfundos>());
                entregados += EntregarSiNoTiene(ModContent.ItemType
                    <global::AethonMod.Content.Weapons.TresCasas.PlumaDelEscribaSanto>());''',
    '''                // v6.183 — EL CORO Y LA PLUMA murieron directos
                // (misma letra): viven en EL AJUAR DEL PORTADOR.''',
    'coro+pluma mueren directos')

parche(
    'Content/Players/ShardPlayer.cs',
    '''                entregados += EntregarSiNoTiene(ModContent.ItemType
                    <global::AethonMod.Content.Weapons.Regla.LaReglaDelCielo>());
                entregados += EntregarSiNoTiene(ModContent.ItemType
                    <global::AethonMod.Content.Weapons.Auras.CoronaDePlumas>());
                entregados += EntregarSiNoTiene(ModContent.ItemType
                    <global::AethonMod.Content.Weapons.Auras.DerrameDeTinta>());
                entregados += EntregarSiNoTiene(ModContent.ItemType
                    <global::AethonMod.Content.Weapons.Definitivas.LaUltimaPalabra>());''',
    '''                // v6.183 — LA REGLA DEL CIELO, LA CORONA, EL DERRAME
                // Y LA ÚLTIMA PALABRA murieron directas (misma letra):
                // viven en EL AJUAR DEL PORTADOR — la regla de la casa
                // cambia por la letra: lo nuevo llega EN SU BOLSA.''',
    'regla+corona+derrame+palabra mueren directos')

parche(
    'Content/Players/ShardPlayer.cs',
    '''                // v6.110 — LA FORMA ASCENDIDA 8 (la letra: «y ademas crear
                // la forma ascendida 8, 9 y 10» — la v6.111 la re-creó como
                // LAS OFANIM y la v6.112 la CONSERVA: la única de la
                // segunda década que vive). La regla permanente: todo lo
                // nuevo llega DIRECTO.
                entregados += EntregarSiNoTiene(ModContent.ItemType
                    <global::AethonMod.Content.Items.Cosmetics.FormaAscendidaOchoItem>());''',
    '''                // v6.183 — LA FORMA ASCENDIDA 8 murió directa (misma
                // letra): vive en EL AJUAR DEL PORTADOR con sus hermanas.''',
    'la forma 8 muere directa')

# ── 4. EL HJSON ×3 (DisplayName + Tooltip de la bolsa nueva) ──────
INSERTOS = {
    'es-MX': ('Bolsa Maestra',
              '''	BolsaAjuarPortador: {
		DisplayName: El Ajuar del Portador
		// Tooltip: ""
	}

'''),
    'es-ES': ('Bolsa Maestra',
              '''	BolsaAjuarPortador: {
		DisplayName: El Ajuar del Portador
		// Tooltip: ""
	}

'''),
    'en-US': ('Bolsa Maestra',
              '''	BolsaAjuarPortador: {
		DisplayName: The Bearer's Trousseau
		// Tooltip: ""
	}

'''),
}
for cultura, (ancla, bloque) in INSERTOS.items():
    p = f'{BASE}/Localization/{cultura}_Mods.AethonMod.hjson'
    with io.open(p, encoding='utf-8') as f:
        s = f.read()
    assert f'\tBolsaMaestra: {{\n\t\tDisplayName: {ancla}\n\t\t// Tooltip: ""\n\t}}\n' in s, \
        f'{cultura}: falta el ancla de BolsaMaestra'
    assert 'BolsaAjuarPortador' not in s, f'{cultura}: ya vive el ajuar'
    s = s.replace(f'\tBolsaMaestra: {{\n\t\tDisplayName: {ancla}\n\t\t// Tooltip: ""\n\t}}\n',
                  f'\tBolsaMaestra: {{\n\t\tDisplayName: {ancla}\n\t\t// Tooltip: ""\n\t}}\n\n' + bloque, 1)
    with io.open(p, 'w', encoding='utf-8') as f:
        f.write(s)
    print(f'  ✔ {cultura}: el ajuar nace en el hjson')

print('CIRUGÍA DEL AJUAR: TODO VIVO')
