"use client";

import { useEffect, useRef } from "react";

interface Star {
  x: number;
  y: number;
  z: number;
  size: number;
  hue: number;
  twinkle: number;
}

interface ShootingStar {
  x: number;
  y: number;
  vx: number;
  vy: number;
  life: number;
  maxLife: number;
}

/**
 * Canvas-based parallax starfield with drifting stars, nebula glows,
 * and occasional shooting stars. Sits behind the whole page.
 */
export function Starfield() {
  const canvasRef = useRef<HTMLCanvasElement>(null);

  useEffect(() => {
    const canvas = canvasRef.current;
    if (!canvas) return;
    const ctx = canvas.getContext("2d");
    if (!ctx) return;

    let raf = 0;
    let stars: Star[] = [];
    let shooters: ShootingStar[] = [];
    let w = 0;
    let h = 0;

    const dpr = Math.min(window.devicePixelRatio || 1, 2);

    function resize() {
      w = window.innerWidth;
      h = window.innerHeight;
      canvas.width = w * dpr;
      canvas.height = h * dpr;
      canvas.style.width = w + "px";
      canvas.style.height = h + "px";
      ctx.setTransform(dpr, 0, 0, dpr, 0, 0);
      const count = Math.min(220, Math.floor((w * h) / 9000));
      stars = Array.from({ length: count }, () => ({
        x: Math.random() * w,
        y: Math.random() * h,
        z: Math.random() * 0.8 + 0.2,
        size: Math.random() * 1.6 + 0.4,
        hue: Math.random() < 0.6 ? 0 : Math.random() < 0.5 ? 85 : 300,
        twinkle: Math.random() * Math.PI * 2,
      }));
    }

    function spawnShooter() {
      const startX = Math.random() * w;
      const startY = Math.random() * h * 0.5;
      const angle = Math.PI / 4 + (Math.random() - 0.5) * 0.6;
      const speed = 6 + Math.random() * 4;
      shooters.push({
        x: startX,
        y: startY,
        vx: Math.cos(angle) * speed,
        vy: Math.sin(angle) * speed,
        life: 0,
        maxLife: 60 + Math.random() * 40,
      });
    }

    let lastShoot = 0;
    function tick(now: number) {
      ctx.clearRect(0, 0, w, h);

      // nebula glow layers (very subtle)
      const g1 = ctx.createRadialGradient(w * 0.2, h * 0.3, 0, w * 0.2, h * 0.3, w * 0.5);
      g1.addColorStop(0, "rgba(120,80,200,0.10)");
      g1.addColorStop(1, "rgba(0,0,0,0)");
      ctx.fillStyle = g1;
      ctx.fillRect(0, 0, w, h);

      const g2 = ctx.createRadialGradient(w * 0.85, h * 0.7, 0, w * 0.85, h * 0.7, w * 0.45);
      g2.addColorStop(0, "rgba(245,196,81,0.07)");
      g2.addColorStop(1, "rgba(0,0,0,0)");
      ctx.fillStyle = g2;
      ctx.fillRect(0, 0, w, h);

      // stars
      for (const s of stars) {
        s.twinkle += 0.02 * s.z;
        const tw = 0.5 + Math.sin(s.twinkle) * 0.5;
        const alpha = (0.35 + tw * 0.6) * s.z;
        const color =
          s.hue === 0
            ? `rgba(255,255,255,${alpha})`
            : s.hue === 85
              ? `rgba(245,196,81,${alpha})`
              : `rgba(179,136,255,${alpha})`;
        ctx.beginPath();
        ctx.fillStyle = color;
        ctx.arc(s.x, s.y, s.size * s.z, 0, Math.PI * 2);
        ctx.fill();
        // drift
        s.y += 0.05 * s.z;
        if (s.y > h) {
          s.y = 0;
          s.x = Math.random() * w;
        }
      }

      // shooting stars
      if (now - lastShoot > 4500 && Math.random() < 0.04) {
        spawnShooter();
        lastShoot = now;
      }
      shooters = shooters.filter((sh) => sh.life < sh.maxLife);
      for (const sh of shooters) {
        sh.life++;
        sh.x += sh.vx;
        sh.y += sh.vy;
        const t = sh.life / sh.maxLife;
        const a = Math.sin(t * Math.PI);
        const grad = ctx.createLinearGradient(
          sh.x - sh.vx * 6,
          sh.y - sh.vy * 6,
          sh.x,
          sh.y,
        );
        grad.addColorStop(0, "rgba(245,196,81,0)");
        grad.addColorStop(1, `rgba(255,240,200,${a * 0.9})`);
        ctx.strokeStyle = grad;
        ctx.lineWidth = 2;
        ctx.beginPath();
        ctx.moveTo(sh.x - sh.vx * 6, sh.y - sh.vy * 6);
        ctx.lineTo(sh.x, sh.y);
        ctx.stroke();
      }

      raf = requestAnimationFrame(tick);
    }

    resize();
    window.addEventListener("resize", resize);
    raf = requestAnimationFrame(tick);

    return () => {
      cancelAnimationFrame(raf);
      window.removeEventListener("resize", resize);
    };
  }, []);

  return (
    <canvas
      ref={canvasRef}
      aria-hidden
      className="pointer-events-none fixed inset-0 z-0 h-full w-full"
    />
  );
}
