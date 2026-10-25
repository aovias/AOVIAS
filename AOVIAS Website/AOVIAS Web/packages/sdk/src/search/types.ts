/**
 * Site-wide search domain models.
 */

export type SearchScope =
  | 'all'
  | 'knowledge'
  | 'products'
  | 'technology'
  | 'company';

export type SearchSuggestion = {
  id: string;
  label: string;
  href: string;
  scope: SearchScope;
};

export type SearchResultItem = {
  id: string;
  title: string;
  description?: string;
  href: string;
  scope: SearchScope;
  highlights?: readonly string[];
};

export type SearchFilters = {
  query: string;
  scopes?: readonly SearchScope[];
  limit?: number;
};

export type SearchResponse = {
  query: string;
  results: readonly SearchResultItem[];
  suggestions: readonly SearchSuggestion[];
  total: number;
};
