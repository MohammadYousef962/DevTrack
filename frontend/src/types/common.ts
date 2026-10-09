export interface PagedResult<T> {
  items: T[]
  currentPage: number
  pageSize: number
  totalCount: number
  totalPages: number
}