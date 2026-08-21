/**
 * The envelope every collection endpoint returns. One shape for every list in
 * the API, so a component that can page one resource can page all of them.
 */
export type PagedResponse<T> = {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
};
