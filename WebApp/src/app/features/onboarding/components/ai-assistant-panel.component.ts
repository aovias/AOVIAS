import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

interface AIMessage {
  title: string;
  message: string;
  insights?: string[];
}

@Component({
  selector: 'app-ai-assistant-panel',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="ai-assistant-panel">
      <div class="ai-header">
        <div class="ai-avatar">
          <svg width="40" height="40" viewBox="0 0 40 40" fill="none">
            <rect width="40" height="40" rx="20" fill="url(#gradient)"/>
            <path d="M20 10C14.477 10 10 14.477 10 20C10 25.523 14.477 30 20 30C25.523 30 30 25.523 30 20C30 14.477 25.523 10 20 10ZM20 12C24.418 12 28 15.582 28 20C28 24.418 24.418 28 20 28C15.582 28 12 24.418 12 20C12 15.582 15.582 12 20 12Z" fill="white"/>
            <circle cx="16" cy="18" r="2" fill="white"/>
            <circle cx="24" cy="18" r="2" fill="white"/>
            <path d="M15 23C15 23 17 25 20 25C23 25 25 23 25 23" stroke="white" stroke-width="2" stroke-linecap="round"/>
            <defs>
              <linearGradient id="gradient" x1="0" y1="0" x2="40" y2="40">
                <stop offset="0%" stop-color="#2f6bff"/>
                <stop offset="100%" stop-color="#2557d6"/>
              </linearGradient>
            </defs>
          </svg>
        </div>
        <div class="ai-title">
          <h3>PRAMAN AI Assistant</h3>
          <span class="ai-status">
            <span class="status-dot"></span>
            Active
          </span>
        </div>
      </div>

      <div class="ai-content">
        <div class="ai-message-card">
          <h4>{{ messageData.title }}</h4>
          <p>{{ messageData.message }}</p>

          <div *ngIf="messageData.insights && messageData.insights.length > 0" class="ai-insights">
            <div class="insights-header">
              <svg width="16" height="16" viewBox="0 0 16 16" fill="none">
                <path d="M8 0L9.5 5.5L15 7L9.5 8.5L8 14L6.5 8.5L1 7L6.5 5.5L8 0Z" fill="currentColor"/>
              </svg>
              <span>Key Insights</span>
            </div>
            <ul class="insights-list">
              <li *ngFor="let insight of messageData.insights">{{ insight }}</li>
            </ul>
          </div>
        </div>

        <div class="ai-tips">
          <div class="tip-icon">💡</div>
          <div class="tip-content">
            <strong>Tip:</strong> All information can be updated later from your settings dashboard.
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .ai-assistant-panel {
      height: 100%;
      display: flex;
      flex-direction: column;
      background: var(--color-surface, #ffffff);
      border-right: 1px solid var(--color-border, #e5e7eb);
    }

    [data-theme="dark"] .ai-assistant-panel {
      background: var(--color-surface, #1f2937);
      border-right-color: var(--color-border, #374151);
    }

    .ai-header {
      padding: 24px;
      border-bottom: 1px solid var(--color-border, #e5e7eb);
      display: flex;
      gap: 16px;
      align-items: center;
    }

    [data-theme="dark"] .ai-header {
      border-bottom-color: var(--color-border, #374151);
    }

    .ai-avatar {
      flex-shrink: 0;
      animation: pulse 2s ease-in-out infinite;
    }

    @keyframes pulse {
      0%, 100% { opacity: 1; }
      50% { opacity: 0.8; }
    }

    .ai-title h3 {
      margin: 0;
      font-size: 16px;
      font-weight: 600;
      color: var(--color-text-primary, #111827);
    }

    [data-theme="dark"] .ai-title h3 {
      color: var(--color-text-primary, #f9fafb);
    }

    .ai-status {
      display: flex;
      align-items: center;
      gap: 6px;
      font-size: 12px;
      color: var(--color-text-secondary, #6b7280);
      margin-top: 4px;
    }

    .status-dot {
      width: 6px;
      height: 6px;
      border-radius: 50%;
      background: #10b981;
      animation: blink 2s ease-in-out infinite;
    }

    @keyframes blink {
      0%, 100% { opacity: 1; }
      50% { opacity: 0.3; }
    }

    .ai-content {
      flex: 1;
      padding: 24px;
      overflow-y: auto;
      display: flex;
      flex-direction: column;
      gap: 24px;
    }

    .ai-message-card {
      background: var(--color-background, #f9fafb);
      border: 1px solid var(--color-border, #e5e7eb);
      border-radius: 12px;
      padding: 20px;
      animation: slideIn 0.3s ease;
    }

    [data-theme="dark"] .ai-message-card {
      background: var(--color-background, #111827);
      border-color: var(--color-border, #374151);
    }

    @keyframes slideIn {
      from {
        opacity: 0;
        transform: translateY(10px);
      }
      to {
        opacity: 1;
        transform: translateY(0);
      }
    }

    .ai-message-card h4 {
      margin: 0 0 12px 0;
      font-size: 16px;
      font-weight: 600;
      color: var(--color-text-primary, #111827);
    }

    [data-theme="dark"] .ai-message-card h4 {
      color: var(--color-text-primary, #f9fafb);
    }

    .ai-message-card p {
      margin: 0;
      font-size: 14px;
      line-height: 1.6;
      color: var(--color-text-secondary, #6b7280);
    }

    [data-theme="dark"] .ai-message-card p {
      color: var(--color-text-secondary, #9ca3af);
    }

    .ai-insights {
      margin-top: 16px;
      padding-top: 16px;
      border-top: 1px solid var(--color-border, #e5e7eb);
    }

    [data-theme="dark"] .ai-insights {
      border-top-color: var(--color-border, #374151);
    }

    .insights-header {
      display: flex;
      align-items: center;
      gap: 8px;
      font-size: 13px;
      font-weight: 600;
      color: var(--color-primary);
      margin-bottom: 12px;
    }

    .insights-list {
      margin: 0;
      padding-left: 20px;
      list-style: none;
    }

    .insights-list li {
      position: relative;
      font-size: 13px;
      line-height: 1.6;
      color: var(--color-text-secondary, #6b7280);
      margin-bottom: 8px;
      padding-left: 12px;
    }

    [data-theme="dark"] .insights-list li {
      color: var(--color-text-secondary, #9ca3af);
    }

    .insights-list li::before {
      content: "•";
      position: absolute;
      left: 0;
      color: var(--color-primary);
      font-weight: bold;
    }

    .ai-tips {
      display: flex;
      gap: 12px;
      padding: 16px;
      background: #fef3c7;
      border: 1px solid #fde68a;
      border-radius: 8px;
      margin-top: auto;
    }

    [data-theme="dark"] .ai-tips {
      background: #422006;
      border-color: #78350f;
    }

    .tip-icon {
      font-size: 20px;
      flex-shrink: 0;
    }

    .tip-content {
      flex: 1;
      font-size: 13px;
      line-height: 1.5;
      color: #92400e;
    }

    [data-theme="dark"] .tip-content {
      color: #fde68a;
    }

    .tip-content strong {
      font-weight: 600;
    }

    @media (max-width: 768px) {
      .ai-assistant-panel {
        border-right: none;
        border-bottom: 1px solid var(--color-border, #e5e7eb);
      }

      [data-theme="dark"] .ai-assistant-panel {
        border-bottom-color: var(--color-border, #374151);
      }
    }
  `]
})
export class AiAssistantPanelComponent {
  @Input() messageData: AIMessage = {
    title: 'Welcome!',
    message: 'Let\'s get started with your compliance setup.'
  };
}
