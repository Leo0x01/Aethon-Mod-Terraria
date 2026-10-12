#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
sim_libro_v650112.py — LA SIMULACIÓN DEL MUELLE DEL LIBRO FLOTANTE.
Compara el muelle ACTUAL (v6.50.111: k=0.02, d=0.90, SIN tope en CAZA)
contra el candidato LENTO de la v6.50.112, en los escenarios reales:
caminar (3), correr (6.25), esprintar (9), caer (10.5), vagoneta (20),
teletransporte (1500 px de golpe), y el REPOSO tras moverse.
"""

def sim(k, damp, cap_fn, u, T=600, dist0=0.0):
    """v_t = (v_{t-1} + k*d_{t-1})*damp ; pos += v_t ; dest avanza a u px/t."""
    pos = -dist0          # el libro ARRANCA a dist0 px del destino
    v = 0.0
    vmax = 0.0
    lag_final = None
    settle = None         # ticks tras parar (t=300) hasta |d|<8 px y |v|<0.5
    for t in range(1, T + 1):
        u_t = u if t <= 300 else 0.0            # para de moverse a t=300
        dest = u * min(t, 300)                  # el destino SE QUEDA donde paró
        d = dest - pos
        v = (v + k * d) * damp
        cap = cap_fn(abs(d))
        sp = abs(v)
        if sp > cap:
            v *= cap / sp
        pos += v
        d_new = dest - pos
        if t <= 300:
            vmax = max(vmax, abs(v))
            if t >= 200:
                lag_final = abs(d_new)          # lag en régimen
        else:
            if settle is None and abs(d_new) < 8 and abs(v) < 0.5:
                settle = t - 300
            vmax = max(vmax, abs(v))
    return vmax, lag_final, settle

def cap_actual(d):        # SIN tope en MODO_CAZA (la .111)
    return 1e9

def cap_nuevo(d):         # tope = clamp(d*0.01, 10, 20)
    return max(10.0, min(20.0, d * 0.01))

CASOS = [
    ("caminar 3 px/t", 3.0),
    ("correr 6.25 px/t", 6.25),
    ("esprintar 9 px/t", 9.0),
    ("caer 10.5 px/t", 10.5),
    ("vagoneta 20 px/t", 20.0),
]

print(f"{'escenario':22} | {'ACTUAL vmax':>11} {'lag':>6} | {'NUEVO vmax':>10} {'lag':>6} {'settle':>7}")
print("-" * 78)
for nombre, u in CASOS:
    a = sim(0.02, 0.90, cap_actual, u)
    n = sim(0.009, 0.88, cap_nuevo, u)
    lag_a = "—" if a[1] is None else f"{a[1]:.1f}"
    lag_n = "—" if n[1] is None else f"{n[1]:.1f}"
    print(f"{nombre:22} | {a[0]:11.2f} {lag_a:>6} | {n[0]:10.2f} {lag_n:>6} {n[2]!s:>7}")

# EL TELETRANSPORTE: el libro queda a 1500 px del destino de golpe
print("\nTELETRANSPORTE (arranca a 1500 px del destino, jugador quieto):")
for etiqueta, k, damp, cap_fn in [("ACTUAL k=0.02 d=0.90 sin tope", 0.02, 0.90, cap_actual),
                                   ("NUEVO  k=0.009 d=0.88 tope 10-20", 0.009, 0.88, cap_nuevo)]:
    pos = -1500.0; v = 0.0; vmax = 0.0; t_arr = None
    for t in range(1, 1200):
        d = -pos
        v = (v + k * d) * damp
        cap = cap_fn(abs(d)); sp = abs(v)
        if sp > cap: v *= cap / sp
        pos += v
        vmax = max(vmax, abs(v))
        if t_arr is None and abs(pos) < 16: t_arr = t
    print(f"  {etiqueta:34} vmax={vmax:6.1f} px/t   llega (<16 px) en t={t_arr}")

# EL MODO VOLVER (la vuelta a casa tras «saciado»): accel fija, tope fijo
def sim_volver(accel, tope, d0):
    pos = -d0; v = 0.0
    for t in range(1, 1200):
        d = -pos
        rumbo = 1.0 if d > 0 else -1.0
        v += rumbo * accel
        if abs(v) > tope: v = tope * rumbo
        pos += v
        if abs(pos) < 30: return t, abs(v)
    return None, abs(v)

print("\nMODO VOLVER (llega a <30 px del dueño):")
for d0 in (64, 300, 800):
    ta, va = sim_volver(0.90, 22.0, d0)
    tn, vn = sim_volver(0.32, 12.0, d0)
    print(f"  desde {d0:4} px:  ACTUAL accel 0.90 tope 22 → t={ta:3} ({va:4.1f} px/t)   "
          f"NUEVO accel 0.32 tope 12 → t={tn:3} ({vn:4.1f} px/t)")
