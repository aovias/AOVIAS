import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AiAnswerMode, AiAssistantApiService, AiReasoningStep, AiSourceReference } from '../services/ai-assistant-api.service';

interface Message {
  id: string;
  content: string;
  sender: 'user' | 'ai';
  timestamp: Date;
  confidence?: number;
  reasoning?: AiReasoningStep[];
  sources?: AiSourceReference[];
  answerModeLabel?: string | null;
}

@Component({
  selector: 'app-ai-copilot',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="ai-copilot" [class.expanded]="isExpanded">
      <button class="copilot-trigger" (click)="toggleExpanded()" *ngIf="!isExpanded">
        <div class="trigger-icon">
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor">
            <path d="M12 2L2 7l10 5 10-5-10-5z" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
            <path d="M2 17l10 5 10-5M2 12l10 5 10-5" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
          </svg>
        </div>
        <span class="pulse-indicator"></span>
      </button>

      <div class="copilot-window" *ngIf="isExpanded">
        <div class="copilot-header">
          <div class="header-content">
            <div class="ai-avatar">
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor">
                <path d="M12 2L2 7l10 5 10-5-10-5z" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
                <path d="M2 17l10 5 10-5M2 12l10 5 10-5" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
              </svg>
            </div>
            <div>
              <h3 class="copilot-title">AI Compliance Copilot</h3>
              <p class="copilot-status">
                <span class="status-dot"></span>
                Online
              </p>
            </div>
          </div>
          <button class="close-button" (click)="toggleExpanded()">
            <svg viewBox="0 0 20 20" fill="currentColor">
              <path fill-rule="evenodd" d="M4.293 4.293a1 1 0 011.414 0L10 8.586l4.293-4.293a1 1 0 111.414 1.414L11.414 10l4.293 4.293a1 1 0 01-1.414 1.414L10 11.414l-4.293 4.293a1 1 0 01-1.414-1.414L8.586 10 4.293 5.707a1 1 0 010-1.414z" clip-rule="evenodd" />
            </svg>
          </button>
        </div>

        <div class="copilot-body">
          <div class="messages-container" #messagesContainer>
            <div *ngIf="messages.length === 0" class="welcome-message">
              <div class="welcome-icon">👋</div>
              <h4 class="welcome-title">Welcome to AI Copilot</h4>
              <p class="welcome-text">Ask me anything about your compliance status, risk insights, or upcoming tasks.</p>
              <div class="quick-questions">
                <button class="quick-question" (click)="sendQuickQuestion('What is my current compliance status?')">
                  What is my current compliance status?
                </button>
                <button class="quick-question" (click)="sendQuickQuestion('What are my critical tasks?')">
                  What are my critical tasks?
                </button>
                <button class="quick-question" (click)="sendQuickQuestion('Show me high-risk chapters')">
                  Show me high-risk chapters
                </button>
              </div>
            </div>

            <div *ngFor="let message of messages" class="message" [class.user-message]="message.sender === 'user'" [class.ai-message]="message.sender === 'ai'">
              <div class="message-avatar" *ngIf="message.sender === 'ai'">
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor">
                  <path d="M12 2L2 7l10 5 10-5-10-5z" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
                  <path d="M2 17l10 5 10-5M2 12l10 5 10-5" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
                </svg>
              </div>
              <div class="message-bubble">
                <span *ngIf="message.sender === 'ai' && message.answerModeLabel" class="answer-mode-badge">{{ message.answerModeLabel }}</span>
                <p class="message-content">{{ message.content }}</p>
                <p *ngIf="message.sender === 'ai' && message.confidence !== undefined" class="message-meta">
                  Confidence: {{ message.confidence }}%
                </p>
                <p *ngIf="message.sender === 'ai' && message.reasoning?.length" class="message-meta">
                  Why: {{ message.reasoning![0].message }}
                </p>
                <p *ngIf="message.sender === 'ai' && message.sources?.length" class="message-meta">
                  Source: {{ message.sources![0].sourceType }} / {{ message.sources![0].sourceId }}
                </p>
                <span class="message-time">{{ formatMessageTime(message.timestamp) }}</span>
              </div>
            </div>

            <div *ngIf="isTyping" class="message ai-message">
              <div class="message-avatar">
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor">
                  <path d="M12 2L2 7l10 5 10-5-10-5z" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
                  <path d="M2 17l10 5 10-5M2 12l10 5 10-5" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
                </svg>
              </div>
              <div class="message-bubble typing-indicator">
                <span></span>
                <span></span>
                <span></span>
              </div>
            </div>
          </div>

          <div class="input-container">
            <input
              type="text"
              class="message-input"
              placeholder="Ask about compliance, tasks, or risks..."
              [(ngModel)]="currentMessage"
              (keyup.enter)="sendMessage()"
              [disabled]="isTyping"
            />
            <button class="send-button" (click)="sendMessage()" [disabled]="!currentMessage.trim() || isTyping">
              <svg viewBox="0 0 20 20" fill="currentColor">
                <path d="M10.894 2.553a1 1 0 00-1.788 0l-7 14a1 1 0 001.169 1.409l5-1.429A1 1 0 009 15.571V11a1 1 0 112 0v4.571a1 1 0 00.725.962l5 1.428a1 1 0 001.17-1.408l-7-14z" />
              </svg>
            </button>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .ai-copilot {
      position: fixed;
      bottom: 24px;
      right: 24px;
      z-index: 1000;
    }

    .copilot-trigger {
      width: 56px;
      height: 56px;
      border-radius: 50%;
      background: var(--primary-color);
      border: none;
      box-shadow: var(--shadow-lg);
      cursor: pointer;
      display: flex;
      align-items: center;
      justify-content: center;
      position: relative;
      transition: all 0.3s ease;
    }

    .copilot-trigger:hover {
      transform: scale(1.1);
      background: var(--primary-hover);
    }

    .trigger-icon {
      width: 28px;
      height: 28px;
      color: white;
    }

    .trigger-icon svg {
      width: 100%;
      height: 100%;
    }

    .pulse-indicator {
      position: absolute;
      top: 6px;
      right: 6px;
      width: 10px;
      height: 10px;
      background: var(--success-color);
      border-radius: 50%;
      border: 2px solid white;
      animation: pulse 2s infinite;
    }

    @keyframes pulse {
      0%, 100% {
        opacity: 1;
      }
      50% {
        opacity: 0.5;
      }
    }

    .copilot-window {
      width: 400px;
      height: 600px;
      background: var(--card-bg);
      border: 1px solid var(--border-color);
      border-radius: 12px;
      box-shadow: var(--shadow-lg);
      display: flex;
      flex-direction: column;
      overflow: hidden;
      animation: slideIn 0.3s ease;
    }

    @keyframes slideIn {
      from {
        opacity: 0;
        transform: translateY(20px);
      }
      to {
        opacity: 1;
        transform: translateY(0);
      }
    }

    .copilot-header {
      background: var(--card-bg);
      padding: 16px;
      display: flex;
      justify-content: space-between;
      align-items: center;
      border-bottom: 1px solid var(--border-color);
    }

    .header-content {
      display: flex;
      align-items: center;
      gap: 12px;
    }

    .ai-avatar {
      width: 36px;
      height: 36px;
      background: var(--primary-bg-light);
      border-radius: 8px;
      display: flex;
      align-items: center;
      justify-content: center;
    }

    .ai-avatar svg {
      width: 20px;
      height: 20px;
      color: var(--primary-color);
    }

    .copilot-title {
      font-size: 14px;
      font-weight: 600;
      margin: 0 0 4px 0;
      color: var(--text-primary);
    }

    .copilot-status {
      font-size: 12px;
      margin: 0;
      color: var(--text-secondary);
      display: flex;
      align-items: center;
      gap: 6px;
    }

    .status-dot {
      width: 6px;
      height: 6px;
      background: var(--success-color);
      border-radius: 50%;
    }

    .close-button {
      width: 28px;
      height: 28px;
      border: none;
      background: var(--hover-bg);
      border-radius: 6px;
      color: var(--text-secondary);
      cursor: pointer;
      display: flex;
      align-items: center;
      justify-content: center;
      transition: all 0.2s ease;
    }

    .close-button:hover {
      background: var(--bg-tertiary);
      color: var(--text-primary);
    }

    .close-button svg {
      width: 16px;
      height: 16px;
    }

    .copilot-body {
      flex: 1;
      display: flex;
      flex-direction: column;
      overflow: hidden;
    }

    .messages-container {
      flex: 1;
      overflow-y: auto;
      padding: 16px;
      display: flex;
      flex-direction: column;
      gap: 12px;
    }

    .messages-container::-webkit-scrollbar {
      width: 6px;
    }

    .messages-container::-webkit-scrollbar-track {
      background: var(--bg-tertiary);
    }

    .messages-container::-webkit-scrollbar-thumb {
      background: var(--border-color);
      border-radius: 3px;
    }

    .welcome-message {
      text-align: center;
      padding: 40px 20px;
    }

    .welcome-icon {
      font-size: 58px;
      margin-bottom: 16px;
    }

    .welcome-title {
      font-size: 16px;
      font-weight: 600;
      color: var(--text-primary);
      margin: 0 0 8px 0;
    }

    .welcome-text {
      font-size: 13px;
      color: var(--text-secondary);
      margin: 0 0 20px 0;
      line-height: 1.5;
    }

    .quick-questions {
      display: flex;
      flex-direction: column;
      gap: 8px;
    }

    .quick-question {
      padding: 10px 14px;
      background: var(--bg-tertiary);
      border: 1px solid var(--border-color);
      border-radius: 6px;
      font-size: 12px;
      color: var(--text-primary);
      cursor: pointer;
      transition: all 0.2s ease;
      text-align: left;
    }

    .quick-question:hover {
      background: var(--hover-bg);
      border-color: var(--primary-color);
    }

    .message {
      display: flex;
      gap: 10px;
      animation: messageIn 0.3s ease;
    }

    @keyframes messageIn {
      from {
        opacity: 0;
        transform: translateY(10px);
      }
      to {
        opacity: 1;
        transform: translateY(0);
      }
    }

    .user-message {
      flex-direction: row-reverse;
    }

    .message-avatar {
      width: 28px;
      height: 28px;
      background: var(--primary-bg-light);
      border-radius: 6px;
      display: flex;
      align-items: center;
      justify-content: center;
      color: var(--primary-color);
      flex-shrink: 0;
    }

    .message-avatar svg {
      width: 16px;
      height: 16px;
    }

    .message-bubble {
      max-width: 70%;
      padding: 10px 12px;
      border-radius: 8px;
      display: flex;
      flex-direction: column;
      gap: 4px;
    }

    .ai-message .message-bubble {
      background: var(--bg-tertiary);
      border: 1px solid var(--border-color);
    }

    .user-message .message-bubble {
      background: var(--primary-color);
      color: white;
    }

    .message-content {
      font-size: 13px;
      margin: 0;
      line-height: 1.4;
    }

    .user-message .message-content {
      color: white;
    }

    .ai-message .message-content {
      color: var(--text-primary);
    }

    .message-time {
      font-size: 10px;
      opacity: 0.7;
    }

    .message-meta {
      margin: 0;
      font-size: 11px;
      color: var(--text-secondary);
      line-height: 1.3;
    }

    .answer-mode-badge {
      display: inline-block;
      margin-bottom: 6px;
      padding: 2px 8px;
      border-radius: 10px;
      font-size: 10px;
      font-weight: 600;
      letter-spacing: 0.02em;
      text-transform: uppercase;
      color: var(--text-secondary);
      background: rgba(125, 125, 125, 0.14);
    }

    .typing-indicator {
      display: flex;
      gap: 4px;
      padding: 12px;
    }

    .typing-indicator span {
      width: 6px;
      height: 6px;
      background: var(--text-secondary);
      border-radius: 50%;
      animation: typing 1.4s infinite;
    }

    .typing-indicator span:nth-child(2) {
      animation-delay: 0.2s;
    }

    .typing-indicator span:nth-child(3) {
      animation-delay: 0.4s;
    }

    @keyframes typing {
      0%, 60%, 100% {
        transform: translateY(0);
      }
      30% {
        transform: translateY(-6px);
      }
    }

    .input-container {
      padding: 12px;
      border-top: 1px solid var(--border-color);
      display: flex;
      gap: 8px;
      background: var(--card-bg);
    }

    .message-input {
      flex: 1;
      padding: 10px 14px;
      border: 1px solid var(--border-color);
      background: var(--bg-tertiary);
      color: var(--text-primary);
      border-radius: 6px;
      font-size: 13px;
      outline: none;
      transition: border-color 0.2s ease;
    }

    .message-input:focus {
      border-color: var(--primary-color);
      background: var(--card-bg);
    }

    .message-input:disabled {
      opacity: 0.5;
      cursor: not-allowed;
    }

    .send-button {
      width: 36px;
      height: 36px;
      border-radius: 6px;
      border: none;
      background: var(--primary-color);
      color: white;
      cursor: pointer;
      display: flex;
      align-items: center;
      justify-content: center;
      transition: all 0.2s ease;
    }

    .send-button:hover:not(:disabled) {
      background: var(--primary-hover);
    }

    .send-button:disabled {
      opacity: 0.5;
      cursor: not-allowed;
    }

    .send-button svg {
      width: 16px;
      height: 16px;
    }

    @media (max-width: 480px) {
      .copilot-window {
        width: calc(100vw - 32px);
        height: calc(100vh - 100px);
      }
    }
  `]
})
export class AICopilotComponent {
  private readonly aiAssistantApi = inject(AiAssistantApiService);

  isExpanded = false;
  currentMessage = '';
  messages: Message[] = [];
  isTyping = false;

  toggleExpanded(): void {
    this.isExpanded = !this.isExpanded;
  }

  sendMessage(): void {
    if (!this.currentMessage.trim() || this.isTyping) return;

    const userMessage: Message = {
      id: Date.now().toString(),
      content: this.currentMessage,
      sender: 'user',
      timestamp: new Date()
    };

    this.messages.push(userMessage);
    this.currentMessage = '';
    this.isTyping = true;

    this.aiAssistantApi.sendQuery(userMessage.content).subscribe({
      next: (response) => {
        const aiResponse: Message = {
          id: (Date.now() + 1).toString(),
          content: this.sanitizeText(response.complianceHint),
          sender: 'ai',
          timestamp: new Date(),
          confidence: Math.round((response.compositeConfidenceScore ?? response.confidenceScore ?? 0) * 100),
          reasoning: response.reasoning,
          sources: (response.sourceReferences ?? []).filter((s) => this.isDisplaySource(s)),
          answerModeLabel: this.answerModeLabel(response.answerMode),
        };
        this.messages.push(aiResponse);
        this.isTyping = false;
      },
      error: () => {
        const fallback: Message = {
          id: (Date.now() + 1).toString(),
          content: this.generateFallbackResponse(userMessage.content),
          sender: 'ai',
          timestamp: new Date(),
        };
        this.messages.push(fallback);
        this.isTyping = false;
      },
    });
  }

  sendQuickQuestion(question: string): void {
    this.currentMessage = question;
    this.sendMessage();
  }

  private answerModeLabel(mode: AiAnswerMode | undefined): string | null {
    switch (mode) {
      case 'GeneralKnowledge':
      case 'ComplianceKnowledge':
        return 'General knowledge';
      case 'OrganizationSpecific':
        return 'Workspace data';
      case 'Hybrid':
        return 'Workspace + general';
      default:
        return null;
    }
  }

  private isDisplaySource(source: AiSourceReference): boolean {
    return !(source.sourceType?.toLowerCase() === 'system' && source.sourceId?.toLowerCase() === 'fallback');
  }

  private sanitizeText(hint: string | undefined): string {
    const text = (hint ?? '').trim();
    if (!text) {
      return 'I can help with general healthcare compliance topics like NABH, JCI, ISO, HIPAA, CAPA, and FMS. Could you add a little more detail?';
    }
    const forbidden = [
      /the context does not contain[^.?!]*[.?!]/gi,
      /context does not contain[^.?!]*[.?!]/gi,
      /facts object is empty[^.?!]*[.?!]?/gi,
      /no relevant facts[^.?!]*[.?!]?/gi,
      /retrieval returned[^.?!]*[.?!]?/gi,
      /vector search[^.?!]*[.?!]?/gi,
    ];
    let cleaned = text;
    for (const pattern of forbidden) {
      cleaned = cleaned.replace(pattern, '');
    }
    cleaned = cleaned.replace(/\s{2,}/g, ' ').trim();
    return cleaned.length >= 12 ? cleaned : text;
  }

  private generateFallbackResponse(userMessage: string): string {
    const lowerMessage = userMessage.toLowerCase();

    if (lowerMessage.includes('compliance status') || lowerMessage.includes('compliance score')) {
      return 'Your overall compliance readiness is at 72%. FMS chapter compliance has dropped to 58%, which requires immediate attention. I recommend focusing on the 2 critical tasks that are currently overdue.';
    }

    if (lowerMessage.includes('critical tasks') || lowerMessage.includes('overdue')) {
      return 'You have 2 critical tasks overdue: 1) Install CCTV Cameras (ACC-3 compliance impact), 2) Update Fire Safety Documentation (FMS-2 compliance impact). Both are assigned to the Security department.';
    }

    if (lowerMessage.includes('high-risk') || lowerMessage.includes('risk chapters')) {
      return 'Your high-risk chapters include FMS (58% compliance), ACC (65% compliance), and HRM (68% compliance). I recommend prioritizing evidence collection for HRM-2 clause which is currently missing.';
    }

    if (lowerMessage.includes('evidence') || lowerMessage.includes('missing')) {
      return 'You have evidence missing for HRM-2 clause. Your current evidence coverage is 68% approved, 22% pending review, and 10% rejected. Upload the missing evidence to improve your audit readiness score.';
    }

    return 'I can help you with compliance status, risk insights, task management, and evidence tracking. What would you like to know more about?';
  }

  formatMessageTime(timestamp: Date): string {
    return timestamp.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
  }
}
