/**
 * Cursor / offset pagination contracts for list endpoints.
 */

export type PageRequest = {
  page?: number;
  pageSize?: number;
  cursor?: string;
};

export type PageInfo = {
  page: number;
  pageSize: number;
  totalItems?: number;
  totalPages?: number;
  nextCursor?: string;
  hasMore: boolean;
};

export type Page<T> = {
  items: readonly T[];
  pageInfo: PageInfo;
};

export function emptyPage<T>(page = 1, pageSize = 20): Page<T> {
  return {
    items: [],
    pageInfo: { page, pageSize, totalItems: 0, totalPages: 0, hasMore: false },
  };
}

export function paginateArray<T>(
  items: readonly T[],
  request: PageRequest = {},
): Page<T> {
  const pageSize = Math.max(1, request.pageSize ?? 20);
  const page = Math.max(1, request.page ?? 1);
  const totalItems = items.length;
  const totalPages = Math.max(1, Math.ceil(totalItems / pageSize) || 1);
  const start = (page - 1) * pageSize;
  const slice = items.slice(start, start + pageSize);
  return {
    items: slice,
    pageInfo: {
      page,
      pageSize,
      totalItems,
      totalPages,
      hasMore: start + pageSize < totalItems,
    },
  };
}
