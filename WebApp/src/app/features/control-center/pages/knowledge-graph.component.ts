import { Component, signal, computed, effect, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

interface InsightCard {
  title: string;
  value: number;
  icon: string;
  color: string;
}

interface GraphNode {
  id: string;
  label: string;
  type: 'framework' | 'chapter' | 'clause' | 'task' | 'evidence' | 'finding' | 'department';
  x: number;
  y: number;
  connections: number;
  riskLevel: 'low' | 'medium' | 'high';
  metadata?: any;
}

interface GraphEdge {
  source: string;
  target: string;
  type: string;
}

interface ActivityEvent {
  id: string;
  user: string;
  action: string;
  target: string;
  timestamp: Date;
  icon: string;
}

interface AnalyticsData {
  label: string;
  value: number;
  color: string;
}

@Component({
  selector: 'app-knowledge-graph',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="knowledge-graph-page">
      <div class="page-header">
        <div class="header-content">
          <h1 class="page-title">Compliance Knowledge Graph</h1>
          <p class="page-description">
            Explore relationships between clauses, tasks, evidence, audit findings, and departments
            to understand compliance dependencies and risks.
          </p>
        </div>
      </div>

      <div class="insight-cards">
        @for (card of insightCards; track card.title) {
          <div class="insight-card" [style.border-left-color]="card.color">
            <div class="card-icon" [innerHTML]="card.icon" [style.color]="card.color"></div>
            <div class="card-content">
              <div class="card-value">{{ card.value }}</div>
              <div class="card-title">{{ card.title }}</div>
            </div>
          </div>
        }
      </div>

      <div class="graph-controls-section">
        <div class="filter-bar">
          <div class="search-box">
            <svg class="search-icon" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor">
              <circle cx="11" cy="11" r="8" stroke-width="2"/>
              <path d="m21 21-4.35-4.35" stroke-width="2" stroke-linecap="round"/>
            </svg>
            <input
              type="text"
              [(ngModel)]="searchQuery"
              placeholder="Search clauses, tasks, evidence..."
              class="search-input"
            />
          </div>
          <div class="filters">
            <select [(ngModel)]="selectedChapter" class="filter-select">
              <option value="">All Chapters</option>
              @for (chapter of chapters; track chapter) {
                <option [value]="chapter">{{ chapter }}</option>
              }
            </select>
            <select [(ngModel)]="selectedDepartment" class="filter-select">
              <option value="">All Departments</option>
              @for (dept of departments; track dept) {
                <option [value]="dept">{{ dept }}</option>
              }
            </select>
            <select [(ngModel)]="selectedRiskLevel" class="filter-select">
              <option value="">All Risk Levels</option>
              <option value="low">Low Risk</option>
              <option value="medium">Medium Risk</option>
              <option value="high">High Risk</option>
            </select>
            <select [(ngModel)]="selectedStatus" class="filter-select">
              <option value="">All Statuses</option>
              <option value="open">Open</option>
              <option value="in-progress">In Progress</option>
              <option value="completed">Completed</option>
            </select>
          </div>
        </div>

        <div class="graph-action-buttons">
          <button class="action-btn" (click)="expandRelated()">
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor">
              <circle cx="12" cy="12" r="3" stroke-width="2"/>
              <circle cx="12" cy="5" r="1" fill="currentColor"/>
              <circle cx="19" cy="12" r="1" fill="currentColor"/>
              <circle cx="12" cy="19" r="1" fill="currentColor"/>
              <circle cx="5" cy="12" r="1" fill="currentColor"/>
            </svg>
            Expand Related
          </button>
          <button class="action-btn" (click)="showUpstream()">
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor">
              <path d="M12 19V5M5 12l7-7 7 7" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
            </svg>
            Upstream
          </button>
          <button class="action-btn" (click)="showDownstream()">
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor">
              <path d="M12 5v14M5 12l7 7 7-7" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
            </svg>
            Downstream
          </button>
          <button class="action-btn secondary" (click)="resetGraph()">
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor">
              <path d="M1 4v6h6M23 20v-6h-6" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
              <path d="M20.49 9A9 9 0 0 0 5.64 5.64L1 10m22 4l-4.64 4.36A9 9 0 0 1 3.51 15" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
            </svg>
            Reset
          </button>
        </div>
      </div>

      <div class="main-content">
        <div class="graph-container" [class.with-panel]="selectedNode()">
          <div class="graph-canvas" #graphCanvas>
            <svg class="graph-svg" [attr.viewBox]="viewBox()" (wheel)="handleZoom($event)">
              <defs>
                <marker id="arrowhead" markerWidth="10" markerHeight="10" refX="9" refY="3" orient="auto">
                  <polygon points="0 0, 10 3, 0 6" fill="var(--edge-color)" />
                </marker>
              </defs>
              <g [attr.transform]="'translate(' + pan().x + ',' + pan().y + ') scale(' + zoom() + ')'">
                @for (edge of edges; track edge.source + edge.target) {
                  <line
                    [attr.x1]="getNode(edge.source)?.x"
                    [attr.y1]="getNode(edge.source)?.y"
                    [attr.x2]="getNode(edge.target)?.x"
                    [attr.y2]="getNode(edge.target)?.y"
                    class="graph-edge"
                    marker-end="url(#arrowhead)"
                  />
                }
                @for (node of filteredNodes(); track node.id) {
                  <g
                    class="graph-node"
                    [attr.transform]="'translate(' + node.x + ',' + node.y + ')'"
                    (click)="selectNode(node)"
                    [class.selected]="selectedNode()?.id === node.id"
                  >
                    <circle
                      [attr.r]="nodeRadius(node)"
                      [class]="'node-' + node.type"
                      [class.risk-high]="node.riskLevel === 'high'"
                      [class.risk-medium]="node.riskLevel === 'medium'"
                      [class.risk-low]="node.riskLevel === 'low'"
                    />
                    <text class="node-label" dy=".35em" text-anchor="middle">
                      {{ node.label }}
                    </text>
                    @if (node.connections > 5) {
                      <circle class="connection-badge" cx="15" cy="-15" r="10" />
                      <text class="connection-count" x="15" y="-15" dy=".35em" text-anchor="middle">
                        {{ node.connections }}
                      </text>
                    }
                  </g>
                }
              </g>
            </svg>
            <div class="graph-legend">
              <div class="legend-title">Node Types</div>
              @for (type of nodeTypes; track type.name) {
                <div class="legend-item">
                  <div class="legend-color" [style.background-color]="type.color"></div>
                  <span>{{ type.name }}</span>
                </div>
              }
            </div>
          </div>
        </div>

        @if (selectedNode()) {
          <div class="detail-panel">
            <div class="panel-header">
              <h3 class="panel-title">{{ selectedNode()!.label }}</h3>
              <button class="close-btn" (click)="closePanel()">
                <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor">
                  <path d="M18 6L6 18M6 6l12 12" stroke-width="2" stroke-linecap="round"/>
                </svg>
              </button>
            </div>
            <div class="panel-content">
              <div class="node-type-badge" [class]="'badge-' + selectedNode()!.type">
                {{ selectedNode()!.type }}
              </div>

              @if (selectedNode()!.type === 'clause') {
                <div class="detail-section">
                  <div class="detail-label">Clause Code</div>
                  <div class="detail-value">{{ selectedNode()!.metadata?.code || 'ACC-1.2.3' }}</div>
                </div>
                <div class="detail-section">
                  <div class="detail-label">Description</div>
                  <div class="detail-value">{{ selectedNode()!.metadata?.description || 'Access control policies must be documented and approved' }}</div>
                </div>
                <div class="detail-section">
                  <div class="detail-label">Risk Level</div>
                  <div class="risk-badge" [class]="'risk-' + selectedNode()!.riskLevel">
                    {{ selectedNode()!.riskLevel }}
                  </div>
                </div>
              }

              @if (selectedNode()!.type === 'task') {
                <div class="detail-section">
                  <div class="detail-label">Owner</div>
                  <div class="detail-value">{{ selectedNode()!.metadata?.owner || 'Rajesh Kumar' }}</div>
                </div>
                <div class="detail-section">
                  <div class="detail-label">Department</div>
                  <div class="detail-value">{{ selectedNode()!.metadata?.department || 'Security' }}</div>
                </div>
                <div class="detail-section">
                  <div class="detail-label">Due Date</div>
                  <div class="detail-value">{{ selectedNode()!.metadata?.dueDate || '2024-04-15' }}</div>
                </div>
                <div class="detail-section">
                  <div class="detail-label">Status</div>
                  <div class="status-badge" [class]="'status-' + (selectedNode()!.metadata?.status || 'in-progress')">
                    {{ selectedNode()!.metadata?.status || 'in-progress' }}
                  </div>
                </div>
              }

              @if (selectedNode()!.type === 'evidence') {
                <div class="detail-section">
                  <div class="detail-label">File Name</div>
                  <div class="detail-value">{{ selectedNode()!.metadata?.fileName || 'access-policy-v2.pdf' }}</div>
                </div>
                <div class="detail-section">
                  <div class="detail-label">Upload Date</div>
                  <div class="detail-value">{{ selectedNode()!.metadata?.uploadDate || '2024-03-20' }}</div>
                </div>
                <div class="detail-section">
                  <div class="detail-label">Approval Status</div>
                  <div class="status-badge status-approved">
                    {{ selectedNode()!.metadata?.approvalStatus || 'Approved' }}
                  </div>
                </div>
              }

              <div class="impact-analysis">
                <h4 class="impact-title">Impact Analysis</h4>
                <div class="impact-grid">
                  <div class="impact-card">
                    <div class="impact-value">{{ selectedNode()!.metadata?.linkedTasks || 3 }}</div>
                    <div class="impact-label">Linked Tasks</div>
                  </div>
                  <div class="impact-card">
                    <div class="impact-value">{{ selectedNode()!.metadata?.evidenceDocs || 2 }}</div>
                    <div class="impact-label">Evidence Docs</div>
                  </div>
                  <div class="impact-card">
                    <div class="impact-value">{{ selectedNode()!.metadata?.openFindings || 1 }}</div>
                    <div class="impact-label">Open Findings</div>
                  </div>
                  <div class="impact-card">
                    <div class="impact-value">{{ selectedNode()!.metadata?.department || 'Security' }}</div>
                    <div class="impact-label">Department</div>
                  </div>
                </div>
              </div>

              <div class="connections-section">
                <h4 class="connections-title">Connections ({{ selectedNode()!.connections }})</h4>
                <div class="connections-list">
                  @for (conn of getConnections(selectedNode()!); track conn) {
                    <div class="connection-item">
                      <div class="connection-type">{{ conn.type }}</div>
                      <div class="connection-target">{{ conn.target }}</div>
                    </div>
                  }
                </div>
              </div>
            </div>
          </div>
        }
      </div>

      <div class="analytics-section">
        <h2 class="section-title">Graph Analytics</h2>
        <div class="analytics-grid">
          <div class="analytics-card">
            <h3 class="analytics-title">Most Connected Clauses</h3>
            <div class="chart-container">
              @for (item of topClauses; track item.label) {
                <div class="chart-bar">
                  <div class="bar-label">{{ item.label }}</div>
                  <div class="bar-wrapper">
                    <div class="bar-fill" [style.width.%]="(item.value / maxClauseConnections) * 100" [style.background-color]="item.color"></div>
                    <div class="bar-value">{{ item.value }}</div>
                  </div>
                </div>
              }
            </div>
          </div>

          <div class="analytics-card">
            <h3 class="analytics-title">Departments with Most Tasks</h3>
            <div class="chart-container">
              @for (item of departmentTasks; track item.label) {
                <div class="chart-bar">
                  <div class="bar-label">{{ item.label }}</div>
                  <div class="bar-wrapper">
                    <div class="bar-fill" [style.width.%]="(item.value / maxDepartmentTasks) * 100" [style.background-color]="item.color"></div>
                    <div class="bar-value">{{ item.value }}</div>
                  </div>
                </div>
              }
            </div>
          </div>

          <div class="analytics-card">
            <h3 class="analytics-title">Evidence Coverage by Chapter</h3>
            <div class="chart-container">
              @for (item of evidenceCoverage; track item.label) {
                <div class="chart-bar">
                  <div class="bar-label">{{ item.label }}</div>
                  <div class="bar-wrapper">
                    <div class="bar-fill" [style.width.%]="item.value" [style.background-color]="item.color"></div>
                    <div class="bar-value">{{ item.value }}%</div>
                  </div>
                </div>
              }
            </div>
          </div>

          <div class="analytics-card">
            <h3 class="analytics-title">Findings by Chapter</h3>
            <div class="chart-container">
              @for (item of findingsByChapter; track item.label) {
                <div class="chart-bar">
                  <div class="bar-label">{{ item.label }}</div>
                  <div class="bar-wrapper">
                    <div class="bar-fill" [style.width.%]="(item.value / maxFindings) * 100" [style.background-color]="item.color"></div>
                    <div class="bar-value">{{ item.value }}</div>
                  </div>
                </div>
              }
            </div>
          </div>
        </div>
      </div>

      <div class="activity-section">
        <h2 class="section-title">Activity Stream</h2>
        <div class="activity-stream">
          @for (event of activities; track event.id) {
            <div class="activity-item">
              <div class="activity-icon" [innerHTML]="event.icon"></div>
              <div class="activity-content">
                <div class="activity-text">
                  <span class="activity-user">{{ event.user }}</span>
                  <span class="activity-action">{{ event.action }}</span>
                  <span class="activity-target">{{ event.target }}</span>
                </div>
                <div class="activity-time">{{ formatTime(event.timestamp) }}</div>
              </div>
            </div>
          }
        </div>
      </div>
    </div>
  `,
  styles: [`
    .knowledge-graph-page {
      padding: var(--page-padding-block-start) var(--page-padding-inline) var(--page-padding-block-end);
      background: var(--bg-primary);
      min-height: calc(100vh - 64px);
    }

    .page-header {
      margin-bottom: 24px;
    }

    .header-content {
      max-width: 800px;
    }

    .page-title {
      font-size: var(--font-size-page-title);
      font-weight: 600;
      color: var(--text-primary);
      margin: 0 0 8px 0;
    }

    .page-description {
      font-size: var(--font-size-subtitle);
      color: var(--text-secondary);
      margin: 0;
      line-height: 1.5;
    }

    .insight-cards {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
      gap: 16px;
      margin-bottom: 24px;
    }

    .insight-card {
      background: var(--card-bg);
      border: 1px solid var(--border-color);
      border-left: 3px solid;
      border-radius: 8px;
      padding: 20px;
      display: flex;
      align-items: center;
      gap: 16px;
      transition: all 0.2s ease;
    }

    .insight-card:hover {
      transform: translateY(-2px);
      box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
    }

    .card-icon {
      width: 40px;
      height: 40px;
      display: flex;
      align-items: center;
      justify-content: center;
      font-size: 24px;
    }

    .card-content {
      flex: 1;
    }

    .card-value {
      font-size: 24px;
      font-weight: 700;
      color: var(--text-primary);
      margin-bottom: 4px;
    }

    .card-title {
      font-size: 12px;
      font-weight: 500;
      color: var(--text-secondary);
      text-transform: uppercase;
      letter-spacing: 0.5px;
    }

    .graph-controls-section {
      background: var(--card-bg);
      border: 1px solid var(--border-color);
      border-radius: 8px;
      padding: 20px;
      margin-bottom: 24px;
    }

    .filter-bar {
      display: flex;
      gap: 16px;
      margin-bottom: 16px;
      flex-wrap: wrap;
    }

    .search-box {
      flex: 1;
      min-width: 250px;
      position: relative;
    }

    .search-icon {
      position: absolute;
      left: 12px;
      top: 50%;
      transform: translateY(-50%);
      color: var(--text-secondary);
      pointer-events: none;
    }

    .search-input {
      width: 100%;
      padding: 10px 12px 10px 40px;
      background: var(--bg-primary);
      border: 1px solid var(--border-color);
      border-radius: 6px;
      color: var(--text-primary);
      font-size: 14px;
      transition: all 0.2s ease;
    }

    .search-input:focus {
      outline: none;
      border-color: var(--primary-color);
      box-shadow: 0 0 0 3px rgba(59, 130, 246, 0.1);
    }

    .filters {
      display: flex;
      gap: 12px;
      flex-wrap: wrap;
    }

    .filter-select {
      padding: 10px 32px 10px 12px;
      background: var(--bg-primary);
      border: 1px solid var(--border-color);
      border-radius: 6px;
      color: var(--text-primary);
      font-size: 14px;
      cursor: pointer;
      transition: all 0.2s ease;
      appearance: none;
      background-image: url("data:image/svg+xml,%3Csvg width='12' height='12' viewBox='0 0 12 12' fill='none' xmlns='http://www.w3.org/2000/svg'%3E%3Cpath d='M3 4.5L6 7.5L9 4.5' stroke='%23666' stroke-width='1.5' stroke-linecap='round' stroke-linejoin='round'/%3E%3C/svg%3E");
      background-repeat: no-repeat;
      background-position: right 12px center;
    }

    .filter-select:focus {
      outline: none;
      border-color: var(--primary-color);
      box-shadow: 0 0 0 3px rgba(59, 130, 246, 0.1);
    }

    .graph-action-buttons {
      display: flex;
      gap: 12px;
      flex-wrap: wrap;
    }

    .action-btn {
      display: flex;
      align-items: center;
      gap: 8px;
      padding: 8px 16px;
      background: var(--primary-color);
      color: white;
      border: none;
      border-radius: 6px;
      font-size: 14px;
      font-weight: 500;
      cursor: pointer;
      transition: all 0.2s ease;
    }

    .action-btn:hover {
      background: var(--primary-hover);
      transform: translateY(-1px);
    }

    .action-btn.secondary {
      background: var(--bg-primary);
      color: var(--text-primary);
      border: 1px solid var(--border-color);
    }

    .action-btn.secondary:hover {
      background: var(--hover-bg);
    }

    .main-content {
      display: grid;
      grid-template-columns: 1fr;
      gap: 24px;
      margin-bottom: 24px;
    }

    .main-content.with-panel {
      grid-template-columns: 1fr 400px;
    }

    .graph-container {
      background: var(--card-bg);
      border: 1px solid var(--border-color);
      border-radius: 8px;
      overflow: hidden;
      min-height: 600px;
      position: relative;
    }

    .graph-canvas {
      width: 100%;
      height: 600px;
      position: relative;
    }

    .graph-svg {
      width: 100%;
      height: 100%;
      cursor: grab;
    }

    .graph-svg:active {
      cursor: grabbing;
    }

    .graph-edge {
      stroke: var(--edge-color, rgba(100, 100, 100, 0.3));
      stroke-width: 2;
      fill: none;
    }

    .graph-node {
      cursor: pointer;
      transition: all 0.3s ease;
    }

    .graph-node:hover circle {
      filter: brightness(1.2);
      stroke-width: 3;
    }

    .graph-node.selected circle {
      stroke: var(--primary-color);
      stroke-width: 4;
    }

    .graph-node circle {
      stroke-width: 2;
      stroke: rgba(255, 255, 255, 0.2);
      transition: all 0.3s ease;
    }

    .node-framework { fill: var(--color-chart-chapter-trend); }
    .node-chapter { fill: #3b82f6; }
    .node-clause { fill: #0ea5e9; }
    .node-task { fill: #f97316; }
    .node-evidence { fill: #10b981; }
    .node-finding { fill: #ef4444; }
    .node-department { fill: #2f6bff; }

    .risk-high { stroke: #ef4444 !important; stroke-width: 3px !important; }
    .risk-medium { stroke: #f59e0b !important; stroke-width: 3px !important; }
    .risk-low { stroke: #10b981 !important; stroke-width: 3px !important; }

    .node-label {
      font-size: 11px;
      font-weight: 600;
      fill: white;
      pointer-events: none;
      user-select: none;
    }

    .connection-badge {
      fill: var(--primary-color);
      stroke: white;
      stroke-width: 2;
    }

    .connection-count {
      font-size: 10px;
      font-weight: 700;
      fill: white;
      pointer-events: none;
    }

    .graph-legend {
      position: absolute;
      bottom: 16px;
      left: 16px;
      background: var(--card-bg);
      border: 1px solid var(--border-color);
      border-radius: 8px;
      padding: 12px;
      box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
    }

    .legend-title {
      font-size: 11px;
      font-weight: 600;
      color: var(--text-primary);
      margin-bottom: 8px;
      text-transform: uppercase;
      letter-spacing: 0.5px;
    }

    .legend-item {
      display: flex;
      align-items: center;
      gap: 8px;
      margin-bottom: 6px;
      font-size: 12px;
      color: var(--text-secondary);
    }

    .legend-item:last-child {
      margin-bottom: 0;
    }

    .legend-color {
      width: 12px;
      height: 12px;
      border-radius: 50%;
      border: 2px solid rgba(255, 255, 255, 0.2);
    }

    .detail-panel {
      background: var(--card-bg);
      border: 1px solid var(--border-color);
      border-radius: 8px;
      overflow: hidden;
      max-height: 600px;
      display: flex;
      flex-direction: column;
    }

    .panel-header {
      padding: 20px;
      border-bottom: 1px solid var(--border-color);
      display: flex;
      align-items: center;
      justify-content: space-between;
    }

    .panel-title {
      font-size: 18px;
      font-weight: 600;
      color: var(--text-primary);
      margin: 0;
    }

    .close-btn {
      background: transparent;
      border: none;
      color: var(--text-secondary);
      cursor: pointer;
      padding: 4px;
      border-radius: 4px;
      transition: all 0.2s ease;
    }

    .close-btn:hover {
      background: var(--hover-bg);
      color: var(--text-primary);
    }

    .panel-content {
      padding: 20px;
      overflow-y: auto;
    }

    .node-type-badge {
      display: inline-block;
      padding: 4px 12px;
      border-radius: 12px;
      font-size: 11px;
      font-weight: 600;
      text-transform: uppercase;
      letter-spacing: 0.5px;
      margin-bottom: 20px;
    }

    .badge-clause { background: rgba(14, 165, 233, 0.2); color: #0ea5e9; }
    .badge-task { background: rgba(249, 115, 22, 0.2); color: #f97316; }
    .badge-evidence { background: rgba(16, 185, 129, 0.2); color: #10b981; }
    .badge-finding { background: rgba(239, 68, 68, 0.2); color: #ef4444; }
    .badge-department { background: var(--color-primary-background); color: #2f6bff; }

    .detail-section {
      margin-bottom: 16px;
    }

    .detail-label {
      font-size: 12px;
      font-weight: 600;
      color: var(--text-secondary);
      margin-bottom: 6px;
      text-transform: uppercase;
      letter-spacing: 0.5px;
    }

    .detail-value {
      font-size: 14px;
      color: var(--text-primary);
      line-height: 1.5;
    }

    .risk-badge {
      display: inline-block;
      padding: 4px 12px;
      border-radius: 12px;
      font-size: 12px;
      font-weight: 600;
      text-transform: capitalize;
    }

    .risk-high { background: rgba(239, 68, 68, 0.2); color: #ef4444; }
    .risk-medium { background: rgba(245, 158, 11, 0.2); color: #f59e0b; }
    .risk-low { background: rgba(16, 185, 129, 0.2); color: #10b981; }

    .status-badge {
      display: inline-block;
      padding: 4px 12px;
      border-radius: 12px;
      font-size: 12px;
      font-weight: 600;
      text-transform: capitalize;
    }

    .status-in-progress { background: rgba(59, 130, 246, 0.2); color: #3b82f6; }
    .status-approved { background: rgba(16, 185, 129, 0.2); color: #10b981; }

    .impact-analysis {
      margin-top: 24px;
      padding-top: 24px;
      border-top: 1px solid var(--border-color);
    }

    .impact-title {
      font-size: 14px;
      font-weight: 600;
      color: var(--text-primary);
      margin: 0 0 16px 0;
    }

    .impact-grid {
      display: grid;
      grid-template-columns: repeat(2, 1fr);
      gap: 12px;
    }

    .impact-card {
      background: var(--bg-primary);
      border: 1px solid var(--border-color);
      border-radius: 6px;
      padding: 12px;
      text-align: center;
    }

    .impact-value {
      font-size: 20px;
      font-weight: 700;
      color: var(--text-primary);
      margin-bottom: 4px;
    }

    .impact-label {
      font-size: 11px;
      color: var(--text-secondary);
      text-transform: uppercase;
      letter-spacing: 0.5px;
    }

    .connections-section {
      margin-top: 24px;
      padding-top: 24px;
      border-top: 1px solid var(--border-color);
    }

    .connections-title {
      font-size: 14px;
      font-weight: 600;
      color: var(--text-primary);
      margin: 0 0 12px 0;
    }

    .connections-list {
      display: flex;
      flex-direction: column;
      gap: 8px;
    }

    .connection-item {
      padding: 8px 12px;
      background: var(--bg-primary);
      border: 1px solid var(--border-color);
      border-radius: 6px;
      display: flex;
      justify-content: space-between;
      align-items: center;
      font-size: 12px;
    }

    .connection-type {
      color: var(--text-secondary);
      font-weight: 500;
    }

    .connection-target {
      color: var(--text-primary);
      font-weight: 600;
    }

    .analytics-section {
      margin-bottom: 24px;
    }

    .section-title {
      font-size: 20px;
      font-weight: 600;
      color: var(--text-primary);
      margin: 0 0 16px 0;
    }

    .analytics-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(300px, 1fr));
      gap: 24px;
    }

    .analytics-card {
      background: var(--card-bg);
      border: 1px solid var(--border-color);
      border-radius: 8px;
      padding: 20px;
    }

    .analytics-title {
      font-size: 16px;
      font-weight: 600;
      color: var(--text-primary);
      margin: 0 0 16px 0;
    }

    .chart-container {
      display: flex;
      flex-direction: column;
      gap: 12px;
    }

    .chart-bar {
      display: flex;
      flex-direction: column;
      gap: 6px;
    }

    .bar-label {
      font-size: 13px;
      font-weight: 500;
      color: var(--text-primary);
    }

    .bar-wrapper {
      display: flex;
      align-items: center;
      gap: 12px;
      height: 28px;
    }

    .bar-fill {
      height: 100%;
      background: var(--primary-color);
      border-radius: 4px;
      transition: width 0.3s ease;
      position: relative;
    }

    .bar-value {
      font-size: 13px;
      font-weight: 600;
      color: var(--text-primary);
      min-width: 30px;
    }

    .activity-section {
      margin-bottom: 24px;
    }

    .activity-stream {
      background: var(--card-bg);
      border: 1px solid var(--border-color);
      border-radius: 8px;
      padding: 20px;
      display: flex;
      flex-direction: column;
      gap: 16px;
      max-height: 400px;
      overflow-y: auto;
    }

    .activity-item {
      display: flex;
      gap: 12px;
      padding-bottom: 16px;
      border-bottom: 1px solid var(--border-color);
    }

    .activity-item:last-child {
      border-bottom: none;
      padding-bottom: 0;
    }

    .activity-icon {
      width: 36px;
      height: 36px;
      background: var(--bg-primary);
      border: 1px solid var(--border-color);
      border-radius: 50%;
      display: flex;
      align-items: center;
      justify-content: center;
      color: var(--primary-color);
      flex-shrink: 0;
    }

    .activity-content {
      flex: 1;
    }

    .activity-text {
      font-size: 14px;
      color: var(--text-primary);
      line-height: 1.5;
      margin-bottom: 4px;
    }

    .activity-user {
      font-weight: 600;
      color: var(--primary-color);
    }

    .activity-action {
      color: var(--text-secondary);
    }

    .activity-target {
      font-weight: 500;
      color: var(--text-primary);
    }

    .activity-time {
      font-size: 12px;
      color: var(--text-secondary);
    }

    @media (max-width: 1024px) {
      .main-content.with-panel {
        grid-template-columns: 1fr;
      }

      .detail-panel {
        max-height: none;
      }
    }

    @media (max-width: 768px) {
      .knowledge-graph-page {
        padding: var(--page-padding-block-start) var(--page-padding-inline) var(--page-padding-block-end);
      }

      .insight-cards {
        grid-template-columns: 1fr;
      }

      .filter-bar {
        flex-direction: column;
      }

      .filters {
        flex-direction: column;
      }

      .filter-select {
        width: 100%;
      }

      .analytics-grid {
        grid-template-columns: 1fr;
      }
    }
  `]
})
export class KnowledgeGraphComponent implements OnInit, OnDestroy {
  searchQuery = '';
  selectedChapter = '';
  selectedDepartment = '';
  selectedRiskLevel = '';
  selectedStatus = '';

  selectedNode = signal<GraphNode | null>(null);
  zoom = signal(1);
  pan = signal({ x: 0, y: 0 });
  viewBox = signal('0 0 1200 600');

  chapters = ['ACC - Access Control', 'COP - Competence', 'AUD - Audit', 'INF - Infrastructure'];
  departments = ['Security', 'Admin', 'Quality', 'IT', 'HR', 'Clinical'];

  insightCards: InsightCard[] = [
    {
      title: 'Clauses',
      value: 185,
      icon: '<svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor"><path d="M14 2H6a2 2 0 00-2 2v16a2 2 0 002 2h12a2 2 0 002-2V8z" stroke-width="2"/><path d="M14 2v6h6M16 13H8m8 4H8m2-8H8" stroke-width="2"/></svg>',
      color: '#3b82f6'
    },
    {
      title: 'Open Tasks',
      value: 124,
      icon: '<svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor"><path d="M9 11l3 3L22 4" stroke-width="2"/><path d="M21 12v7a2 2 0 01-2 2H5a2 2 0 01-2-2V5a2 2 0 012-2h11" stroke-width="2"/></svg>',
      color: '#f97316'
    },
    {
      title: 'Evidence Documents',
      value: 312,
      icon: '<svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor"><path d="M13 2H6a2 2 0 00-2 2v16a2 2 0 002 2h12a2 2 0 002-2V9z" stroke-width="2"/><path d="M13 2v7h7" stroke-width="2"/></svg>',
      color: '#10b981'
    },
    {
      title: 'Audit Findings',
      value: 15,
      icon: '<svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor"><circle cx="12" cy="12" r="10" stroke-width="2"/><path d="M12 8v4m0 4h.01" stroke-width="2" stroke-linecap="round"/></svg>',
      color: '#ef4444'
    },
    {
      title: 'Departments',
      value: 8,
      icon: '<svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor"><path d="M17 21v-2a4 4 0 00-4-4H5a4 4 0 00-4 4v2M9 11a4 4 0 100-8 4 4 0 000 8zm14 0a4 4 0 100-8 4 4 0 000 8z" stroke-width="2"/></svg>',
      color: '#2f6bff'
    }
  ];

  nodes: GraphNode[] = [
    { id: 'n1', label: 'NABH', type: 'framework', x: 600, y: 100, connections: 8, riskLevel: 'low', metadata: {} },
    { id: 'n2', label: 'ACC', type: 'chapter', x: 400, y: 200, connections: 12, riskLevel: 'medium', metadata: {} },
    { id: 'n3', label: 'COP', type: 'chapter', x: 600, y: 200, connections: 10, riskLevel: 'low', metadata: {} },
    { id: 'n4', label: 'AUD', type: 'chapter', x: 800, y: 200, connections: 8, riskLevel: 'high', metadata: {} },
    { id: 'n5', label: 'ACC-1.2.3', type: 'clause', x: 300, y: 320, connections: 5, riskLevel: 'high', metadata: { code: 'ACC-1.2.3', linkedTasks: 3, evidenceDocs: 2, openFindings: 1 } },
    { id: 'n6', label: 'ACC-2.1.1', type: 'clause', x: 500, y: 320, connections: 4, riskLevel: 'medium', metadata: { code: 'ACC-2.1.1', linkedTasks: 2, evidenceDocs: 3, openFindings: 0 } },
    { id: 'n7', label: 'Update Policy', type: 'task', x: 200, y: 440, connections: 3, riskLevel: 'medium', metadata: { owner: 'Rajesh Kumar', department: 'Security', dueDate: '2024-04-15', status: 'in-progress', linkedTasks: 0, evidenceDocs: 1, openFindings: 0 } },
    { id: 'n8', label: 'Training', type: 'task', x: 400, y: 440, connections: 2, riskLevel: 'low', metadata: { owner: 'Priya Sharma', department: 'HR', dueDate: '2024-04-20', status: 'completed', linkedTasks: 0, evidenceDocs: 2, openFindings: 0 } },
    { id: 'n9', label: 'Policy Doc', type: 'evidence', x: 150, y: 560, connections: 2, riskLevel: 'low', metadata: { fileName: 'access-policy-v2.pdf', uploadDate: '2024-03-20', approvalStatus: 'Approved', linkedTasks: 1, evidenceDocs: 0, openFindings: 0 } },
    { id: 'n10', label: 'Audit Report', type: 'evidence', x: 350, y: 560, connections: 3, riskLevel: 'low', metadata: { fileName: 'audit-report-march.pdf', uploadDate: '2024-03-25', approvalStatus: 'Approved', linkedTasks: 2, evidenceDocs: 0, openFindings: 0 } },
    { id: 'n11', label: 'Security Dept', type: 'department', x: 100, y: 320, connections: 6, riskLevel: 'medium', metadata: { department: 'Security', linkedTasks: 24, evidenceDocs: 15, openFindings: 3 } },
    { id: 'n12', label: 'Major Finding', type: 'finding', x: 300, y: 200, connections: 2, riskLevel: 'high', metadata: { linkedTasks: 2, evidenceDocs: 1, openFindings: 1 } },
  ];

  edges: GraphEdge[] = [
    { source: 'n1', target: 'n2', type: 'contains' },
    { source: 'n1', target: 'n3', type: 'contains' },
    { source: 'n1', target: 'n4', type: 'contains' },
    { source: 'n2', target: 'n5', type: 'contains' },
    { source: 'n2', target: 'n6', type: 'contains' },
    { source: 'n5', target: 'n7', type: 'generates' },
    { source: 'n6', target: 'n8', type: 'generates' },
    { source: 'n7', target: 'n9', type: 'requires' },
    { source: 'n8', target: 'n10', type: 'requires' },
    { source: 'n11', target: 'n7', type: 'responsible' },
    { source: 'n12', target: 'n5', type: 'linked' },
  ];

  nodeTypes = [
    { name: 'Framework', color: '#0d9488' },
    { name: 'Chapter', color: '#3b82f6' },
    { name: 'Clause', color: '#0ea5e9' },
    { name: 'Task', color: '#f97316' },
    { name: 'Evidence', color: '#10b981' },
    { name: 'Finding', color: '#ef4444' },
    { name: 'Department', color: '#2f6bff' },
  ];

  topClauses: AnalyticsData[] = [
    { label: 'ACC-1.2.3', value: 12, color: '#3b82f6' },
    { label: 'COP-2.1.1', value: 10, color: '#3b82f6' },
    { label: 'AUD-3.4.2', value: 9, color: '#3b82f6' },
    { label: 'INF-1.1.5', value: 8, color: '#3b82f6' },
    { label: 'ACC-2.3.1', value: 7, color: '#3b82f6' },
  ];

  departmentTasks: AnalyticsData[] = [
    { label: 'Security', value: 24, color: '#f97316' },
    { label: 'Admin', value: 18, color: '#f97316' },
    { label: 'Quality', value: 16, color: '#f97316' },
    { label: 'IT', value: 14, color: '#f97316' },
    { label: 'HR', value: 12, color: '#f97316' },
  ];

  evidenceCoverage: AnalyticsData[] = [
    { label: 'ACC', value: 92, color: '#10b981' },
    { label: 'COP', value: 88, color: '#10b981' },
    { label: 'AUD', value: 75, color: '#f59e0b' },
    { label: 'INF', value: 95, color: '#10b981' },
  ];

  findingsByChapter: AnalyticsData[] = [
    { label: 'ACC', value: 5, color: '#ef4444' },
    { label: 'COP', value: 3, color: '#f59e0b' },
    { label: 'AUD', value: 4, color: '#ef4444' },
    { label: 'INF', value: 3, color: '#f59e0b' },
  ];

  activities: ActivityEvent[] = [
    {
      id: 'a1',
      user: 'Rajesh Kumar',
      action: 'uploaded',
      target: 'CCTV installation report',
      timestamp: new Date(Date.now() - 1000 * 60 * 5),
      icon: '<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor"><path d="M21 15v4a2 2 0 01-2 2H5a2 2 0 01-2-2v-4M17 8l-5-5-5 5M12 3v12" stroke-width="2"/></svg>'
    },
    {
      id: 'a2',
      user: 'Priya Sharma',
      action: 'closed',
      target: 'ACC-1 remediation task',
      timestamp: new Date(Date.now() - 1000 * 60 * 15),
      icon: '<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor"><path d="M22 11.08V12a10 10 0 11-5.93-9.14" stroke-width="2"/><path d="M22 4L12 14.01l-3-3" stroke-width="2"/></svg>'
    },
    {
      id: 'a3',
      user: 'Audit Team',
      action: 'raised major finding for',
      target: 'COP-2',
      timestamp: new Date(Date.now() - 1000 * 60 * 30),
      icon: '<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor"><circle cx="12" cy="12" r="10" stroke-width="2"/><path d="M12 8v4m0 4h.01" stroke-width="2"/></svg>'
    },
    {
      id: 'a4',
      user: 'Dr. Anita Verma',
      action: 'approved evidence for',
      target: 'Clinical SOP document',
      timestamp: new Date(Date.now() - 1000 * 60 * 45),
      icon: '<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor"><path d="M9 11l3 3L22 4" stroke-width="2"/></svg>'
    },
  ];

  maxClauseConnections = 12;
  maxDepartmentTasks = 24;
  maxFindings = 5;

  filteredNodes = computed(() => {
    return this.nodes.filter(node => {
      if (this.searchQuery && !node.label.toLowerCase().includes(this.searchQuery.toLowerCase())) {
        return false;
      }
      return true;
    });
  });

  ngOnInit(): void {
    console.log('Knowledge Graph initialized');
  }

  ngOnDestroy(): void {
    console.log('Knowledge Graph destroyed');
  }

  selectNode(node: GraphNode): void {
    this.selectedNode.set(node);
  }

  closePanel(): void {
    this.selectedNode.set(null);
  }

  getNode(id: string): GraphNode | undefined {
    return this.nodes.find(n => n.id === id);
  }

  nodeRadius(node: GraphNode): number {
    const baseRadius = 25;
    const connectionBonus = Math.min(node.connections * 2, 20);
    return baseRadius + connectionBonus;
  }

  getConnections(node: GraphNode): { type: string; target: string }[] {
    const connections: { type: string; target: string }[] = [];

    this.edges.forEach(edge => {
      if (edge.source === node.id) {
        const targetNode = this.getNode(edge.target);
        if (targetNode) {
          connections.push({ type: edge.type, target: targetNode.label });
        }
      }
      if (edge.target === node.id) {
        const sourceNode = this.getNode(edge.source);
        if (sourceNode) {
          connections.push({ type: 'from', target: sourceNode.label });
        }
      }
    });

    return connections;
  }

  handleZoom(event: WheelEvent): void {
    event.preventDefault();
    const delta = event.deltaY > 0 ? 0.9 : 1.1;
    this.zoom.update(z => Math.max(0.5, Math.min(3, z * delta)));
  }

  expandRelated(): void {
    console.log('Expanding related nodes');
  }

  showUpstream(): void {
    console.log('Showing upstream dependencies');
  }

  showDownstream(): void {
    console.log('Showing downstream impact');
  }

  resetGraph(): void {
    this.zoom.set(1);
    this.pan.set({ x: 0, y: 0 });
    this.selectedNode.set(null);
  }

  formatTime(date: Date): string {
    const now = Date.now();
    const diff = now - date.getTime();
    const minutes = Math.floor(diff / 1000 / 60);

    if (minutes < 1) return 'Just now';
    if (minutes < 60) return `${minutes} minutes ago`;
    const hours = Math.floor(minutes / 60);
    if (hours < 24) return `${hours} hours ago`;
    const days = Math.floor(hours / 24);
    return `${days} days ago`;
  }
}
