import { CanDeactivateFn } from '@angular/router';

export interface SettingsDirtyComponent {
  isSettingsDirty(): boolean;
}

export const settingsDirtyGuard: CanDeactivateFn<SettingsDirtyComponent> = (component) => {
  if (!component?.isSettingsDirty?.()) {
    return true;
  }
  return window.confirm('You have unsaved changes. Leave this page without saving?');
};
