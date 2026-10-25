import type { ReactNode } from 'react';

import { cn } from '@/lib/utils';

const FONT =
  'ui-sans-serif, system-ui, -apple-system, "Segoe UI", sans-serif';

export type HealthcareWorkspaceId =
  | 'executive'
  | 'compliance'
  | 'evidence'
  | 'capa'
  | 'dept'
  | 'mobile';

type ShotProps = {
  className?: string | undefined;
};

/**
 * High-fidelity AOVIAS Command workspace surfaces for the healthcare product
 * showcase. Designed as product UI — not abstract wireframes. Replace with
 * captured screenshots via imageSrc when available.
 */
export function HealthcareWorkspaceShot({
  id,
  className,
}: {
  id: HealthcareWorkspaceId;
  className?: string;
}) {
  const shotProps = className ? { className } : {};
  switch (id) {
    case 'executive':
      return <ExecutiveShot {...shotProps} />;
    case 'compliance':
      return <ComplianceShot {...shotProps} />;
    case 'evidence':
      return <EvidenceShot {...shotProps} />;
    case 'capa':
      return <CapaShot {...shotProps} />;
    case 'dept':
      return <DeptShot {...shotProps} />;
    case 'mobile':
      return <MobileShot {...shotProps} />;
    default:
      return null;
  }
}

function Shell({
  className,
  viewBox,
  children,
  title,
}: {
  className?: string | undefined;
  viewBox: string;
  children: ReactNode;
  title: string;
}) {
  return (
    <svg
      viewBox={viewBox}
      className={cn('size-full', className)}
      xmlns="http://www.w3.org/2000/svg"
      role="img"
      aria-label={title}
    >
      <title>{title}</title>
      {children}
    </svg>
  );
}

function Sidebar({
  uid,
  active,
  labels,
}: {
  uid: string;
  active: number;
  labels: string[];
}) {
  return (
    <g>
      <rect width="56" height="540" fill="#0a1018" />
      <rect x="14" y="16" width="28" height="16" rx="4" fill="#5a8fd0" opacity="0.9" />
      <text
        x="20"
        y="28"
        fill="#e8eef8"
        fontSize="9"
        fontFamily={FONT}
        fontWeight="700"
      >
        AO
      </text>
      {labels.map((label, i) => (
        <g key={label}>
          <rect
            x="10"
            y={48 + i * 36}
            width="36"
            height="28"
            rx="6"
            fill={i === active ? '#1a3050' : 'transparent'}
            stroke={i === active ? '#5a8fd0' : 'transparent'}
            strokeWidth="1"
          />
          <rect
            x="18"
            y={56 + i * 36}
            width="20"
            height="8"
            rx="2"
            fill={i === active ? '#7eb8e8' : '#3a4a60'}
            opacity="0.75"
          />
          <title>{label}</title>
        </g>
      ))}
      <defs>
        <linearGradient id={`${uid}-bg`} x1="0" y1="0" x2="0" y2="1">
          <stop offset="0" stopColor="#141c2c" />
          <stop offset="1" stopColor="#0c121e" />
        </linearGradient>
      </defs>
    </g>
  );
}

function TopBar({ title, subtitle }: { title: string; subtitle: string }) {
  return (
    <g>
      <rect x="56" y="0" width="904" height="44" fill="#0e1520" />
      <text
        x="76"
        y="20"
        fill="#e8eef8"
        fontSize="13"
        fontFamily={FONT}
        fontWeight="600"
      >
        {title}
      </text>
      <text x="76" y="34" fill="#5a7088" fontSize="9" fontFamily={FONT}>
        {subtitle}
      </text>
      <rect
        x="700"
        y="12"
        width="160"
        height="20"
        rx="4"
        fill="#121a28"
        stroke="#243044"
        strokeWidth="0.75"
      />
      <text x="712" y="25" fill="#5a7088" fontSize="8" fontFamily={FONT}>
        Search workspace
      </text>
      <circle
        cx="920"
        cy="22"
        r="11"
        fill="#1a2538"
        stroke="#3a4a63"
        strokeWidth="0.75"
      />
      <text
        x="913"
        y="26"
        fill="#9fc4f0"
        fontSize="8"
        fontFamily={FONT}
        fontWeight="600"
      >
        AD
      </text>
    </g>
  );
}

function Kpi({
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
  const color =
    tone === 'good' ? '#6bcea8' : tone === 'warn' ? '#e8a87c' : '#e0e8f4';
  return (
    <g transform={`translate(${x} ${y})`}>
      <rect
        width={w}
        height="52"
        rx="8"
        fill="#121a28"
        stroke="#243044"
        strokeWidth="0.75"
      />
      <text x="12" y="18" fill="#6a7a90" fontSize="9" fontFamily={FONT}>
        {label}
      </text>
      <text
        x="12"
        y="40"
        fill={color}
        fontSize="18"
        fontFamily={FONT}
        fontWeight="600"
      >
        {value}
      </text>
    </g>
  );
}

function ExecutiveShot({ className }: ShotProps) {
  const uid = 'exec';
  return (
    <Shell
      className={className}
      viewBox="0 0 960 540"
      title="AOVIAS Command Executive Dashboard"
    >
      <defs>
        <linearGradient id={`${uid}-bg`} x1="0" y1="0" x2="0" y2="1">
          <stop offset="0" stopColor="#141c2c" />
          <stop offset="1" stopColor="#0c121e" />
        </linearGradient>
        <linearGradient id={`${uid}-area`} x1="0" y1="1" x2="0" y2="0">
          <stop offset="0" stopColor="#5a8fd0" stopOpacity="0" />
          <stop offset="1" stopColor="#5a8fd0" stopOpacity="0.3" />
        </linearGradient>
      </defs>
      <rect width="960" height="540" fill={`url(#${uid}-bg)`} />
      <Sidebar
        uid={uid}
        active={0}
        labels={['Overview', 'Compliance', 'Tasks', 'Evidence', 'CAPA', 'Reports']}
      />
      <TopBar
        title="Executive Overview"
        subtitle="Multi-site · Live operational posture"
      />
      <Kpi x={76} y={60} w={150} label="Compliance" value="94%" tone="good" />
      <Kpi x={240} y={60} w={150} label="Open tasks" value="128" />
      <Kpi x={404} y={60} w={150} label="CAPA open" value="7" tone="warn" />
      <Kpi x={568} y={60} w={150} label="Depts active" value="24" />
      <Kpi x={732} y={60} w={172} label="Audit readiness" value="94%" tone="good" />

      <rect
        x="76"
        y="130"
        width="520"
        height="220"
        rx="10"
        fill="#121a28"
        stroke="#243044"
        strokeWidth="0.75"
      />
      <text x="92" y="154" fill="#8a9bb3" fontSize="11" fontFamily={FONT}>
        Compliance timeline · 6 months
      </text>
      <path
        d="M100 300 L160 285 L220 290 L280 255 L340 265 L400 230 L460 240 L520 210 L560 218"
        fill="none"
        stroke="#5a8fd0"
        strokeWidth="2.2"
        strokeLinecap="round"
        strokeLinejoin="round"
      />
      <path
        d="M100 300 L160 285 L220 290 L280 255 L340 265 L400 230 L460 240 L520 210 L560 218 L560 330 L100 330 Z"
        fill={`url(#${uid}-area)`}
      />

      <rect
        x="612"
        y="130"
        width="292"
        height="220"
        rx="10"
        fill="#121a28"
        stroke="#243044"
        strokeWidth="0.75"
      />
      <text x="628" y="154" fill="#8a9bb3" fontSize="11" fontFamily={FONT}>
        Department performance
      </text>
      {[
        ['Nursing', 0.9],
        ['ICU', 0.84],
        ['Laboratory', 0.78],
        ['Pharmacy', 0.86],
        ['Administration', 0.81],
      ].map(([label, pct], i) => (
        <g key={String(label)} transform={`translate(628 ${172 + i * 32})`}>
          <text x="0" y="10" fill="#7a8aa3" fontSize="9" fontFamily={FONT}>
            {label}
          </text>
          <rect x="100" y="2" width="160" height="10" rx="3" fill="#1a2538" />
          <rect
            x="100"
            y="2"
            width={160 * Number(pct)}
            height="10"
            rx="3"
            fill="#5a8fd0"
            opacity="0.88"
          />
        </g>
      ))}

      <rect
        x="76"
        y="368"
        width="520"
        height="148"
        rx="10"
        fill="#121a28"
        stroke="#243044"
        strokeWidth="0.75"
      />
      <text x="92" y="392" fill="#8a9bb3" fontSize="11" fontFamily={FONT}>
        Recent governance activity
      </text>
      {[
        ['Evidence uploaded', 'Ward B · Fire drill', '12m'],
        ['Policy approved', 'QMS-12 · Quality', '1h'],
        ['CAPA assigned', 'CAPA-441 · ICU', '2h'],
        ['Assessment closed', 'Chapter 5 · NABH', 'Yesterday'],
      ].map(([a, b, t], i) => (
        <g key={a} transform={`translate(92 ${410 + i * 24})`}>
          <circle cx="4" cy="0" r="3" fill={i === 0 ? '#6bcea8' : '#5a8fd0'} />
          <text x="16" y="3" fill="#d0daf0" fontSize="10" fontFamily={FONT}>
            {a}
          </text>
          <text x="180" y="3" fill="#6a7a90" fontSize="9" fontFamily={FONT}>
            {b}
          </text>
          <text x="420" y="3" fill="#5a7088" fontSize="9" fontFamily={FONT}>
            {t}
          </text>
        </g>
      ))}

      <rect
        x="612"
        y="368"
        width="292"
        height="148"
        rx="10"
        fill="#121a28"
        stroke="#243044"
        strokeWidth="0.75"
      />
      <text x="628" y="392" fill="#8a9bb3" fontSize="11" fontFamily={FONT}>
        Risk indicators
      </text>
      {[
        ['Overdue CAPA', '1', '#d4847a'],
        ['Policies pending', '3', '#e8a87c'],
        ['Evidence gaps', '6', '#e8a87c'],
        ['Controls on track', '188', '#6bcea8'],
      ].map(([label, value, color], i) => (
        <g key={label} transform={`translate(628 ${412 + i * 24})`}>
          <text x="0" y="3" fill="#8a9bb3" fontSize="10" fontFamily={FONT}>
            {label}
          </text>
          <text
            x="250"
            y="3"
            fill={color}
            fontSize="11"
            fontFamily={FONT}
            fontWeight="600"
            textAnchor="end"
          >
            {value}
          </text>
        </g>
      ))}
    </Shell>
  );
}

function ComplianceShot({ className }: ShotProps) {
  const uid = 'cmp';
  return (
    <Shell
      className={className}
      viewBox="0 0 960 540"
      title="AOVIAS Command Compliance Workspace"
    >
      <defs>
        <linearGradient id={`${uid}-bg`} x1="0" y1="0" x2="0" y2="1">
          <stop offset="0" stopColor="#141c2c" />
          <stop offset="1" stopColor="#0c121e" />
        </linearGradient>
      </defs>
      <rect width="960" height="540" fill={`url(#${uid}-bg)`} />
      <Sidebar
        uid={uid}
        active={1}
        labels={['Overview', 'Compliance', 'Tasks', 'Evidence', 'CAPA', 'Reports']}
      />
      <TopBar
        title="Compliance Workspace"
        subtitle="Standards · Objective elements · Assessments"
      />

      <rect
        x="76"
        y="60"
        width="220"
        height="456"
        rx="10"
        fill="#121a28"
        stroke="#243044"
        strokeWidth="0.75"
      />
      <text
        x="92"
        y="86"
        fill="#e8eef8"
        fontSize="12"
        fontFamily={FONT}
        fontWeight="600"
      >
        Standards library
      </text>
      {[
        ['NABH 5th Edition', true],
        ['Infection Control', false],
        ['Patient Safety', false],
        ['Facility Management', false],
        ['Human Resource', false],
        ['Information Mgmt', false],
      ].map(([label, on], i) => (
        <g key={String(label)} transform={`translate(88 ${104 + i * 44})`}>
          <rect
            width="196"
            height="36"
            rx="6"
            fill={on ? '#1a3050' : '#0e1520'}
            stroke={on ? '#5a8fd0' : '#243044'}
            strokeWidth="0.75"
          />
          <text
            x="12"
            y="22"
            fill={on ? '#e8eef8' : '#a8b8cc'}
            fontSize="10"
            fontFamily={FONT}
          >
            {label}
          </text>
        </g>
      ))}

      <rect
        x="312"
        y="60"
        width="592"
        height="456"
        rx="10"
        fill="#121a28"
        stroke="#243044"
        strokeWidth="0.75"
      />
      <text
        x="332"
        y="90"
        fill="#e8eef8"
        fontSize="14"
        fontFamily={FONT}
        fontWeight="600"
      >
        Chapter 3 · Access, Assessment & Continuity of Care
      </text>
      <text x="332" y="112" fill="#6a7a90" fontSize="10" fontFamily={FONT}>
        18 objective elements · 16 mapped · 2 under review
      </text>

      <rect
        x="332"
        y="132"
        width="170"
        height="48"
        rx="8"
        fill="#0e1520"
        stroke="#243044"
      />
      <text x="344" y="152" fill="#6a7a90" fontSize="9" fontFamily={FONT}>
        Mapped
      </text>
      <text
        x="344"
        y="170"
        fill="#6bcea8"
        fontSize="16"
        fontFamily={FONT}
        fontWeight="600"
      >
        89%
      </text>
      <rect
        x="518"
        y="132"
        width="170"
        height="48"
        rx="8"
        fill="#0e1520"
        stroke="#243044"
      />
      <text x="530" y="152" fill="#6a7a90" fontSize="9" fontFamily={FONT}>
        Assessments due
      </text>
      <text
        x="530"
        y="170"
        fill="#e8a87c"
        fontSize="16"
        fontFamily={FONT}
        fontWeight="600"
      >
        4
      </text>
      <rect
        x="704"
        y="132"
        width="176"
        height="48"
        rx="8"
        fill="#0e1520"
        stroke="#243044"
      />
      <text x="716" y="152" fill="#6a7a90" fontSize="9" fontFamily={FONT}>
        Owners assigned
      </text>
      <text
        x="716"
        y="170"
        fill="#d0daf0"
        fontSize="16"
        fontFamily={FONT}
        fontWeight="600"
      >
        18/18
      </text>

      <text x="332" y="214" fill="#8a9bb3" fontSize="11" fontFamily={FONT}>
        Objective elements
      </text>
      {[
        ['AAC.1', 'Registration & admission', 'Mapped', '#6bcea8'],
        ['AAC.2', 'Initial assessment', 'Review', '#e8a87c'],
        ['AAC.3', 'Care planning', 'Mapped', '#6bcea8'],
        ['AAC.4', 'Transfer & referral', 'Mapped', '#6bcea8'],
        ['AAC.5', 'Discharge process', 'Gap', '#d4847a'],
        ['AAC.6', 'Continuity of care', 'Mapped', '#6bcea8'],
      ].map(([code, name, status, color], i) => (
        <g key={code} transform={`translate(332 ${228 + i * 42})`}>
          <rect
            width="548"
            height="36"
            rx="6"
            fill="#0e1520"
            stroke="#1e2a3c"
            strokeWidth="0.75"
          />
          <text
            x="14"
            y="22"
            fill="#9fc4f0"
            fontSize="10"
            fontFamily={FONT}
            fontWeight="600"
          >
            {code}
          </text>
          <text x="70" y="22" fill="#d0daf0" fontSize="10" fontFamily={FONT}>
            {name}
          </text>
          <text
            x="520"
            y="22"
            fill={color}
            fontSize="10"
            fontFamily={FONT}
            fontWeight="600"
            textAnchor="end"
          >
            {status}
          </text>
        </g>
      ))}
    </Shell>
  );
}

function EvidenceShot({ className }: ShotProps) {
  const uid = 'evd';
  return (
    <Shell
      className={className}
      viewBox="0 0 960 540"
      title="AOVIAS Command Evidence Repository"
    >
      <defs>
        <linearGradient id={`${uid}-bg`} x1="0" y1="0" x2="0" y2="1">
          <stop offset="0" stopColor="#141c2c" />
          <stop offset="1" stopColor="#0c121e" />
        </linearGradient>
      </defs>
      <rect width="960" height="540" fill={`url(#${uid}-bg)`} />
      <Sidebar
        uid={uid}
        active={3}
        labels={['Overview', 'Compliance', 'Tasks', 'Evidence', 'CAPA', 'Reports']}
      />
      <TopBar
        title="Evidence Repository"
        subtitle="Indexed · Versioned · Linked to standards"
      />

      <rect
        x="76"
        y="60"
        width="280"
        height="52"
        rx="8"
        fill="#121a28"
        stroke="#243044"
      />
      <text x="92" y="82" fill="#6a7a90" fontSize="9" fontFamily={FONT}>
        Indexed files
      </text>
      <text
        x="92"
        y="100"
        fill="#e0e8f4"
        fontSize="16"
        fontFamily={FONT}
        fontWeight="600"
      >
        12,840
      </text>
      <rect
        x="372"
        y="60"
        width="240"
        height="52"
        rx="8"
        fill="#121a28"
        stroke="#243044"
      />
      <text x="388" y="82" fill="#6a7a90" fontSize="9" fontFamily={FONT}>
        Linked standards
      </text>
      <text
        x="388"
        y="100"
        fill="#e0e8f4"
        fontSize="16"
        fontFamily={FONT}
        fontWeight="600"
      >
        214
      </text>
      <rect
        x="628"
        y="60"
        width="276"
        height="52"
        rx="8"
        fill="#121a28"
        stroke="#243044"
      />
      <text x="644" y="82" fill="#6a7a90" fontSize="9" fontFamily={FONT}>
        Search status
      </text>
      <text
        x="644"
        y="100"
        fill="#6bcea8"
        fontSize="16"
        fontFamily={FONT}
        fontWeight="600"
      >
        Ready
      </text>

      <rect
        x="76"
        y="132"
        width="828"
        height="40"
        rx="8"
        fill="#0e1520"
        stroke="#243044"
      />
      <text x="96" y="156" fill="#5a7088" fontSize="11" fontFamily={FONT}>
        Search evidence by standard, department, file type, or owner…
      </text>

      <rect
        x="76"
        y="188"
        width="828"
        height="328"
        rx="10"
        fill="#121a28"
        stroke="#243044"
      />
      <text x="96" y="216" fill="#8a9bb3" fontSize="10" fontFamily={FONT}>
        Name
      </text>
      <text x="360" y="216" fill="#8a9bb3" fontSize="10" fontFamily={FONT}>
        Linked standard
      </text>
      <text x="560" y="216" fill="#8a9bb3" fontSize="10" fontFamily={FONT}>
        Version
      </text>
      <text x="660" y="216" fill="#8a9bb3" fontSize="10" fontFamily={FONT}>
        Updated
      </text>
      <text x="780" y="216" fill="#8a9bb3" fontSize="10" fontFamily={FONT}>
        Owner
      </text>
      <line
        x1="96"
        y1="228"
        x2="880"
        y2="228"
        stroke="#1e2a3c"
        strokeWidth="1"
      />
      {[
        ['Fire_Drill_WardB_Jun.pdf', 'FM.5 · Safety', 'v3', '14m ago', 'Quality'],
        ['IC_Audit_Checklist.xlsx', 'IC.2 · Infection', 'v7', '2h ago', 'Nursing'],
        ['Policy_QMS12_Signed.pdf', 'QMS.12 · Governance', 'v2', 'Yesterday', 'Admin'],
        ['ICU_Handover_Log.pdf', 'AAC.6 · Continuity', 'v11', 'Yesterday', 'ICU'],
        ['Pharmacy_Temp_Log.csv', 'PSQ.4 · Safety', 'v4', '2d ago', 'Pharmacy'],
        ['Staff_Competency_Matrix.xlsx', 'HR.3 · Competence', 'v9', '3d ago', 'HR'],
      ].map(([name, std, ver, upd, owner], i) => (
        <g key={name} transform={`translate(0 ${244 + i * 40})`}>
          <text x="96" y="0" fill="#d0daf0" fontSize="11" fontFamily={FONT}>
            {name}
          </text>
          <text x="360" y="0" fill="#9fc4f0" fontSize="10" fontFamily={FONT}>
            {std}
          </text>
          <text x="560" y="0" fill="#8a9bb3" fontSize="10" fontFamily={FONT}>
            {ver}
          </text>
          <text x="660" y="0" fill="#6a7a90" fontSize="10" fontFamily={FONT}>
            {upd}
          </text>
          <text x="780" y="0" fill="#a8b8cc" fontSize="10" fontFamily={FONT}>
            {owner}
          </text>
        </g>
      ))}
    </Shell>
  );
}

function CapaShot({ className }: ShotProps) {
  const uid = 'capa';
  return (
    <Shell
      className={className}
      viewBox="0 0 960 540"
      title="AOVIAS Command Corrective Action Tracking"
    >
      <defs>
        <linearGradient id={`${uid}-bg`} x1="0" y1="0" x2="0" y2="1">
          <stop offset="0" stopColor="#141c2c" />
          <stop offset="1" stopColor="#0c121e" />
        </linearGradient>
      </defs>
      <rect width="960" height="540" fill={`url(#${uid}-bg)`} />
      <Sidebar
        uid={uid}
        active={4}
        labels={['Overview', 'Compliance', 'Tasks', 'Evidence', 'CAPA', 'Reports']}
      />
      <TopBar
        title="Corrective Action Tracking"
        subtitle="Assignments · Deadlines · Escalations"
      />

      <Kpi x={76} y={60} w={196} label="Open CAPA" value="7" />
      <Kpi x={288} y={60} w={196} label="Due today" value="2" tone="warn" />
      <Kpi x={500} y={60} w={196} label="Overdue" value="1" tone="warn" />
      <Kpi x={712} y={60} w={192} label="Closed (30d)" value="14" tone="good" />

      <rect
        x="76"
        y="132"
        width="828"
        height="384"
        rx="10"
        fill="#121a28"
        stroke="#243044"
      />
      <text x="96" y="160" fill="#8a9bb3" fontSize="10" fontFamily={FONT}>
        ID
      </text>
      <text x="170" y="160" fill="#8a9bb3" fontSize="10" fontFamily={FONT}>
        Finding
      </text>
      <text x="460" y="160" fill="#8a9bb3" fontSize="10" fontFamily={FONT}>
        Owner
      </text>
      <text x="580" y="160" fill="#8a9bb3" fontSize="10" fontFamily={FONT}>
        Due
      </text>
      <text x="680" y="160" fill="#8a9bb3" fontSize="10" fontFamily={FONT}>
        Progress
      </text>
      <text x="820" y="160" fill="#8a9bb3" fontSize="10" fontFamily={FONT}>
        Status
      </text>
      <line
        x1="96"
        y1="172"
        x2="880"
        y2="172"
        stroke="#1e2a3c"
        strokeWidth="1"
      />
      {[
        ['CAPA-441', 'ICU handoff documentation gap', 'Dr. Mehta', 'Today', 0.55, 'In progress', '#5a8fd0'],
        ['CAPA-438', 'Pharmacy cold-chain log delay', 'R. Singh', 'Today', 0.7, 'In progress', '#5a8fd0'],
        ['CAPA-429', 'Fire drill evidence incomplete', 'Facilities', 'Overdue', 0.35, 'Escalated', '#d4847a'],
        ['CAPA-422', 'Consent form version drift', 'Quality', '3d', 0.85, 'Review', '#e8a87c'],
        ['CAPA-418', 'Ward B checklist ownership', 'Nursing', '5d', 0.4, 'Open', '#8a9bb3'],
        ['CAPA-410', 'Lab TAT reporting lag', 'Lab Head', '1w', 0.6, 'In progress', '#5a8fd0'],
      ].map(([id, finding, owner, due, pct, status, color], i) => (
        <g key={String(id)} transform={`translate(0 ${196 + i * 48})`}>
          <text
            x="96"
            y="0"
            fill="#9fc4f0"
            fontSize="10"
            fontFamily={FONT}
            fontWeight="600"
          >
            {id}
          </text>
          <text x="170" y="0" fill="#d0daf0" fontSize="10" fontFamily={FONT}>
            {finding}
          </text>
          <text x="460" y="0" fill="#a8b8cc" fontSize="10" fontFamily={FONT}>
            {owner}
          </text>
          <text
            x="580"
            y="0"
            fill={due === 'Overdue' ? '#d4847a' : '#8a9bb3'}
            fontSize="10"
            fontFamily={FONT}
          >
            {due}
          </text>
          <rect x="680" y="-8" width="100" height="8" rx="2" fill="#1a2538" />
          <rect
            x="680"
            y="-8"
            width={100 * Number(pct)}
            height="8"
            rx="2"
            fill={String(color)}
            opacity="0.85"
          />
          <text
            x="820"
            y="0"
            fill={String(color)}
            fontSize="10"
            fontFamily={FONT}
            fontWeight="600"
          >
            {status}
          </text>
        </g>
      ))}
    </Shell>
  );
}

function DeptShot({ className }: ShotProps) {
  const uid = 'dept';
  return (
    <Shell
      className={className}
      viewBox="0 0 960 540"
      title="AOVIAS Command Department Performance"
    >
      <defs>
        <linearGradient id={`${uid}-bg`} x1="0" y1="0" x2="0" y2="1">
          <stop offset="0" stopColor="#141c2c" />
          <stop offset="1" stopColor="#0c121e" />
        </linearGradient>
      </defs>
      <rect width="960" height="540" fill={`url(#${uid}-bg)`} />
      <Sidebar
        uid={uid}
        active={5}
        labels={['Overview', 'Compliance', 'Tasks', 'Evidence', 'CAPA', 'Depts']}
      />
      <TopBar
        title="Department Performance"
        subtitle="Accountability · Pending work · Readiness trends"
      />

      {[
        ['Nursing', '94%', '12 open', '1 CAPA', 0.94],
        ['ICU', '88%', '9 open', '2 CAPA', 0.88],
        ['Laboratory', '91%', '6 open', '0 CAPA', 0.91],
        ['Pharmacy', '86%', '8 open', '1 CAPA', 0.86],
        ['Administration', '90%', '5 open', '0 CAPA', 0.9],
        ['Facilities', '82%', '11 open', '1 CAPA', 0.82],
      ].map(([name, score, tasks, capa, pct], i) => {
        const col = i % 3;
        const row = Math.floor(i / 3);
        const x = 76 + col * 292;
        const y = 60 + row * 230;
        return (
          <g key={String(name)} transform={`translate(${x} ${y})`}>
            <rect
              width="276"
              height="210"
              rx="12"
              fill="#121a28"
              stroke="#243044"
              strokeWidth="0.75"
            />
            <text
              x="20"
              y="36"
              fill="#e8eef8"
              fontSize="14"
              fontFamily={FONT}
              fontWeight="600"
            >
              {name}
            </text>
            <text
              x="20"
              y="68"
              fill="#6bcea8"
              fontSize="28"
              fontFamily={FONT}
              fontWeight="600"
            >
              {score}
            </text>
            <text x="100" y="68" fill="#6a7a90" fontSize="11" fontFamily={FONT}>
              readiness
            </text>
            <text x="20" y="100" fill="#8a9bb3" fontSize="11" fontFamily={FONT}>
              Pending work
            </text>
            <text
              x="20"
              y="122"
              fill="#d0daf0"
              fontSize="13"
              fontFamily={FONT}
              fontWeight="600"
            >
              {tasks}
            </text>
            <text x="140" y="100" fill="#8a9bb3" fontSize="11" fontFamily={FONT}>
              Corrective
            </text>
            <text
              x="140"
              y="122"
              fill="#e8a87c"
              fontSize="13"
              fontFamily={FONT}
              fontWeight="600"
            >
              {capa}
            </text>
            <text x="20" y="158" fill="#6a7a90" fontSize="10" fontFamily={FONT}>
              Trend
            </text>
            <rect x="20" y="168" width="236" height="10" rx="3" fill="#1a2538" />
            <rect
              x="20"
              y="168"
              width={236 * Number(pct)}
              height="10"
              rx="3"
              fill="#5a8fd0"
              opacity="0.88"
            />
            <text x="20" y="198" fill="#5a7088" fontSize="9" fontFamily={FONT}>
              Owner · Department lead
            </text>
          </g>
        );
      })}
    </Shell>
  );
}

function MobileShot({ className }: ShotProps) {
  const uid = 'mob';
  return (
    <Shell
      className={className}
      viewBox="0 0 320 640"
      title="AOVIAS Command Mobile Experience"
    >
      <defs>
        <linearGradient id={`${uid}-bg`} x1="0" y1="0" x2="0" y2="1">
          <stop offset="0" stopColor="#151e2e" />
          <stop offset="1" stopColor="#0c121e" />
        </linearGradient>
      </defs>
      <rect width="320" height="640" fill={`url(#${uid}-bg)`} />
      <text
        x="24"
        y="48"
        fill="#e8eef8"
        fontSize="22"
        fontFamily={FONT}
        fontWeight="600"
      >
        My Tasks
      </text>
      <text x="24" y="72" fill="#6a7a90" fontSize="12" fontFamily={FONT}>
        Field operations · due today
      </text>

      {[
        ['Evidence upload', 'Ward A · photo capture', '#6bcea8', true],
        ['Policy review', 'QMS-08 · due 16:00', '#5a8fd0', false],
        ['CAPA approval', 'CAPA-219 · ICU', '#e8a87c', false],
        ['Audit checklist', 'Fire drill · B-wing', '#5a8fd0', false],
        ['Assigned task', 'Lab evidence link', '#8a9bb3', false],
      ].map(([label, meta, color, done], i) => (
        <g key={String(label)} transform={`translate(20 ${100 + i * 88})`}>
          <rect
            width="280"
            height="72"
            rx="12"
            fill="#121a28"
            stroke="#243044"
            strokeWidth="1"
          />
          <circle
            cx="28"
            cy="36"
            r="12"
            fill={done ? String(color) : 'transparent'}
            stroke={String(color)}
            strokeWidth="1.5"
            opacity="0.9"
          />
          {done ? (
            <path
              d="M22 36 L26 40 L34 30"
              fill="none"
              stroke="#0c121e"
              strokeWidth="2"
              strokeLinecap="round"
            />
          ) : null}
          <text
            x="56"
            y="32"
            fill="#e8eef8"
            fontSize="14"
            fontFamily={FONT}
            fontWeight="600"
          >
            {label}
          </text>
          <text x="56" y="52" fill="#6a7a90" fontSize="11" fontFamily={FONT}>
            {meta}
          </text>
        </g>
      ))}

      <rect
        x="20"
        y="560"
        width="280"
        height="52"
        rx="12"
        fill="#1a3050"
        stroke="#5a8fd0"
        strokeWidth="1"
      />
      <text
        x="160"
        y="592"
        fill="#e8eef8"
        fontSize="13"
        fontFamily={FONT}
        fontWeight="600"
        textAnchor="middle"
      >
        Capture evidence
      </text>
    </Shell>
  );
}
