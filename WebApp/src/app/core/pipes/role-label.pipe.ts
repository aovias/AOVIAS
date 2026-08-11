import { Pipe, PipeTransform, inject } from '@angular/core';
import { RoleRegistryService } from '../auth/roles/role-registry.service';

@Pipe({
  name: 'roleLabel',
  standalone: true,
})
export class RoleLabelPipe implements PipeTransform {
  private readonly registry = inject(RoleRegistryService);

  transform(value: string | null | undefined): string {
    return this.registry.labelOf(value);
  }
}
