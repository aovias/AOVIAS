'use client';

/**
 * Root-level error boundary UI (must include html/body).
 * Shown when the root layout itself fails — cannot rely on theme CSS loading,
 * so values mirror design-system dark canvas / ink tokens.
 */
/** Mirrored from calibrated dark theme (R1.DS.2.1) — CSS may be unavailable here. */
const DS_DARK_BG = '#090C11';
const DS_DARK_INK = '#EEF1F5';
const DS_DARK_BORDER = '#2E343F';
const DS_DARK_SURFACE = '#161B23';

export default function GlobalError({
  reset,
}: {
  error: Error & { digest?: string };
  reset: () => void;
}) {
  return (
    <html lang="en">
      <body
        style={{
          margin: 0,
          fontFamily:
            'ui-sans-serif, system-ui, -apple-system, Segoe UI, Roboto, sans-serif',
          background: DS_DARK_BG,
          color: DS_DARK_INK,
          minHeight: '100vh',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          padding: '2rem',
        }}
      >
        <main style={{ maxWidth: '28rem', textAlign: 'center' }}>
          <p
            style={{
              letterSpacing: '0.08em',
              textTransform: 'uppercase',
              opacity: 0.7,
            }}
          >
            500
          </p>
          <h1 style={{ fontSize: '1.75rem', margin: '0.75rem 0' }}>
            AOVIAS is temporarily unavailable
          </h1>
          <p style={{ opacity: 0.85, lineHeight: 1.6 }}>
            A critical error occurred. Please try again in a moment.
          </p>
          <button
            type="button"
            onClick={reset}
            style={{
              marginTop: '1.5rem',
              padding: '0.65rem 1.25rem',
              borderRadius: '0.375rem',
              border: `1px solid ${DS_DARK_BORDER}`,
              background: DS_DARK_SURFACE,
              color: DS_DARK_INK,
              cursor: 'pointer',
            }}
          >
            Try again
          </button>
        </main>
      </body>
    </html>
  );
}
