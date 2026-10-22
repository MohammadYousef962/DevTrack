import { useEffect, useState } from 'react'
import { ChevronLeft, ChevronRight, Loader2 } from 'lucide-react'
import { getProjectActivity } from '../api/activity'
import { getErrorMessage } from '../api/errors'
import { useProjectContext } from '../context/useProjectContext'
import { ActivityFeedItem } from '../components/ActivityFeedItem'
import type { ActivityLogResponse } from '../types/activity'
import type { PagedResult } from '../types/common'

// Temporarily change this to 2 to try the paging controls without needing 21 events.
const PAGE_SIZE = 20

export function ProjectActivityPage() {
    const { project } = useProjectContext()
    const id = project.id

    const [page, setPage] = useState(1)
    const [result, setResult] = useState<PagedResult<ActivityLogResponse> | null>(null)
    const [error, setError] = useState('')
    const [isLoading, setIsLoading] = useState(true)
    const [attempt, setAttempt] = useState(0)

    useEffect(() => {
        let cancelled = false
        getProjectActivity(id, page, PAGE_SIZE)
            .then((data) => {
                if (!cancelled) {
                    setResult(data)
                    setError('')
                }
            })
            .catch((err) => {
                if (!cancelled) {
                    setResult(null)
                    setError(getErrorMessage(err))
                }
            })
            .finally(() => {
                if (!cancelled) setIsLoading(false)
            })
        return () => {
            cancelled = true
        }
    }, [id, page, attempt])

    function retry() {
        setIsLoading(true)
        setAttempt((n) => n + 1)
    }

    function goToPage(next: number) {
        setIsLoading(true)
        setPage(next)
    }

    const totalPages = result?.totalPages ?? 0

    return (
        <div className="p-6 md:p-8 max-w-3xl w-full mx-auto space-y-4">
            {isLoading && !result && (
                <div className="flex items-center justify-center py-16 text-app-muted">
                    <Loader2 className="w-5 h-5 animate-spin mr-2" /> Loading activity...
                </div>
            )}

            {!isLoading && error && (
                <div className="text-center py-16">
                    <p className="text-app-bug text-sm mb-3">{error}</p>
                    <button onClick={retry} className="text-sm text-app-accent hover:underline">
                        Try again
                    </button>
                </div>
            )}

            {!error && result && result.totalCount === 0 && (
                <div className="text-center py-16 border border-dashed border-app-border rounded-xl">
                    <p className="text-app-text font-medium mb-1">No activity yet</p>
                    <p className="text-app-muted text-sm">
                        Creating tasks, changing their status or assigning them will show up here.
                    </p>
                </div>
            )}

            {!error && result && result.totalCount > 0 && (
                <>
                    <div
                        className={`bg-app-surface border border-app-border rounded-xl divide-y divide-app-border overflow-hidden transition-opacity ${isLoading ? 'opacity-50' : ''
                            }`}
                    >
                        {result.items.map((entry) => (
                            <ActivityFeedItem key={entry.id} entry={entry} />
                        ))}
                    </div>

                    <div className="flex items-center justify-between text-sm">
                        <span className="text-app-muted">
                            {result.totalCount} event{result.totalCount === 1 ? '' : 's'}
                        </span>
                        {totalPages > 1 && (
                            <div className="flex items-center gap-2">
                                <button
                                    onClick={() => goToPage(page - 1)}
                                    disabled={page <= 1 || isLoading}
                                    aria-label="Previous page"
                                    className="p-1.5 rounded-md border border-app-border text-app-text hover:bg-app-hover transition-colors disabled:opacity-40 disabled:cursor-not-allowed"
                                >
                                    <ChevronLeft className="w-4 h-4" />
                                </button>
                                <span className="text-app-muted">
                                    Page {result.currentPage} of {totalPages}
                                </span>
                                <button
                                    onClick={() => goToPage(page + 1)}
                                    disabled={page >= totalPages || isLoading}
                                    aria-label="Next page"
                                    className="p-1.5 rounded-md border border-app-border text-app-text hover:bg-app-hover transition-colors disabled:opacity-40 disabled:cursor-not-allowed"
                                >
                                    <ChevronRight className="w-4 h-4" />
                                </button>
                            </div>
                        )}
                    </div>
                </>
            )}
        </div>
    )
}