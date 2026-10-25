/** Lightweight ID helpers — OTel-compatible hex shapes, no crypto dependency. */

function randomHex(bytes: number): string {
  const parts: string[] = [];
  for (let i = 0; i < bytes; i += 1) {
    parts.push(Math.floor(Math.random() * 256).toString(16).padStart(2, '0'));
  }
  return parts.join('');
}

/** 16-byte trace id (32 hex chars). */
export function createTraceId(): string {
  return randomHex(16);
}

/** 8-byte span id (16 hex chars). */
export function createSpanId(): string {
  return randomHex(8);
}

export function createEventId(): string {
  return `evt_${randomHex(8)}`;
}
