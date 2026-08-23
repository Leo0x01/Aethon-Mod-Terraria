"use client";

import { useEffect, useRef, useState } from "react";
import { cn } from "@/lib/utils";

type Status = "idle" | "loading" | "playing" | "paused" | "error";

interface Props {
  text: string;
  label?: string;
  className?: string;
  voice?: string;
  speed?: number;
}

/**
 * A self-contained TTS play/pause button.
 * Fetches a WAV from /api/tts (cached server-side), then plays it via a
 * hidden <audio> element. Handles loading, playing, paused and error states.
 */
export function TtsButton({
  text,
  label = "Narrar",
  className,
  voice = "tongtong",
  speed = 0.95,
}: Props) {
  const [status, setStatus] = useState<Status>("idle");
  const [progress, setProgress] = useState(0);
  const audioRef = useRef<HTMLAudioElement | null>(null);
  const blobUrlRef = useRef<string | null>(null);

  // Clean up any object URL when the component unmounts.
  useEffect(() => {
    return () => {
      if (blobUrlRef.current) {
        URL.revokeObjectURL(blobUrlRef.current);
        blobUrlRef.current = null;
      }
    };
  }, []);

  const fetchAudio = async (): Promise<string | null> => {
    try {
      const res = await fetch("/api/tts", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ text, voice, speed }),
      });
      if (!res.ok) {
        throw new Error(`TTS failed: ${res.status}`);
      }
      const blob = await res.blob();
      if (blobUrlRef.current) URL.revokeObjectURL(blobUrlRef.current);
      const url = URL.createObjectURL(blob);
      blobUrlRef.current = url;
      return url;
    } catch {
      return null;
    }
  };

  const handlePlay = async () => {
    // If already playing, pause.
    if (audioRef.current && status === "playing") {
      audioRef.current.pause();
      setStatus("paused");
      return;
    }
    // If paused, resume.
    if (audioRef.current && status === "paused") {
      audioRef.current.play();
      setStatus("playing");
      return;
    }
    // Otherwise, fetch + play.
    setStatus("loading");
    const url = await fetchAudio();
    if (!url) {
      setStatus("error");
      window.setTimeout(() => setStatus("idle"), 2400);
      return;
    }
    if (!audioRef.current) {
      audioRef.current = new Audio();
      audioRef.current.addEventListener("ended", () => {
        setStatus("idle");
        setProgress(0);
      });
      audioRef.current.addEventListener("timeupdate", () => {
        const a = audioRef.current!;
        if (a.duration > 0) {
          setProgress((a.currentTime / a.duration) * 100);
        }
      });
      audioRef.current.addEventListener("error", () => {
        setStatus("error");
        window.setTimeout(() => setStatus("idle"), 2400);
      });
    }
    audioRef.current.src = url;
    try {
      await audioRef.current.play();
      setStatus("playing");
    } catch {
      setStatus("error");
      window.setTimeout(() => setStatus("idle"), 2400);
    }
  };

  const stop = () => {
    if (audioRef.current) {
      audioRef.current.pause();
      audioRef.current.currentTime = 0;
    }
    setStatus("idle");
    setProgress(0);
  };

  const icon =
    status === "loading"
      ? "◌"
      : status === "playing"
        ? "❚❚"
        : status === "paused"
          ? "▶"
          : status === "error"
            ? "✕"
            : "🔊";

  return (
    <div className={cn("inline-flex items-center gap-2", className)}>
      <button
        onClick={handlePlay}
        disabled={status === "loading"}
        className={cn(
          "group inline-flex items-center gap-1.5 rounded-full border px-2.5 py-1 text-[11px] font-medium transition",
          status === "idle" &&
            "border-border/60 text-muted-foreground hover:border-primary/50 hover:text-primary",
          status === "loading" &&
            "border-primary/40 text-primary opacity-70",
          status === "playing" && "border-primary/60 bg-primary/10 text-primary",
          status === "paused" && "border-primary/40 text-primary",
          status === "error" && "border-destructive/50 text-destructive",
        )}
        aria-label={
          status === "playing" ? "Pausar narración" : "Reproducir narración"
        }
      >
        <span
          className={cn(
            "text-xs",
            status === "loading" && "animate-spin",
          )}
        >
          {icon}
        </span>
        <span className="hidden sm:inline">
          {status === "loading"
            ? "cargando…"
            : status === "playing"
              ? "reproduciendo"
              : status === "paused"
                ? "reanudar"
                : status === "error"
                  ? "error"
                  : label}
        </span>
      </button>
      {/* progress bar */}
      {(status === "playing" || status === "paused") && (
        <div className="hidden h-1 w-16 overflow-hidden rounded-full bg-secondary/60 sm:block">
          <div
            className="h-full bg-primary transition-[width] duration-150"
            style={{ width: `${progress}%` }}
          />
        </div>
      )}
      {(status === "playing" || status === "paused") && (
        <button
          onClick={stop}
          className="rounded-full px-1.5 text-[11px] text-muted-foreground transition hover:text-destructive"
          aria-label="Detener narración"
        >
          ◼
        </button>
      )}
    </div>
  );
}
