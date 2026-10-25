import { cn } from '@/lib/utils';

export type InstitutionalTrustProps = {
  title?: string;
  className?: string;
};

/**
 * Why AOVIAS exists — faceted shield on classical columns (institutional trust).
 */
export function InstitutionalTrust({
  title = 'AOVIAS institutional trust illustration',
  className,
}: InstitutionalTrustProps) {
  return (
    <div
      role="img"
      aria-label={title}
      className={cn(
        'relative w-full overflow-hidden rounded-2xl',
        'shadow-[0_0_24px_rgba(74,114,173,0.28)]',
        className,
      )}
    >
      <svg
        viewBox="0 0 800 600"
        xmlns="http://www.w3.org/2000/svg"
        className="size-full"
        aria-hidden
      >
        <title>{title}</title>
        <desc>
          A faceted shield containing a network of connected nodes, resting on
          three classical columns, set against a dark navy card with a soft glow
          and grid.
        </desc>
        <defs>
          <linearGradient id="iit-bgGrad" x1="0" y1="0" x2="1" y2="1">
            <stop offset="0" stopColor="#111a2c" />
            <stop offset="1" stopColor="#0a0e17" />
          </linearGradient>
          <linearGradient id="iit-shieldGrad" x1="0" y1="0" x2="0" y2="1">
            <stop offset="0" stopColor="#324566" />
            <stop offset="1" stopColor="#161f31" />
          </linearGradient>
          <radialGradient id="iit-glow1" cx="0.5" cy="0.5" r="0.5">
            <stop offset="0" stopColor="#4a72ad" stopOpacity="0.45" />
            <stop offset="1" stopColor="#4a72ad" stopOpacity="0" />
          </radialGradient>
          <radialGradient id="iit-glow2" cx="0.5" cy="0.5" r="0.5">
            <stop offset="0" stopColor="#3a5583" stopOpacity="0.3" />
            <stop offset="1" stopColor="#3a5583" stopOpacity="0" />
          </radialGradient>
          <radialGradient id="iit-nodeGlow" cx="0.5" cy="0.5" r="0.5">
            <stop offset="0" stopColor="#9fc4f0" stopOpacity="0.55" />
            <stop offset="1" stopColor="#9fc4f0" stopOpacity="0" />
          </radialGradient>
          <pattern
            id="iit-grid"
            width="40"
            height="40"
            patternUnits="userSpaceOnUse"
          >
            <path
              d="M40 0H0V40"
              fill="none"
              stroke="#8fb3e0"
              strokeOpacity="0.05"
              strokeWidth="1"
            />
          </pattern>
          <clipPath id="iit-cardClip">
            <rect x="0" y="0" width="800" height="600" rx="24" />
          </clipPath>
        </defs>

        <g clipPath="url(#iit-cardClip)">
          <rect x="0" y="0" width="800" height="600" fill="url(#iit-bgGrad)" />
          <rect x="0" y="0" width="800" height="600" fill="url(#iit-grid)" />

          <circle cx="620" cy="130" r="210" fill="url(#iit-glow1)" />
          <circle cx="110" cy="500" r="170" fill="url(#iit-glow2)" />

          <ellipse
            cx="400"
            cy="320"
            rx="235"
            ry="192"
            fill="none"
            stroke="#8fb3e0"
            strokeOpacity="0.22"
            strokeWidth="1"
            strokeDasharray="3 7"
          />

          <circle cx="200" cy="215" r="3" fill="#8fb3e0" opacity="0.6" />
          <circle cx="234" cy="422" r="4" fill="#8fb3e0" opacity="0.55" />
          <circle cx="562" cy="228" r="3" fill="#8fb3e0" opacity="0.6" />
          <circle cx="592" cy="404" r="4" fill="#8fb3e0" opacity="0.5" />
          <circle cx="250" cy="322" r="2.5" fill="#8fb3e0" opacity="0.45" />
          <circle cx="556" cy="336" r="3" fill="#8fb3e0" opacity="0.45" />

          <line
            x1="234"
            y1="422"
            x2="340"
            y2="397"
            stroke="#8fb3e0"
            strokeOpacity="0.2"
            strokeWidth="1"
            strokeDasharray="2 5"
          />
          <line
            x1="562"
            y1="228"
            x2="497"
            y2="200"
            stroke="#8fb3e0"
            strokeOpacity="0.2"
            strokeWidth="1"
            strokeDasharray="2 5"
          />

          <rect
            x="326"
            y="470"
            width="34"
            height="8"
            rx="1"
            fill="#26324a"
            stroke="#4a5f82"
            strokeWidth="0.5"
          />
          <rect
            x="333"
            y="478"
            width="14"
            height="64"
            fill="#26324a"
            stroke="#4a5f82"
            strokeWidth="0.5"
          />
          <rect
            x="383"
            y="470"
            width="34"
            height="8"
            rx="1"
            fill="#26324a"
            stroke="#4a5f82"
            strokeWidth="0.5"
          />
          <rect
            x="393"
            y="478"
            width="14"
            height="64"
            fill="#26324a"
            stroke="#4a5f82"
            strokeWidth="0.5"
          />
          <rect
            x="440"
            y="470"
            width="34"
            height="8"
            rx="1"
            fill="#26324a"
            stroke="#4a5f82"
            strokeWidth="0.5"
          />
          <rect
            x="447"
            y="478"
            width="14"
            height="64"
            fill="#26324a"
            stroke="#4a5f82"
            strokeWidth="0.5"
          />
          <rect
            x="300"
            y="542"
            width="200"
            height="12"
            rx="2"
            fill="#222b3e"
            stroke="#3d4d6b"
            strokeWidth="0.5"
          />

          <path
            d="M400,155 L497,193 L497,300 L461,396 L400,469 L339,396 L303,300 L303,193 Z"
            fill="url(#iit-shieldGrad)"
            stroke="#7ea3d1"
            strokeWidth="1.5"
            strokeOpacity="0.7"
          />

          <g stroke="#8fb3e0" strokeWidth="1" strokeOpacity="0.5">
            <line x1="400" y1="225" x2="400" y2="300" />
            <line x1="357" y1="268" x2="400" y2="300" />
            <line x1="443" y1="268" x2="400" y2="300" />
            <line x1="400" y1="300" x2="372" y2="358" />
            <line x1="400" y1="300" x2="428" y2="358" />
            <line x1="357" y1="268" x2="400" y2="225" />
            <line x1="443" y1="268" x2="400" y2="225" />
          </g>

          <circle cx="400" cy="300" r="17" fill="url(#iit-nodeGlow)" />
          <circle cx="400" cy="300" r="7" fill="#bcd7f5" />
          <circle cx="400" cy="225" r="4" fill="#cfe0f5" opacity="0.9" />
          <circle cx="357" cy="268" r="4" fill="#cfe0f5" opacity="0.9" />
          <circle cx="443" cy="268" r="4" fill="#cfe0f5" opacity="0.9" />
          <circle cx="372" cy="358" r="4" fill="#cfe0f5" opacity="0.9" />
          <circle cx="428" cy="358" r="4" fill="#cfe0f5" opacity="0.9" />

          <rect
            x="0.5"
            y="0.5"
            width="799"
            height="599"
            rx="23.5"
            fill="none"
            stroke="#ffffff"
            strokeOpacity="0.08"
          />
        </g>
      </svg>
    </div>
  );
}
