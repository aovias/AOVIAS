import { ImageResponse } from 'next/og';
import { readFile } from 'node:fs/promises';
import path from 'node:path';

export const size = { width: 32, height: 32 };
export const contentType = 'image/png';

/** Brand mark favicon from the R1.3 asset library. */
export default async function Icon() {
  try {
    const file = await readFile(
      path.join(process.cwd(), 'public/icons/icon-32.png'),
    );
    return new Response(file, {
      headers: { 'Content-Type': 'image/png', 'Cache-Control': 'public, max-age=86400' },
    });
  } catch {
    return new ImageResponse(
      (
        <div
          style={{
            width: '100%',
            height: '100%',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            background: '#0A1520',
            color: '#F6F7F9',
            fontSize: 16,
            fontWeight: 700,
          }}
        >
          A
        </div>
      ),
      { ...size },
    );
  }
}
