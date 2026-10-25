import { ChangeDetectionStrategy, Component, Input } from '@angular/core';
import { NgIcon, provideIcons } from '@ng-icons/core';
import {
  lucideActivity,
  lucideBriefcase,
  lucideBuilding2,
  lucideDroplet,
  lucideFileText,
  lucideFlame,
  lucideFlaskConical,
  lucideHeartPulse,
  lucideLandmark,
  lucideMicroscope,
  lucideScale,
  lucideShieldCheck,
  lucideStethoscope,
  lucideUsers,
} from '@ng-icons/lucide';

/** Maps API `iconKey` (lowercase) → Ng Icons registration name. Extend as backend adds keys. */
const FRAMEWORK_ICON_KEY_MAP: Record<string, string> = {
  default: 'lucideShieldCheck',
  shield: 'lucideShieldCheck',
  compliance: 'lucideShieldCheck',
  building: 'lucideBuilding2',
  hospital: 'lucideBuilding2',
  facility: 'lucideBuilding2',
  lab: 'lucideMicroscope',
  laboratory: 'lucideMicroscope',
  nabl: 'lucideMicroscope',
  flask: 'lucideFlaskConical',
  blood: 'lucideDroplet',
  droplet: 'lucideDroplet',
  fire: 'lucideFlame',
  flame: 'lucideFlame',
  legal: 'lucideScale',
  justice: 'lucideScale',
  scale: 'lucideScale',
  document: 'lucideFileText',
  file: 'lucideFileText',
  clinical: 'lucideStethoscope',
  stethoscope: 'lucideStethoscope',
  pulse: 'lucideHeartPulse',
  heart: 'lucideHeartPulse',
  government: 'lucideLandmark',
  landmark: 'lucideLandmark',
  team: 'lucideUsers',
  users: 'lucideUsers',
  workforce: 'lucideBriefcase',
  wage: 'lucideBriefcase',
  activity: 'lucideActivity',
};

const DEFAULT_ICON_NAME = 'lucideShieldCheck';

const REGISTERED_ICON_NAMES = new Set<string>([
  DEFAULT_ICON_NAME,
  ...Object.values(FRAMEWORK_ICON_KEY_MAP),
]);

@Component({
  selector: 'app-framework-card-icon',
  standalone: true,
  imports: [NgIcon],
  providers: [
    provideIcons({
      lucideShieldCheck,
      lucideBuilding2,
      lucideMicroscope,
      lucideFlaskConical,
      lucideDroplet,
      lucideFlame,
      lucideScale,
      lucideFileText,
      lucideStethoscope,
      lucideHeartPulse,
      lucideLandmark,
      lucideUsers,
      lucideBriefcase,
      lucideActivity,
    }),
  ],
  template: `<ng-icon [name]="resolvedName" [size]="size" [strokeWidth]="strokeWidth" />`,
  styles: `
    :host {
      display: flex;
      align-items: center;
      justify-content: center;
      line-height: 0;
      color: inherit;
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FrameworkCardIconComponent {
  /** Value from API (`iconKey`, `icon`, etc.). Unknown/null uses default `lucideShieldCheck`. */
  @Input() iconKey: string | null | undefined;
  @Input() size = '1.75rem';
  @Input() strokeWidth = 1.75;

  get resolvedName(): string {
    const raw = (this.iconKey ?? '').trim();
    if (!raw) {
      return DEFAULT_ICON_NAME;
    }
    if (raw.startsWith('lucide') && REGISTERED_ICON_NAMES.has(raw)) {
      return raw;
    }
    const mapped = FRAMEWORK_ICON_KEY_MAP[raw.toLowerCase()];
    if (mapped) {
      return mapped;
    }
    return DEFAULT_ICON_NAME;
  }
}
