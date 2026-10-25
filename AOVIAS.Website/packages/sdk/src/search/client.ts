import type { WithSdkInfo } from '../core/client';
import type { SdkResult } from '../core/result';
import type {
  SearchFilters,
  SearchResponse,
  SearchSuggestion,
} from './types';

/**
 * Search SDK — local/static indexes only in R1.5.
 */
export type SearchClient = WithSdkInfo & {
  search: (filters: SearchFilters) => Promise<SdkResult<SearchResponse>>;

  suggest: (input: {
    query: string;
    limit?: number;
  }) => Promise<SdkResult<readonly SearchSuggestion[]>>;
};
