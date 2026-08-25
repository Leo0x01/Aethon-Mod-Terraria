import type { Metadata } from "next";
import { Geist, Geist_Mono } from "next/font/google";
import "./globals.css";
import { Toaster } from "@/components/ui/toaster";

const geistSans = Geist({
  variable: "--font-geist-sans",
  subsets: ["latin"],
});

const geistMono = Geist_Mono({
  variable: "--font-geist-mono",
  subsets: ["latin"],
});

export const metadata: Metadata = {
  title: "Aethon, the Primordial Light — Terraria Mod Concept",
  description:
    "A cosmic Terraria mod concept: discover an ancient altar, bond a Genesis Shard that levels infinitely, and grow a procedurally-generated skill tree as you chase the final cosmic entity.",
  keywords: ["Terraria", "mod", "cosmic", "skill tree", "procedural", "tModLoader", "Aethon"],
  authors: [{ name: "Z.ai Code" }],
  icons: {
    icon: "https://z-cdn.chatglm.cn/z-ai/static/logo.svg",
  },
  openGraph: {
    title: "Aethon, the Primordial Light",
    description: "A cosmic Terraria mod concept — infinite leveling, procedural skill trees, a final entity.",
    siteName: "Aethon Mod",
    type: "website",
  },
  twitter: {
    card: "summary_large_image",
    title: "Aethon, the Primordial Light",
    description: "A cosmic Terraria mod concept.",
  },
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html lang="es" suppressHydrationWarning className="dark">
      <body
        className={`${geistSans.variable} ${geistMono.variable} antialiased bg-background text-foreground min-h-screen`}
      >
        {children}
        <Toaster />
      </body>
    </html>
  );
}
