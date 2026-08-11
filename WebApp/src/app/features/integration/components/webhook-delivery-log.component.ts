import { Component, input } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-webhook-delivery-log',
  standalone: true,
  imports: [CommonModule],
  template: `
    <ul class="webhook-delivery-log">
      @for (d of deliveries(); track d.id) {
        <li>{{ d.eventType }} — {{ d.status }} (attempts: {{ d.attemptCount }})</li>
      }
    </ul>
  `,
})
export class WebhookDeliveryLogComponent {
  deliveries = input<{ id: string; eventType: string; status: string; attemptCount: number }[]>([]);
}
