import {
  Component,
  ChangeDetectionStrategy,
  inject,
  computed,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { CopilotStore } from '../../state/copilot.store';
import { CopilotPersona, PERSONA_DISPLAY_NAMES } from '../../models/copilot-persona.model';

@Component({
  selector: 'app-persona-switcher',
  standalone: true,
  imports: [CommonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="persona-switcher">
      <label class="switcher-label" for="persona-select">Copilot Persona</label>
      <select
        id="persona-select"
        class="persona-select"
        [value]="selectedValue()"
        (change)="onSelect($event)"
      >
        <option value="auto">{{ autoLabel() }}</option>
        @for (p of personas; track p) {
          <option [value]="p">{{ displayName(p) }}</option>
        }
      </select>
    </div>
  `,
  styles: [
    `
      .persona-switcher {
        display: flex;
        flex-direction: column;
        gap: 6px;
        min-width: 220px;
      }
      .switcher-label {
        font-size: 11px;
        font-weight: 700;
        letter-spacing: 0.05em;
        text-transform: uppercase;
        color: var(--copilot-muted, var(--text-secondary));
      }
      .persona-select {
        width: 100%;
        padding: 10px 12px;
        border: 1px solid var(--copilot-border, var(--border-color));
        border-radius: 10px;
        background: var(--copilot-surface-elevated, var(--card-bg));
        color: var(--copilot-text, var(--text-primary));
        font-size: 14px;
        font-weight: 600;
        cursor: pointer;
      }
      .persona-select:focus-visible {
        outline: 2px solid var(--copilot-accent, var(--color-primary));
        outline-offset: 2px;
      }
    `,
  ],
})
export class PersonaSwitcherComponent {
  protected readonly store = inject(CopilotStore);

  readonly personas = Object.values(CopilotPersona);

  readonly selectedValue = computed(() =>
    this.store.personaOverride() ?? 'auto',
  );

  readonly autoLabel = computed(() => {
    const persona = this.store.effectivePersona();
    const name = PERSONA_DISPLAY_NAMES[persona];
    const suffix = this.store.personaOverride() ? '' : ' (Auto)';
    return `${name}${suffix}`;
  });

  displayName(p: CopilotPersona): string {
    return PERSONA_DISPLAY_NAMES[p];
  }

  onSelect(event: Event): void {
    const value = (event.target as HTMLSelectElement).value;
    if (value === 'auto') {
      this.store.clearPersonaOverride();
      return;
    }
    this.store.overridePersona(value as CopilotPersona);
  }
}
