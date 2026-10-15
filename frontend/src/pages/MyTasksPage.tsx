import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { Calendar, ChevronLeft, ChevronRight, Loader2, Search } from 'lucide-react'
import { getTasks } from '../api/tasks'
import { getErrorMessage } from '../api/errors'
import { useAuth } from '../context/AuthContext'
import { todayLocal } from '../lib/dates'
import {
    taskPriorityOrder,
    taskPriorityStyles,
    taskStatusLabels,
    taskStatusOrder,
    taskStatusStyles,
} from '../lib/styles'
import type { PagedResult } from '../types/common'
import type { TaskPriority, TaskResponse, TaskStatus } from '../types/task'

// Temporarily change this to 2 to try the paging controls without needing 21 tasks.
const PAGE_SIZE = 20

const controlClass =
    'bg-app-surface border border-app-border rounded-lg px-3 py-2 text-sm text-app-text focus:outline-none focus:border-app-accent focus:ring-1 focus:ring-app-accent'

interface LoadedState {
    key: string
    result: PagedResult<TaskResponse> | null
    error: string
}

export function MyTasksPage() {
    const { user } = useAuth()
    const userId = user?.userId

    const [searchInput, setSearchInput] = useState('')
    const [search, setSearch] = useState('')
    const [status, setStatus] = useState<'' | TaskStatus>('')
    const [priority, setPriority] = useState<'' | TaskPriority>('')
    const [page, setPage] = useState(1)
    const [attempt, setAttempt] = useState(0)
    const [loaded, setLoaded] = useState<LoadedState | null>(null)

    // Wait for a short pause in typing before searching, so each keystroke isn't a request.
    useEffect(() => {
        const timer = setTimeout(() => {
            setSearch(searchInput.trim())
            setPage(1)
        }, 300)
        return () => clearTimeout(timer)
    }, [searchInput])

    // Identifies the request the current filters describe. Results are tagged with it, so the page
    // knows whether what it's showing is up to date, without separate loading flags to keep in sync.
    const key = JSON.stringify([userId, status, priority, search, page, attempt])

    useEffect(() => {
        if (userId === undefined) return
        let cancelled = false
        getTasks({
            assigneeId: userId,
            status: status || undefined,
            priority: priority || undefined,
            search: search || undefined,
            page,
            pageSize: PAGE_SIZE,
        })
            .then((result) => {
                if (!cancelled) setLoaded({ key, result, error: '' })
            })
            .catch((err) => {
                if (!cancelled) setLoaded({ key, result: null, error: getErrorMessage(err) })
            })
        return () => {
            cancelled = true
        }
    }, [key, userId, status, priority, search, page])

    const isLoading = loaded === null || loaded.key !== key
    const result = loaded?.result ?? null
    const error = !isLoading ? (loaded?.error ?? '') : ''
    const hasFilters = status !== '' || priority !== '' || search !== ''
    const totalPages = result?.totalPages ?? 0
    const today = todayLocal()

    function changeStatus(value: '' | TaskStatus) {
        setStatus(value)
        setPage(1)
    }

    function changePriority(value: '' | TaskPriority) {
        setPriority(value)
        setPage(1)
    }

    function clearFilters() {
        setSearchInput('')
        setSearch('')
        setStatus('')
        setPriority('')
        setPage(1)
    }

    return (
        <div className="p-6 md:p-8 max-w-4xl mx-auto space-y-6">
            <header>
                <h1 className="text-2xl font-semibold tracking-tight text-app-text">My Tasks</h1>
                <p className="text-sm text-app-muted mt-1">
                    Tasks assigned to you
                    {result && (
                        <>
                            {' '}
                            &middot; {result.totalCount} {hasFilters ? 'matching' : 'total'}
                        </>
                    )}
                </p>
            </header>

            <div className="flex flex-col sm:flex-row gap-3">
                <div className="relative flex-1">
                    <Search className="w-4 h-4 text-app-muted absolute left-3 top-1/2 -translate-y-1/2" />
                    <input
                        type="text"
                        value={searchInput}
                        onChange={(e) => setSearchInput(e.target.value)}
                        placeholder="Search title or description"
                        className={`${controlClass} w-full pl-9 placeholder-app-muted`}
                    />
                </div>
                <select
                    value={status}
                    onChange={(e) => changeStatus(e.target.value as '' | TaskStatus)}
                    aria-label="Filter by status"
                    className={controlClass}
                >
                    <option value="">All statuses</option>
                    {taskStatusOrder.map((s) => (
                        <option key={s} value={s}>
                            {taskStatusLabels[s]}
                        </option>
                    ))}
                </select>
                <select
                    value={priority}
                    onChange={(e) => changePriority(e.target.value as '' | TaskPriority)}
                    aria-label="Filter by priority"
                    className={controlClass}
                >
                    <option value="">All priorities</option>
                    {taskPriorityOrder.map((p) => (
                        <option key={p} value={p}>
                            {p}
                        </option>
                    ))}
                </select>
            </div>

            {isLoading && !result && (
                <div className="flex items-center justify-center py-16 text-app-muted">
                    <Loader2 className="w-5 h-5 animate-spin mr-2" /> Loading your tasks...
                </div>
            )}

            {!isLoading && error && (
                <div className="text-center py-16">
                    <p className="text-app-bug text-sm mb-3">{error}</p>
                    <button onClick={() => setAttempt((n) => n + 1)} className="text-sm text-app-accent hover:underline">
                        Try again
                    </button>
                </div>
            )}

            {!error && result && result.totalCount === 0 && (
                <div className="text-center py-16 border border-dashed border-app-border rounded-xl">
                    {hasFilters ? (
                        <>
                            <p className="text-app-text font-medium mb-1">No tasks match these filters</p>
                            <button onClick={clearFilters} className="text-sm text-app-accent hover:underline mt-1">
                                Clear filters
                            </button>
                        </>
                    ) : (
                        <>
                            <p className="text-app-text font-medium mb-1">Nothing assigned to you</p>
                            <p className="text-app-muted text-sm">
                                Tasks show up here when someone assigns them to you, or when you assign one to yourself on a project board.
                            </p>
                        </>
                    )}
                </div>
            )}

            {!error && result && result.totalCount > 0 && (
                <>
                    <div
                        className={`bg-app-surface border border-app-border rounded-xl divide-y divide-app-border overflow-hidden transition-opacity ${isLoading ? 'opacity-50' : ''
                            }`}
                    >
                        {result.items.map((task) => (
                            <TaskRow key={task.id} task={task} today={today} />
                        ))}
                    </div>

                    {totalPages > 1 && (
                        <div className="flex items-center justify-end gap-2 text-sm">
                            <button
                                onClick={() => setPage(page - 1)}
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
                                onClick={() => setPage(page + 1)}
                                disabled={page >= totalPages || isLoading}
                                aria-label="Next page"
                                className="p-1.5 rounded-md border border-app-border text-app-text hover:bg-app-hover transition-colors disabled:opacity-40 disabled:cursor-not-allowed"
                            >
                                <ChevronRight className="w-4 h-4" />
                            </button>
                        </div>
                    )}
                </>
            )}
        </div>
    )
}

function TaskRow({ task, today }: { task: TaskResponse; today: string }) {
    const isDone = task.status === 'Done'
    const isOverdue = !!task.dueDate && !isDone && task.dueDate.slice(0, 10) < today

    return (
        <Link to={`/projects/${task.projectId}?task=${task.id}`} className="block p-3 hover:bg-app-hover transition-colors">
            <div className="flex flex-col sm:flex-row sm:items-center gap-2 sm:gap-4">
                <div className="min-w-0 flex-1">
                    <div className="flex items-center gap-2 mb-0.5 text-xs text-app-muted">
                        <span className="font-medium">#{task.id}</span>
                        <span className="truncate">{task.projectName}</span>
                    </div>
                    <p className={`text-sm font-medium truncate ${isDone ? 'text-app-muted line-through' : 'text-app-text'}`}>
                        {task.title}
                    </p>
                </div>
                <div className="flex items-center gap-2 flex-wrap shrink-0">
                    <span className={`px-1.5 py-0.5 rounded text-[10px] font-semibold border ${taskPriorityStyles[task.priority]}`}>
                        {task.priority}
                    </span>
                    <span className={`px-1.5 py-0.5 rounded text-[10px] font-semibold border ${taskStatusStyles[task.status]}`}>
                        {taskStatusLabels[task.status]}
                    </span>
                    {task.dueDate && (
                        <span className={`flex items-center gap-1 text-xs ${isOverdue ? 'text-app-bug' : 'text-app-muted'}`}>
                            <Calendar className="w-3 h-3" />
                            {new Date(task.dueDate).toLocaleDateString()}
                        </span>
                    )}
                </div>
            </div>
        </Link>
    )
}