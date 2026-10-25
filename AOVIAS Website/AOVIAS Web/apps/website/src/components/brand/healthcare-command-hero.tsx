import type { ReactNode } from 'react';

import { cn } from '@/lib/utils';

export type HealthcareCommandHeroProps = {
  title?: string;
  className?: string;
};

const FONT =
  'ui-sans-serif, system-ui, -apple-system, "Segoe UI", sans-serif';

/**
 * Healthcare solutions hero — AOVIAS Command as the operational command center
 * for modern hospitals: premium laptop dashboard, living data pathways,
 * contextual glass modules, and mobile workflows.
 */
export function HealthcareCommandHero({
  title = 'AOVIAS Command — hospital operational command center',
  className,
}: HealthcareCommandHeroProps) {
  return (
    <div
      role="img"
      aria-label={title}
      className={cn(
        'relative w-full overflow-hidden rounded-2xl',
        'shadow-[0_0_40px_rgba(58,106,158,0.22)]',
        'border border-[#2a3a55]/55',
        className,
      )}
    >
      <svg
        viewBox="0 0 760 580"
        xmlns="http://www.w3.org/2000/svg"
        className="size-full"
        aria-hidden
      >
        <title>{title}</title>
        <desc>
          Premium AOVIAS Command executive dashboard on a laptop, connected by
          glowing operational pathways to contextual modules for evidence,
          compliance, corrective actions, department performance, audit
          readiness, policies, and tasks, with a mobile My Tasks device in the
          foreground — communicating hospital-wide governance and continuous
          readiness.
        </desc>

        <defs>
          {/* Atmosphere */}
          <linearGradient id="hch-bg" x1="0.15" y1="0" x2="0.85" y2="1">
            <stop offset="0" stopColor="#121a2b" />
            <stop offset="0.45" stopColor="#0b111c" />
            <stop offset="1" stopColor="#070b12" />
          </linearGradient>
          <radialGradient id="hch-glow-core" cx="0.48" cy="0.4" r="0.5">
            <stop offset="0" stopColor="#3d6fa8" stopOpacity="0.32" />
            <stop offset="0.45" stopColor="#1e3a5c" stopOpacity="0.14" />
            <stop offset="1" stopColor="#070b12" stopOpacity="0" />
          </radialGradient>
          <radialGradient id="hch-glow-tl" cx="0.2" cy="0.15" r="0.35">
            <stop offset="0" stopColor="#2a5080" stopOpacity="0.18" />
            <stop offset="1" stopColor="#070b12" stopOpacity="0" />
          </radialGradient>
          <radialGradient id="hch-glow-br" cx="0.82" cy="0.78" r="0.35">
            <stop offset="0" stopColor="#1a4060" stopOpacity="0.16" />
            <stop offset="1" stopColor="#070b12" stopOpacity="0" />
          </radialGradient>
          <pattern
            id="hch-grid"
            width="28"
            height="28"
            patternUnits="userSpaceOnUse"
          >
            <path
              d="M28 0H0V28"
              fill="none"
              stroke="#8fb3e0"
              strokeOpacity="0.07"
              strokeWidth="0.75"
            />
          </pattern>
          <pattern
            id="hch-grid-fine"
            width="7"
            height="7"
            patternUnits="userSpaceOnUse"
          >
            <circle cx="0.5" cy="0.5" r="0.35" fill="#8fb3e0" fillOpacity="0.04" />
          </pattern>

          {/* Hardware */}
          <linearGradient id="hch-bezel" x1="0" y1="0" x2="0" y2="1">
            <stop offset="0" stopColor="#3a465c" />
            <stop offset="0.4" stopColor="#252e40" />
            <stop offset="1" stopColor="#161c28" />
          </linearGradient>
          <linearGradient id="hch-bezel-edge" x1="0" y1="0" x2="1" y2="0">
            <stop offset="0" stopColor="#5a6a82" stopOpacity="0.35" />
            <stop offset="0.5" stopColor="#8a9bb3" stopOpacity="0.15" />
            <stop offset="1" stopColor="#5a6a82" stopOpacity="0.35" />
          </linearGradient>
          <linearGradient id="hch-screen" x1="0" y1="0" x2="0" y2="1">
            <stop offset="0" stopColor="#141c2c" />
            <stop offset="1" stopColor="#0c121e" />
          </linearGradient>
          <linearGradient id="hch-screen-shine" x1="0" y1="0" x2="0.4" y2="1">
            <stop offset="0" stopColor="#ffffff" stopOpacity="0.06" />
            <stop offset="0.35" stopColor="#ffffff" stopOpacity="0.015" />
            <stop offset="1" stopColor="#ffffff" stopOpacity="0" />
          </linearGradient>
          <linearGradient id="hch-base" x1="0" y1="0" x2="0" y2="1">
            <stop offset="0" stopColor="#2a3448" />
            <stop offset="1" stopColor="#121820" />
          </linearGradient>
          <linearGradient id="hch-base-top" x1="0" y1="0" x2="0" y2="1">
            <stop offset="0" stopColor="#3a465c" />
            <stop offset="1" stopColor="#1e2636" />
          </linearGradient>

          {/* Glass panels */}
          <linearGradient id="hch-glass" x1="0" y1="0" x2="0" y2="1">
            <stop offset="0" stopColor="#2a3a52" stopOpacity="0.82" />
            <stop offset="1" stopColor="#121a28" stopOpacity="0.88" />
          </linearGradient>
          <linearGradient id="hch-glass-shine" x1="0" y1="0" x2="0" y2="1">
            <stop offset="0" stopColor="#ffffff" stopOpacity="0.08" />
            <stop offset="0.4" stopColor="#ffffff" stopOpacity="0" />
          </linearGradient>
          <linearGradient id="hch-phone-shell" x1="0" y1="0" x2="0" y2="1">
            <stop offset="0" stopColor="#2a3448" />
            <stop offset="1" stopColor="#121820" />
          </linearGradient>
          <linearGradient id="hch-phone-screen" x1="0" y1="0" x2="0" y2="1">
            <stop offset="0" stopColor="#151e2e" />
            <stop offset="1" stopColor="#0c121e" />
          </linearGradient>

          {/* Flows */}
          <linearGradient id="hch-flow-a" x1="0" y1="0" x2="1" y2="1">
            <stop offset="0" stopColor="#67e8f9" stopOpacity="0" />
            <stop offset="0.3" stopColor="#5a8fd0" stopOpacity="0.7" />
            <stop offset="0.7" stopColor="#7eb8e8" stopOpacity="0.45" />
            <stop offset="1" stopColor="#67e8f9" stopOpacity="0" />
          </linearGradient>
          <linearGradient id="hch-flow-b" x1="1" y1="0" x2="0" y2="1">
            <stop offset="0" stopColor="#5a8fd0" stopOpacity="0" />
            <stop offset="0.4" stopColor="#67e8f9" stopOpacity="0.55" />
            <stop offset="1" stopColor="#5a8fd0" stopOpacity="0" />
          </linearGradient>

          {/* Charts */}
          <linearGradient id="hch-area" x1="0" y1="1" x2="0" y2="0">
            <stop offset="0" stopColor="#5a8fd0" stopOpacity="0" />
            <stop offset="1" stopColor="#5a8fd0" stopOpacity="0.28" />
          </linearGradient>
          <linearGradient id="hch-donut" x1="0" y1="0" x2="1" y2="1">
            <stop offset="0" stopColor="#67e8f9" />
            <stop offset="1" stopColor="#5a8fd0" />
          </linearGradient>

          <filter id="hch-glow" x="-50%" y="-50%" width="200%" height="200%">
            <feGaussianBlur stdDeviation="2.8" result="b" />
            <feMerge>
              <feMergeNode in="b" />
              <feMergeNode in="SourceGraphic" />
            </feMerge>
          </filter>
          <filter id="hch-glow-soft" x="-60%" y="-60%" width="220%" height="220%">
            <feGaussianBlur stdDeviation="5" result="b" />
            <feMerge>
              <feMergeNode in="b" />
              <feMergeNode in="SourceGraphic" />
            </feMerge>
          </filter>
          <filter id="hch-shadow" x="-25%" y="-25%" width="150%" height="160%">
            <feDropShadow
              dx="0"
              dy="10"
              stdDeviation="14"
              floodColor="#02050a"
              floodOpacity="0.65"
            />
          </filter>
          <filter id="hch-panel-shadow" x="-20%" y="-20%" width="140%" height="150%">
            <feDropShadow
              dx="0"
              dy="5"
              stdDeviation="8"
              floodColor="#040a14"
              floodOpacity="0.55"
            />
            <feDropShadow
              dx="0"
              dy="0"
              stdDeviation="4"
              floodColor="#3a6a9e"
              floodOpacity="0.18"
            />
          </filter>
          <filter id="hch-phone-shadow" x="-30%" y="-20%" width="160%" height="150%">
            <feDropShadow
              dx="4"
              dy="12"
              stdDeviation="12"
              floodColor="#02050a"
              floodOpacity="0.7"
            />
          </filter>

          <clipPath id="hch-frame">
            <rect x="0" y="0" width="760" height="580" rx="20" />
          </clipPath>
          <clipPath id="hch-screen-clip">
            <rect x="196" y="118" width="348" height="228" rx="3" />
          </clipPath>
          <clipPath id="hch-phone-clip">
            <rect x="8" y="14" width="96" height="158" rx="6" />
          </clipPath>
        </defs>

        <g clipPath="url(#hch-frame)">
          {/* ===== BACKGROUND ===== */}
          <rect width="760" height="580" fill="url(#hch-bg)" />
          <rect width="760" height="580" fill="url(#hch-grid)" />
          <rect width="760" height="580" fill="url(#hch-grid-fine)" />
          <ellipse cx="360" cy="250" rx="320" ry="230" fill="url(#hch-glow-core)" />
          <ellipse cx="120" cy="80" rx="180" ry="140" fill="url(#hch-glow-tl)" />
          <ellipse cx="640" cy="460" rx="200" ry="150" fill="url(#hch-glow-br)" />

          {/* Abstract workflow paths — background layer */}
          <g
            fill="none"
            stroke="#4a72ad"
            strokeOpacity="0.08"
            strokeWidth="1"
            strokeDasharray="3 6"
          >
            <path
              d="M40 200 C 120 160, 180 220, 240 180"
              strokeOpacity="0.12"
            />
            <path
              d="M520 120 C 580 160, 640 140, 720 200"
              strokeOpacity="0.12"
            />
            <path
              d="M60 420 C 140 380, 200 450, 280 410"
              strokeOpacity="0.1"
            />
            <path
              d="M480 480 C 560 440, 620 500, 700 460"
              strokeOpacity="0.1"
            />
          </g>

          {/* Tiny data particles */}
          <g fill="#7eb8e8" className="hch-flow-pulse-delay">
            {[
              [90, 120, 1.2],
              [160, 90, 0.9],
              [680, 100, 1.1],
              [700, 280, 0.8],
              [80, 300, 1],
              [50, 480, 0.9],
              [720, 400, 1],
              [200, 520, 0.8],
              [400, 40, 1],
              [500, 530, 0.9],
            ].map(([x, y, r], i) => (
              <circle
                key={i}
                cx={x}
                cy={y}
                r={r}
                opacity={0.25 + (i % 3) * 0.08}
              />
            ))}
          </g>

          {/* Soft ground plane / architectural light */}
          <ellipse
            cx="380"
            cy="430"
            rx="260"
            ry="48"
            fill="#3a6a9e"
            opacity="0.07"
          />
          <ellipse
            cx="380"
            cy="438"
            rx="200"
            ry="28"
            fill="#02050a"
            opacity="0.45"
          />

          {/* ===== CONNECTION PATHWAYS (midground, behind devices) ===== */}
          {/* Glow underlay */}
          <g className="hch-flow-pulse" filter="url(#hch-glow-soft)" opacity="0.55">
            <path
              d="M280 200 C 200 175, 150 140, 118 108"
              fill="none"
              stroke="#5a8fd0"
              strokeWidth="4"
              strokeLinecap="round"
            />
            <path
              d="M380 145 C 380 100, 360 70, 345 52"
              fill="none"
              stroke="#5a8fd0"
              strokeWidth="3.5"
              strokeLinecap="round"
            />
            <path
              d="M480 190 C 560 160, 600 130, 638 98"
              fill="none"
              stroke="#5a8fd0"
              strokeWidth="4"
              strokeLinecap="round"
            />
            <path
              d="M250 270 C 160 290, 120 330, 98 370"
              fill="none"
              stroke="#5a8fd0"
              strokeWidth="3.5"
              strokeLinecap="round"
            />
            <path
              d="M510 260 C 590 275, 630 310, 652 348"
              fill="none"
              stroke="#5a8fd0"
              strokeWidth="3.5"
              strokeLinecap="round"
            />
            <path
              d="M300 340 C 240 380, 180 420, 145 455"
              fill="none"
              stroke="#5a8fd0"
              strokeWidth="3.2"
              strokeLinecap="round"
            />
            <path
              d="M460 340 C 520 380, 560 420, 590 455"
              fill="none"
              stroke="#5a8fd0"
              strokeWidth="3.2"
              strokeLinecap="round"
            />
            <path
              d="M500 320 C 540 350, 570 390, 585 430"
              fill="none"
              stroke="#5a8fd0"
              strokeWidth="3"
              strokeLinecap="round"
            />
          </g>
          {/* Core pathway strokes */}
          <g className="hch-flow-pulse" filter="url(#hch-glow)">
            <path
              d="M280 200 C 200 175, 150 140, 118 108"
              fill="none"
              stroke="url(#hch-flow-a)"
              strokeWidth="1.5"
              strokeLinecap="round"
              opacity="0.95"
            />
            <path
              d="M380 145 C 380 100, 360 70, 345 52"
              fill="none"
              stroke="url(#hch-flow-b)"
              strokeWidth="1.35"
              strokeLinecap="round"
              opacity="0.9"
            />
            <path
              d="M480 190 C 560 160, 600 130, 638 98"
              fill="none"
              stroke="url(#hch-flow-a)"
              strokeWidth="1.5"
              strokeLinecap="round"
              opacity="0.95"
            />
            <path
              d="M250 270 C 160 290, 120 330, 98 370"
              fill="none"
              stroke="url(#hch-flow-b)"
              strokeWidth="1.4"
              strokeLinecap="round"
              opacity="0.85"
            />
            <path
              d="M510 260 C 590 275, 630 310, 652 348"
              fill="none"
              stroke="url(#hch-flow-a)"
              strokeWidth="1.4"
              strokeLinecap="round"
              opacity="0.9"
            />
            <path
              d="M300 340 C 240 380, 180 420, 145 455"
              fill="none"
              stroke="url(#hch-flow-b)"
              strokeWidth="1.3"
              strokeLinecap="round"
              opacity="0.8"
            />
            <path
              d="M460 340 C 520 380, 560 420, 590 455"
              fill="none"
              stroke="url(#hch-flow-a)"
              strokeWidth="1.3"
              strokeLinecap="round"
              opacity="0.8"
            />
            <path
              d="M500 320 C 540 350, 570 390, 585 430"
              fill="none"
              stroke="url(#hch-flow-b)"
              strokeWidth="1.25"
              strokeLinecap="round"
              opacity="0.75"
            />
          </g>

          {/* Pathway nodes */}
          <g className="hch-flow-pulse-delay" filter="url(#hch-glow)">
            {[
              [118, 108],
              [345, 52],
              [638, 98],
              [98, 370],
              [652, 348],
              [145, 455],
              [590, 455],
              [280, 200],
              [480, 190],
              [370, 340],
            ].map(([cx, cy], i) => (
              <g key={i}>
                <circle cx={cx} cy={cy} r="4.5" fill="#5a8fd0" opacity="0.2" />
                <circle cx={cx} cy={cy} r="2.2" fill="#9fc4f0" opacity="0.85" />
              </g>
            ))}
          </g>

          {/* Traveling pulse dots along pathways */}
          <g className="hch-flow-pulse" filter="url(#hch-glow)">
            <circle cx="200" cy="165" r="2" fill="#67e8f9" opacity="0.7" />
            <circle cx="560" cy="145" r="2" fill="#7eb8e8" opacity="0.65" />
            <circle cx="180" cy="310" r="1.8" fill="#67e8f9" opacity="0.55" />
            <circle cx="580" cy="300" r="1.8" fill="#5a8fd0" opacity="0.6" />
            <circle cx="220" cy="400" r="1.6" fill="#7eb8e8" opacity="0.5" />
          </g>

          {/* ===== FLOATING MODULES ===== */}
          <g filter="url(#hch-panel-shadow)">
            {/* Evidence Repository — top left */}
            <ModulePanel x={28} y={42} w={168} h={92}>
              <IconFolder x={14} y={14} />
              <text x={36} y={26} fill="#e8eef8" fontSize="9" fontFamily={FONT} fontWeight="600">
                Evidence Repository
              </text>
              <MetricRow x={14} y={42} label="Indexed" value="12,840" valueX={154} />
              <MetricRow x={14} y={58} label="Last updated" value="14m ago" muted valueX={154} />
              <MetricRow x={14} y={74} label="Search" value="Ready" tone="good" valueX={154} />
            </ModulePanel>

            {/* Audit Readiness — top center */}
            <ModulePanel x={290} y={18} w={156} h={78}>
              <IconShield x={14} y={12} />
              <text x={36} y={24} fill="#e8eef8" fontSize="9" fontFamily={FONT} fontWeight="600">
                Audit Readiness
              </text>
              <text x={14} y={48} fill="#d0daf0" fontSize="16" fontFamily={FONT} fontWeight="600">
                94%
              </text>
              <text x={52} y={48} fill="#6bcea8" fontSize="8" fontFamily={FONT}>
                ↑ 2.1%
              </text>
              <text x={14} y={64} fill="#7a8aa3" fontSize="7" fontFamily={FONT}>
                Continuous posture · stable
              </text>
            </ModulePanel>

            {/* Hospital Compliance — top right */}
            <ModulePanel x={560} y={38} w={168} h={92}>
              <IconRing x={14} y={14} />
              <text x={36} y={26} fill="#e8eef8" fontSize="9" fontFamily={FONT} fontWeight="600">
                Hospital Compliance
              </text>
              <DonutMini cx={28} cy={62} r={16} pct={0.94} />
              <MetricRow x={52} y={48} label="Controls mapped" value="94%" valueX={154} />
              <MetricRow x={52} y={64} label="Open gaps" value="6" muted valueX={154} />
              <MetricRow x={52} y={80} label="Reviewed" value="This week" muted valueX={154} />
            </ModulePanel>

            {/* CAPA — mid left */}
            <ModulePanel x={22} y={320} w={152} h={96}>
              <IconList x={14} y={14} />
              <text x={36} y={26} fill="#e8eef8" fontSize="9" fontFamily={FONT} fontWeight="600">
                Corrective Actions
              </text>
              <MetricRow x={14} y={44} label="Open" value="7" valueX={138} />
              <MetricRow x={14} y={60} label="Due today" value="2" tone="warn" valueX={138} />
              <MetricRow x={14} y={76} label="Overdue" value="1" tone="danger" valueX={138} />
            </ModulePanel>

            {/* Policies — mid right */}
            <ModulePanel x={580} y={180} w={148} h={88}>
              <IconDoc x={14} y={14} />
              <text x={36} y={26} fill="#e8eef8" fontSize="9" fontFamily={FONT} fontWeight="600">
                Policy Management
              </text>
              <MetricRow x={14} y={44} label="Active" value="41" valueX={134} />
              <MetricRow x={14} y={60} label="In review" value="5" muted valueX={134} />
              <MetricRow x={14} y={76} label="Approvals" value="3 pending" tone="warn" valueX={134} />
            </ModulePanel>

            {/* Dept Performance — bottom left */}
            <ModulePanel x={58} y={450} w={172} h={100}>
              <IconBars x={14} y={12} />
              <text x={36} y={24} fill="#e8eef8" fontSize="9" fontFamily={FONT} fontWeight="600">
                Department Performance
              </text>
              <MiniBar x={14} y={40} label="Nursing" pct={0.9} w={100} />
              <MiniBar x={14} y={54} label="ICU" pct={0.84} w={100} />
              <MiniBar x={14} y={68} label="Laboratory" pct={0.78} w={100} />
              <MiniBar x={14} y={82} label="Pharmacy" pct={0.86} w={100} />
            </ModulePanel>

            {/* Tasks — bottom mid-right */}
            <ModulePanel x={420} y={458} w={140} h={88}>
              <IconCheck x={14} y={14} />
              <text x={36} y={26} fill="#e8eef8" fontSize="9" fontFamily={FONT} fontWeight="600">
                Task Assignment
              </text>
              <MetricRow x={14} y={44} label="Open" value="128" valueX={126} />
              <MetricRow x={14} y={60} label="Completed" value="36 today" tone="good" valueX={126} />
              <MetricRow x={14} y={76} label="Unassigned" value="4" muted valueX={126} />
            </ModulePanel>

            {/* Executive Notifications — over laptop base */}
            <ModulePanel x={248} y={392} w={196} h={78}>
              <IconBell x={14} y={14} />
              <text x={36} y={26} fill="#e8eef8" fontSize="9" fontFamily={FONT} fontWeight="600">
                Executive Notifications
              </text>
              <NotifLine x={14} y={42} text="Policy approval · QMS-12" tone="warn" />
              <NotifLine x={14} y={56} text="CAPA-441 assigned · ICU" />
              <NotifLine x={14} y={70} text="Internal audit due · 9d" muted />
            </ModulePanel>
          </g>

          {/* ===== LAPTOP (hero) — slight depth via scale + lift ===== */}
          <g filter="url(#hch-shadow)" transform="translate(380 255) scale(1.06) translate(-380 -255)">
            {/* Lid rear edge for depth */}
            <rect
              x="190"
              y="104"
              width="360"
              height="8"
              rx="3"
              fill="#1a2230"
              opacity="0.9"
            />
            {/* Perspective base */}
            <path
              d="M158 376 L602 376 L624 410 L136 410 Z"
              fill="url(#hch-base)"
            />
            <path
              d="M158 376 L602 376 L596 384 L164 384 Z"
              fill="url(#hch-base-top)"
            />
            {/* Base specular */}
            <path
              d="M200 378 L560 378 L556 381 L204 381 Z"
              fill="#8a9bb3"
              opacity="0.12"
            />
            <rect
              x="136"
              y="408"
              width="488"
              height="14"
              rx="2"
              fill="#0a1018"
              stroke="#2a3548"
              strokeWidth="0.6"
            />
            {/* Trackpad */}
            <rect
              x="328"
              y="388"
              width="84"
              height="14"
              rx="2.5"
              fill="#1a2230"
              stroke="#2e3c50"
              strokeWidth="0.55"
              opacity="0.85"
            />

            {/* Lid — thin bezel */}
            <rect
              x="186"
              y="106"
              width="368"
              height="264"
              rx="13"
              fill="url(#hch-bezel)"
              stroke="#5a6a82"
              strokeWidth="1.35"
            />
            {/* Inner bezel ring */}
            <rect
              x="192"
              y="112"
              width="356"
              height="244"
              rx="6"
              fill="none"
              stroke="#0a0e14"
              strokeWidth="2.5"
              opacity="0.55"
            />
            {/* Top bezel highlight */}
            <rect
              x="186"
              y="106"
              width="368"
              height="1.75"
              rx="1"
              fill="url(#hch-bezel-edge)"
            />
            {/* Side edge catchlight */}
            <rect
              x="186"
              y="106"
              width="1.25"
              height="264"
              fill="#9fc4f0"
              opacity="0.12"
            />
            {/* Camera */}
            <circle cx="370" cy="111" r="2.4" fill="#060a10" />
            <circle cx="370" cy="111" r="1" fill="#3a4a60" />

            {/* Screen */}
            <rect
              x="196"
              y="118"
              width="348"
              height="228"
              rx="3"
              fill="url(#hch-screen)"
            />

            {/* Dashboard UI */}
            <g clipPath="url(#hch-screen-clip)">
              {/* Sidebar */}
              <rect x="196" y="118" width="40" height="228" fill="#0a1018" />
              <rect x="204" y="128" width="24" height="14" rx="3" fill="#5a8fd0" opacity="0.85" />
              <text
                x="208"
                y="138"
                fill="#e8eef8"
                fontSize="7"
                fontFamily={FONT}
                fontWeight="700"
              >
                AO
              </text>
              {[152, 172, 192, 212, 232, 252, 272, 292].map((y, i) => (
                <g key={y}>
                  <rect
                    x="206"
                    y={y}
                    width="20"
                    height="14"
                    rx="3"
                    fill={i === 0 ? '#1e3a5c' : '#121a28'}
                    stroke={i === 0 ? '#5a8fd0' : 'transparent'}
                    strokeWidth="0.75"
                    opacity={i === 0 ? 1 : 0.85}
                  />
                  <rect
                    x="211"
                    y={y + 4}
                    width="10"
                    height="6"
                    rx="1"
                    fill={i === 0 ? '#7eb8e8' : '#3a4a60'}
                    opacity="0.7"
                  />
                </g>
              ))}

              {/* Top bar */}
              <rect x="236" y="118" width="308" height="28" fill="#0e1520" />
              <text
                x="248"
                y="136"
                fill="#e8eef8"
                fontSize="10"
                fontFamily={FONT}
                fontWeight="600"
              >
                AOVIAS Command
              </text>
              <rect
                x="380"
                y="126"
                width="90"
                height="14"
                rx="3"
                fill="#121a28"
                stroke="#243044"
                strokeWidth="0.6"
              />
              <text x="388" y="136" fill="#5a7088" fontSize="6" fontFamily={FONT}>
                Search workspace
              </text>
              <circle
                cx="520"
                cy="132"
                r="8"
                fill="#1a2538"
                stroke="#3a4a63"
                strokeWidth="0.6"
              />
              <text
                x="514"
                y="135"
                fill="#9fc4f0"
                fontSize="6"
                fontFamily={FONT}
                fontWeight="600"
              >
                AD
              </text>

              {/* Title row */}
              <text
                x="248"
                y="162"
                fill="#c8d4e8"
                fontSize="8"
                fontFamily={FONT}
                fontWeight="600"
              >
                Executive Overview
              </text>
              <text x="248" y="172" fill="#5a7088" fontSize="6" fontFamily={FONT}>
                Multi-site · Live
              </text>

              {/* KPI strip */}
              <KpiCard x={248} y={180} w={68} label="Compliance" value="94%" tone="good" />
              <KpiCard x={322} y={180} w={68} label="Open tasks" value="128" />
              <KpiCard x={396} y={180} w={68} label="CAPA open" value="7" tone="warn" />
              <KpiCard x={470} y={180} w={62} label="Depts active" value="24" />

              {/* Compliance trend */}
              <rect
                x="248"
                y="218"
                width="148"
                height="78"
                rx="4"
                fill="#0e1520"
                stroke="#1e2a3c"
                strokeWidth="0.75"
              />
              <text x="256" y="232" fill="#7a8aa3" fontSize="6.5" fontFamily={FONT}>
                Compliance timeline
              </text>
              <path
                d="M256 278 L276 272 L296 275 L316 262 L336 266 L356 252 L376 256 L386 248"
                fill="none"
                stroke="#5a8fd0"
                strokeWidth="1.6"
                strokeLinecap="round"
                strokeLinejoin="round"
              />
              <path
                d="M256 278 L276 272 L296 275 L316 262 L336 266 L356 252 L376 256 L386 248 L386 286 L256 286 Z"
                fill="url(#hch-area)"
              />
              <text x="256" y="290" fill="#4a5a70" fontSize="5" fontFamily={FONT}>
                Jan · Feb · Mar · Apr · May · Jun
              </text>

              {/* Department bars */}
              <rect
                x="404"
                y="218"
                width="128"
                height="78"
                rx="4"
                fill="#0e1520"
                stroke="#1e2a3c"
                strokeWidth="0.75"
              />
              <text x="412" y="232" fill="#7a8aa3" fontSize="6.5" fontFamily={FONT}>
                Department performance
              </text>
              <DashBar x={412} y={242} label="Nursing" pct={0.9} w={88} />
              <DashBar x={412} y={256} label="ICU" pct={0.84} w={88} />
              <DashBar x={412} y={270} label="Lab" pct={0.78} w={88} />
              <DashBar x={412} y={284} label="Pharmacy" pct={0.86} w={88} />

              {/* Recent activity + readiness */}
              <rect
                x="248"
                y="304"
                width="200"
                height="36"
                rx="3"
                fill="#0e1520"
                stroke="#1e2a3c"
                strokeWidth="0.6"
              />
              <text x="256" y="316" fill="#7a8aa3" fontSize="5.5" fontFamily={FONT}>
                Recent activities
              </text>
              <text x="256" y="328" fill="#a8b8cc" fontSize="6" fontFamily={FONT}>
                Evidence · Ward B · Policy QMS-12 · CAPA-441
              </text>
              <circle cx="480" cy="322" r="14" fill="none" stroke="#1e2a3c" strokeWidth="3" />
              <circle
                cx="480"
                cy="322"
                r="14"
                fill="none"
                stroke="#5a8fd0"
                strokeWidth="3"
                strokeDasharray="70 88"
                strokeLinecap="round"
                transform="rotate(-90 480 322)"
              />
              <text
                x="472"
                y="325"
                fill="#d0daf0"
                fontSize="7"
                fontFamily={FONT}
                fontWeight="600"
              >
                94
              </text>
            </g>

            {/* Screen glass reflection */}
            <rect
              x="196"
              y="118"
              width="348"
              height="228"
              rx="3"
              fill="url(#hch-screen-shine)"
              pointerEvents="none"
            />
          </g>

          {/* ===== PHONE (foreground) ===== */}
          <g filter="url(#hch-phone-shadow)" transform="translate(598 368) rotate(-4)">
            <rect
              width="112"
              height="178"
              rx="16"
              fill="url(#hch-phone-shell)"
              stroke="#4a5a72"
              strokeWidth="1.5"
            />
            <rect
              x="8"
              y="14"
              width="96"
              height="158"
              rx="6"
              fill="url(#hch-phone-screen)"
            />
            <g clipPath="url(#hch-phone-clip)" transform="translate(0 0)">
              <rect x="38" y="18" width="36" height="4" rx="2" fill="#1a2538" />
              <text
                x="16"
                y="40"
                fill="#e8eef8"
                fontSize="9"
                fontFamily={FONT}
                fontWeight="600"
              >
                My Tasks
              </text>
              <text x="16" y="50" fill="#5a7088" fontSize="5.5" fontFamily={FONT}>
                Assigned · due today
              </text>
              <PhoneRow y={58} label="Evidence upload" meta="Ward A · photo" done />
              <PhoneRow y={84} label="Policy review" meta="QMS-08 · due 16:00" />
              <PhoneRow y={110} label="CAPA approval" meta="CAPA-219 · ICU" warn />
              <PhoneRow y={136} label="Audit checklist" meta="Fire drill · B-wing" />
            </g>
            {/* Home indicator */}
            <rect
              x="40"
              y="168"
              width="32"
              height="3"
              rx="1.5"
              fill="#3a4a60"
              opacity="0.6"
            />
          </g>
        </g>
      </svg>
    </div>
  );
}

/* -------------------------------------------------------------------------- */
/* Helpers                                                                     */
/* -------------------------------------------------------------------------- */

function ModulePanel({
  x,
  y,
  w,
  h,
  children,
}: {
  x: number;
  y: number;
  w: number;
  h: number;
  children: ReactNode;
}) {
  return (
    <g transform={`translate(${x} ${y})`}>
      <rect
        width={w}
        height={h}
        rx="11"
        fill="url(#hch-glass)"
        stroke="#4a5f7a"
        strokeOpacity="0.5"
        strokeWidth="1"
      />
      <rect
        width={w}
        height={h}
        rx="11"
        fill="url(#hch-glass-shine)"
        pointerEvents="none"
      />
      {children}
    </g>
  );
}

function MetricRow({
  x,
  y,
  label,
  value,
  valueX = 140,
  muted,
  tone,
}: {
  x: number;
  y: number;
  label: string;
  value: string;
  valueX?: number;
  muted?: boolean;
  tone?: 'good' | 'warn' | 'danger';
}) {
  const valueColor =
    tone === 'good'
      ? '#6bcea8'
      : tone === 'warn'
        ? '#e8a87c'
        : tone === 'danger'
          ? '#d4847a'
          : muted
            ? '#8a9bb3'
            : '#d0daf0';
  return (
    <g>
      <text x={x} y={y} fill="#6a7a90" fontSize="6.5" fontFamily={FONT}>
        {label}
      </text>
      <text
        x={valueX}
        y={y}
        fill={valueColor}
        fontSize="7"
        fontFamily={FONT}
        fontWeight="600"
        textAnchor="end"
      >
        {value}
      </text>
    </g>
  );
}

function NotifLine({
  x,
  y,
  text,
  tone,
  muted,
}: {
  x: number;
  y: number;
  text: string;
  tone?: 'warn';
  muted?: boolean;
}) {
  const color = tone === 'warn' ? '#e8a87c' : muted ? '#5a7088' : '#a8b8cc';
  return (
    <g>
      <circle
        cx={x + 3}
        cy={y - 2}
        r="2"
        fill={tone === 'warn' ? '#e8a87c' : muted ? '#3a4a60' : '#5a8fd0'}
        opacity="0.85"
      />
      <text x={x + 12} y={y} fill={color} fontSize="6.5" fontFamily={FONT}>
        {text}
      </text>
    </g>
  );
}

function KpiCard({
  x,
  y,
  w,
  label,
  value,
  tone,
}: {
  x: number;
  y: number;
  w: number;
  label: string;
  value: string;
  tone?: 'good' | 'warn';
}) {
  const valueColor =
    tone === 'good' ? '#6bcea8' : tone === 'warn' ? '#e8a87c' : '#e0e8f4';
  return (
    <g transform={`translate(${x} ${y})`}>
      <rect
        width={w}
        height={30}
        rx="3"
        fill="#0e1520"
        stroke="#1e2a3c"
        strokeWidth="0.7"
      />
      <text x="6" y="11" fill="#5a7088" fontSize="5.5" fontFamily={FONT}>
        {label}
      </text>
      <text
        x="6"
        y="24"
        fill={valueColor}
        fontSize="11"
        fontFamily={FONT}
        fontWeight="600"
      >
        {value}
      </text>
    </g>
  );
}

function DashBar({
  x,
  y,
  label,
  pct,
  w,
}: {
  x: number;
  y: number;
  label: string;
  pct: number;
  w: number;
}) {
  return (
    <g transform={`translate(${x} ${y})`}>
      <text x="0" y="5" fill="#6a7a90" fontSize="5" fontFamily={FONT}>
        {label}
      </text>
      <rect x="36" y="0" width={w} height="4.5" rx="1.5" fill="#1a2538" />
      <rect
        x="36"
        y="0"
        width={Math.max(4, w * pct)}
        height="4.5"
        rx="1.5"
        fill="#5a8fd0"
        opacity="0.88"
      />
    </g>
  );
}

function MiniBar({
  x,
  y,
  label,
  pct,
  w,
}: {
  x: number;
  y: number;
  label: string;
  pct: number;
  w: number;
}) {
  return (
    <g transform={`translate(${x} ${y})`}>
      <text x="0" y="5" fill="#7a8aa3" fontSize="5.5" fontFamily={FONT}>
        {label}
      </text>
      <rect x="52" y="0" width={w} height="5" rx="1.5" fill="#1a2538" />
      <rect
        x="52"
        y="0"
        width={Math.max(4, w * pct)}
        height="5"
        rx="1.5"
        fill="#5a8fd0"
        opacity="0.85"
      />
    </g>
  );
}

function DonutMini({
  cx,
  cy,
  r,
  pct,
}: {
  cx: number;
  cy: number;
  r: number;
  pct: number;
}) {
  const c = 2 * Math.PI * r;
  const filled = c * pct;
  return (
    <g>
      <circle
        cx={cx}
        cy={cy}
        r={r}
        fill="none"
        stroke="#1a2538"
        strokeWidth="4"
      />
      <circle
        cx={cx}
        cy={cy}
        r={r}
        fill="none"
        stroke="url(#hch-donut)"
        strokeWidth="4"
        strokeDasharray={`${filled} ${c}`}
        strokeLinecap="round"
        transform={`rotate(-90 ${cx} ${cy})`}
      />
      <text
        x={cx}
        y={cy + 3}
        fill="#d0daf0"
        fontSize="7"
        fontFamily={FONT}
        fontWeight="600"
        textAnchor="middle"
      >
        {Math.round(pct * 100)}
      </text>
    </g>
  );
}

function PhoneRow({
  y,
  label,
  meta,
  done,
  warn,
}: {
  y: number;
  label: string;
  meta: string;
  done?: boolean;
  warn?: boolean;
}) {
  return (
    <g transform={`translate(12 ${y})`}>
      <rect
        width="88"
        height="22"
        rx="4"
        fill="#101820"
        stroke="#243044"
        strokeWidth="0.55"
      />
      <circle
        cx="9"
        cy="11"
        r="3.5"
        fill={done ? '#6bcea8' : warn ? '#e8a87c' : 'transparent'}
        stroke={done ? '#6bcea8' : warn ? '#e8a87c' : '#3a4a63'}
        strokeWidth="0.85"
        opacity={0.9}
      />
      {done ? (
        <path
          d="M7 11 L8.5 12.5 L11.5 9.5"
          fill="none"
          stroke="#0c121e"
          strokeWidth="1"
          strokeLinecap="round"
        />
      ) : null}
      <text x="18" y="10" fill="#d0daf0" fontSize="6" fontFamily={FONT}>
        {label}
      </text>
      <text x="18" y="18" fill="#5a7088" fontSize="5" fontFamily={FONT}>
        {meta}
      </text>
    </g>
  );
}

/* Outline icons — consistent 12×12 enterprise stroke */

function IconFolder({ x, y }: { x: number; y: number }) {
  return (
    <g transform={`translate(${x} ${y})`} fill="none" stroke="#7eb8e8" strokeWidth="1.2" strokeLinecap="round" strokeLinejoin="round">
      <path d="M1 4.5 V11 H11 V4.5 H6.5 L5 3 H1 Z" />
    </g>
  );
}

function IconShield({ x, y }: { x: number; y: number }) {
  return (
    <g transform={`translate(${x} ${y})`} fill="none" stroke="#7eb8e8" strokeWidth="1.2" strokeLinecap="round" strokeLinejoin="round">
      <path d="M6 1.5 L10.5 3.5 V7 C10.5 9.5 8.5 11.2 6 12 C3.5 11.2 1.5 9.5 1.5 7 V3.5 Z" />
    </g>
  );
}

function IconRing({ x, y }: { x: number; y: number }) {
  return (
    <g transform={`translate(${x} ${y})`} fill="none" stroke="#7eb8e8" strokeWidth="1.2">
      <circle cx="6" cy="6.5" r="4.5" />
      <path d="M6 2 A4.5 4.5 0 0 1 10.5 6.5" strokeLinecap="round" />
    </g>
  );
}

function IconList({ x, y }: { x: number; y: number }) {
  return (
    <g transform={`translate(${x} ${y})`} fill="none" stroke="#7eb8e8" strokeWidth="1.2" strokeLinecap="round">
      <path d="M4 3.5 H10.5 M4 6.5 H10.5 M4 9.5 H8.5" />
      <circle cx="2" cy="3.5" r="0.8" fill="#7eb8e8" stroke="none" />
      <circle cx="2" cy="6.5" r="0.8" fill="#7eb8e8" stroke="none" />
      <circle cx="2" cy="9.5" r="0.8" fill="#7eb8e8" stroke="none" />
    </g>
  );
}

function IconDoc({ x, y }: { x: number; y: number }) {
  return (
    <g transform={`translate(${x} ${y})`} fill="none" stroke="#7eb8e8" strokeWidth="1.2" strokeLinecap="round" strokeLinejoin="round">
      <path d="M3.5 1.5 H7.5 L10.5 4.5 V11.5 H3.5 Z" />
      <path d="M7.5 1.5 V4.5 H10.5" />
    </g>
  );
}

function IconBars({ x, y }: { x: number; y: number }) {
  return (
    <g transform={`translate(${x} ${y})`} fill="#7eb8e8" opacity="0.9">
      <rect x="1.5" y="7" width="2.2" height="4" rx="0.4" />
      <rect x="4.8" y="4.5" width="2.2" height="6.5" rx="0.4" />
      <rect x="8.1" y="2.5" width="2.2" height="8.5" rx="0.4" />
    </g>
  );
}

function IconCheck({ x, y }: { x: number; y: number }) {
  return (
    <g transform={`translate(${x} ${y})`} fill="none" stroke="#7eb8e8" strokeWidth="1.2" strokeLinecap="round" strokeLinejoin="round">
      <rect x="1.5" y="1.5" width="9" height="9" rx="2" />
      <path d="M3.5 6.5 L5.2 8.2 L8.8 4" />
    </g>
  );
}

function IconBell({ x, y }: { x: number; y: number }) {
  return (
    <g transform={`translate(${x} ${y})`} fill="none" stroke="#7eb8e8" strokeWidth="1.2" strokeLinecap="round" strokeLinejoin="round">
      <path d="M6 1.5 C4 1.5 2.5 3.2 2.5 5.5 V8 L1.5 9.5 H10.5 L9.5 8 V5.5 C9.5 3.2 8 1.5 6 1.5 Z" />
      <path d="M5 10.5 C5.3 11.2 5.6 11.5 6 11.5 C6.4 11.5 6.7 11.2 7 10.5" />
    </g>
  );
}
