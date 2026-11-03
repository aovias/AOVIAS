import type { Metadata } from "next";
import "./globals.css";

export const metadata: Metadata = {
  title: "AccredaOne",
  description:
    "Accreditation knowledge discovery and explanation platform. Engineering foundation only.",
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html lang="en">
      <body>{children}</body>
    </html>
  );
}
