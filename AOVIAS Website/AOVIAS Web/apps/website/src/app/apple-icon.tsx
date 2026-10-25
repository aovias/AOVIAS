import { readFile } from 'node:fs/promises';
import path from 'node:path';

export const size = { width: 180, height: 180 };
export const contentType = 'image/png';

/** Apple touch icon from the R1.3 brand asset library. */
export default async function AppleIcon() {
  const file = await readFile(
    path.join(process.cwd(), 'public/icons/icon-180.png'),
  );
  return new Response(file, {
    headers: { 'Content-Type': 'image/png', 'Cache-Control': 'public, max-age=86400' },
  });
}
