import type { JsonLd } from './schema';

export type SchemaValidationIssue = {
  severity: 'error' | 'warning';
  message: string;
  path?: string;
};

export type SchemaValidationResult = {
  ok: boolean;
  issues: SchemaValidationIssue[];
};

const REQUIRED_BY_TYPE: Record<string, readonly string[]> = {
  Organization: ['@type', 'name', 'url'],
  WebSite: ['@type', 'name', 'url'],
  WebPage: ['@type', 'name', 'url'],
  AboutPage: ['@type', 'name', 'url'],
  ContactPage: ['@type', 'name', 'url'],
  CollectionPage: ['@type', 'name', 'url'],
  BreadcrumbList: ['@type', 'itemListElement'],
  FAQPage: ['@type', 'mainEntity'],
  SoftwareApplication: ['@type', 'name'],
  Product: ['@type', 'name'],
  Article: ['@type', 'headline'],
  BlogPosting: ['@type', 'headline'],
  TechArticle: ['@type', 'headline'],
  ItemList: ['@type', 'itemListElement'],
  SearchAction: ['@type', 'target'],
};

function asType(value: unknown): string | undefined {
  if (typeof value === 'string') return value;
  if (Array.isArray(value) && typeof value[0] === 'string') return value[0];
  return undefined;
}

/**
 * Lightweight schema.org structural validation for AOVIAS JSON-LD graphs.
 * Does not call external validators — suitable for CI / smoke checks.
 */
export function validateJsonLd(data: JsonLd | readonly JsonLd[]): SchemaValidationResult {
  const nodes = Array.isArray(data) ? data : [data];
  const issues: SchemaValidationIssue[] = [];

  nodes.forEach((node, index) => {
    const type = asType(node['@type']);
    const prefix = `nodes[${index}]`;

    if (!type) {
      issues.push({
        severity: 'error',
        message: 'Missing @type',
        path: prefix,
      });
      return;
    }

    const required = REQUIRED_BY_TYPE[type];
    if (required) {
      for (const key of required) {
        if (node[key] === undefined || node[key] === null || node[key] === '') {
          issues.push({
            severity: 'error',
            message: `Missing required property "${key}" for ${type}`,
            path: `${prefix}.${key}`,
          });
        }
      }
    } else {
      issues.push({
        severity: 'warning',
        message: `No required-field map for @type "${type}" — skipped strict checks`,
        path: prefix,
      });
    }

    if (type === 'BreadcrumbList') {
      const elements = node['itemListElement'];
      if (!Array.isArray(elements) || elements.length === 0) {
        issues.push({
          severity: 'error',
          message: 'BreadcrumbList.itemListElement must be a non-empty array',
          path: `${prefix}.itemListElement`,
        });
      }
    }

    if (type === 'FAQPage') {
      const main = node['mainEntity'];
      if (!Array.isArray(main) || main.length === 0) {
        issues.push({
          severity: 'error',
          message: 'FAQPage.mainEntity must be a non-empty array',
          path: `${prefix}.mainEntity`,
        });
      }
    }
  });

  return {
    ok: issues.every((issue) => issue.severity !== 'error'),
    issues,
  };
}
