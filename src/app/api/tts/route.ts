import { NextRequest, NextResponse } from "next/server";

// In-memory LRU cache of generated TTS audio so repeat narrations are instant
// and don't re-hit the (rate-limited) upstream API. Key = sha-ish of input.
const cache = new Map<string, Buffer>();
const CACHE_MAX = 64;

export async function POST(req: NextRequest) {
  try {
    const body = await req.json();
    const text = typeof body?.text === "string" ? body.text.trim() : "";
    const voice =
      typeof body?.voice === "string" ? body.voice : "tongtong";
    const speed =
      typeof body?.speed === "number" && body.speed >= 0.5 && body.speed <= 2
        ? body.speed
        : 0.95;

    if (!text) {
      return NextResponse.json(
        { error: "text is required" },
        { status: 400 },
      );
    }
    if (text.length > 1024) {
      return NextResponse.json(
        { error: "text exceeds 1024 characters" },
        { status: 413 },
      );
    }

    const cacheKey = `${voice}:${speed}:${text}`;
    const cached = cache.get(cacheKey);
    if (cached) {
      return new NextResponse(cached, {
        status: 200,
        headers: {
          "Content-Type": "audio/wav",
          "Content-Length": cached.length.toString(),
          "Cache-Control": "public, max-age=86400",
          "X-TTS-Cache": "HIT",
        },
      });
    }

    const ZAI = (await import("z-ai-web-dev-sdk")).default;
    const zai = await ZAI.create();

    const response = await zai.audio.tts.create({
      input: text,
      voice,
      speed,
      response_format: "wav",
      stream: false,
    });

    const arrayBuffer = await response.arrayBuffer();
    const buffer = Buffer.from(new Uint8Array(arrayBuffer));

    // LRU eviction
    if (cache.size >= CACHE_MAX) {
      const firstKey = cache.keys().next().value;
      if (firstKey) cache.delete(firstKey);
    }
    cache.set(cacheKey, buffer);

    return new NextResponse(buffer, {
      status: 200,
      headers: {
        "Content-Type": "audio/wav",
        "Content-Length": buffer.length.toString(),
        "Cache-Control": "public, max-age=86400",
        "X-TTS-Cache": "MISS",
      },
    });
  } catch (error) {
    console.error("[/api/tts] error:", error);
    return NextResponse.json(
      {
        error: error instanceof Error ? error.message : "TTS failed",
      },
      { status: 500 },
    );
  }
}
