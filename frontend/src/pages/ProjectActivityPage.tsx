import { useEffect, useState, type ReactNode } from 'react'
import { Link, useParams } from 'react-router-dom'
import { ChevronLeft, ChevronRight, Loader2 } from 'lucide-react'
import { getProject } from '../api/projects'
import { getProjectActivity } from '../api/activity'
import { getErrorMessage } from '../api/errors'
import { useAuth } from '../context/AuthContext'
import { formatRelativeTime, parseApiDate } from '../lib/dates'
import { projectStatusStyles, taskStatusLabels } from '../lib/styles'
import { ProjectTabs } from '../components/ProjectTabs'
import type { ActivityLogResponse } from '../types/activity'
import type { PagedResult } from '../types/common'
import type { ProjectResponse } from '../types/project'
import type { TaskStatus } from '../types/task'

// Temporarily change this to 2 to try the paging controls without needing 21 events.
const PAGE_SIZE = 20

function Emphasis({ children }: { children: ReactNode }) {
    return <span className="font-medium text-app-text">{children}</span>
}

function statusLabel(value: string): string {
    return taskStatusLabels[value as TaskStatus] ?? value
}

// Turns a raw log entry into a sentence fragment like: created task #12 "Fix login"
function describeActivity(entry: ActivityLogResponse): ReactNode {
    const task = <Emphasis>task #{entry.entityId}</Emphasis>
    const details = entry.details

    switch (entry.action) {
        case 'Created project':
            return (
                <>
                    created project <Emphasis>{details ?? `#${entry.entityId}`}</Emphasis>
                </>
            )
        case 'Created task':
            return (
                <>
                    created {task}
                    {details && <> &ldquo;{details}&rdquo;</>}
                </>
            )
        case 'Changed task status': {
            const [from, to] = (details ?? '').split(' \u2192 ')
            if (from && to) {
                return (
                    <>
                        changed {task} from <Emphasis>{statusLabel(from)}</Emphasis> to <Emphasis>{statusLabel(to)}</Emphasis>
                    </>
                )
            }
            return <>changed the status of {task}</>
        }
        case 'Assigned task':
            return details && details !== 'Unassigned' ? (
                <>
                    assigned {task} to <Emphasis>{details}</Emphasis>
                </>
            ) : (
                <>unassigned {task}</>
            )
        default:
            return (
                <>
                    {entry.action.toLowerCase()} {entry.entityType.toLowerCase()} #{entry.entityId}
                    {details ? `: ${details}` : ''}
                </>
            )
    }
}

export function ProjectActivityPage() {
    const { projectId } = useParams()
    const id = Number(projectId)
    const { user } = useAuth()

    const [project, setProject] = useState<ProjectResponse | null>(null)
    const [projectError, setProjectError] = useState('')
    const [isProjectLoading, setIsProjectLoading] = useState(true)
    const [projectAttempt, setProjectAttempt] = useState(0)

    const [page, setPage] = useState(1)
    const [result, setResult] = useState<PagedResult<ActivityLogResponse> | null>(null)
    const [activityError, setActivityError] = useState('')
    const [isActivityLoading, setIsActivityLoading] = useState(true)
    const [activityAttempt, setActivityAttempt] = useState(0)

    useEffect(() => {
        let cancelled = false
        getProject(id)
            .then((data) => {
                if (!cancelled) {
                    setProject(data)
                    setProjectError('')
                }
            })
            .catch((err) => {
                if (!cancelled) setProjectError(getErrorMessage(err))
            })
            .finally(() => {
                if (!cancelled) setIsProjectLoading(false)
            })
        return () => {
            cancelled = true
        }
    }, [id, projectAttempt])

    useEffect(() => {
        let cancelled = false
        getProjectActivity(id, page, PAGE_SIZE)
            .then((data) => {
                if (!cancelled) {
                    setResult(data)
                    setActivityError('')
                }
            })
            .catch((err) => {
                if (!cancelled) {
                    setResult(null)
                    setActivityError(getErrorMessage(err))
                }
            })
            .finally(() => {
                if (!cancelled) setIsActivityLoading(false)
            })
        return () => {
            cancelled = true
        }
    }, [id, page, activityAttempt])

    function retryProject() {
        setIsProjectLoading(true)
        setProjectAttempt((n) => n + 1)
    }

    function retryActivity() {
        setIsActivityLoading(true)
        setActivityAttempt((n) => n + 1)
    }

    function goToPage(next: number) {
        setIsActivityLoading(true)
        setPage(next)
    }

    if (isProjectLoading) {
        return (
            <div className="flex items-center justify-center py-24 text-app-muted">
                <Loader2 className="w-5 h-5 animate-spin mr-2" /> Loading activity...
            </div>
        )
    }

    if (projectError || !project) {
        return (
            <div className="text-center py-24">
                <p className="text-app-bug text-sm mb-3">{projectError || 'Project not found.'}</p>
                <div className="flex items-center justify-center gap-4 text-sm">
                    <button onClick={retryProject} className="text-app-accent hover:underline">
                        Try again
                    </button>
                    <Link to="/projects" className="text-app-muted hover:text-app-text">
                        Back to projects
                    </Link>
                </div>
            </div>
        )
    }

    const totalPages = result?.totalPages ?? 0

    return (
        <div className="flex flex-col min-h-full">
            <div className="px-6 py-4 border-b border-app-border bg-app-surface">
                <div className="flex items-center gap-2 text-sm text-app-muted mb-2">
                    <Link to="/projects" className="hover:text-app-text transition-colors">
                        Projects
                    </Link>
                    <ChevronRight className="w-3 h-3" />
                    <Link to={`/projects/${project.id}`} className="hover:text-app-text transition-colors">
                        {project.name}
                    </Link>
                    <ChevronRight className="w-3 h-3" />
                    <span className="text-app-text font-medium">Activity</span>
                </div>
                <div className="flex items-center gap-3 flex-wrap">
                    <h1 className="text-2xl font-semibold tracking-tight text-app-text">{project.name}</h1>
                    <span className={`px-2 py-0.5 rounded text-[10px] font-semibold border ${projectStatusStyles[project.status]}`}>
                        {project.status}
                    </span>
                    <span className="text-xs text-app-muted">{project.teamName}</span>
                </div>
            </div>

            <ProjectTabs projectId={project.id} active="activity" />

            <div className="p-6 md:p-8 max-w-3xl w-full mx-auto space-y-4">
                {isActivityLoading && !result && (
                    <div className="flex items-center justify-center py-16 text-app-muted">
                        <Loader2 className="w-5 h-5 animate-spin mr-2" /> Loading activity...
                    </div>
                )}

                {!isActivityLoading && activityError && (
                    <div className="text-center py-16">
                        <p className="text-app-bug text-sm mb-3">{activityError}</p>
                        <button onClick={retryActivity} className="text-sm text-app-accent hover:underline">
                            Try again
                        </button>
                    </div>
                )}

                {!activityError && result && result.totalCount === 0 && (
                    <div className="text-center py-16 border border-dashed border-app-border rounded-xl">
                        <p className="text-app-text font-medium mb-1">No activity yet</p>
                        <p className="text-app-muted text-sm">
                            Creating tasks, changing their status or assigning them will show up here.
                        </p>
                    </div>
                )}

                {!activityError && result && result.totalCount > 0 && (
                    <>
                        <div
                            className={`bg-app-surface border border-app-border rounded-xl divide-y divide-app-border overflow-hidden transition-opacity ${isActivityLoading ? 'opacity-50' : ''
                                }`}
                        >
                            {result.items.map((entry) => {
                                const actor = entry.userId === user?.userId ? 'You' : entry.userName
                                const when = parseApiDate(entry.timestamp)
                                return (
                                    <div key={entry.id} className="p-4 flex gap-3">
                                        <div className="w-7 h-7 rounded-full bg-app-accent flex items-center justify-center text-white text-xs font-semibold shrink-0">
                                            {entry.userName.charAt(0)}
                                        </div>
                                        <div className="min-w-0 flex-1">
                                            <p className="text-sm text-app-muted break-words">
                                                <Emphasis>{actor}</Emphasis> {describeActivity(entry)}
                                            </p>
                                            <p className="text-xs text-app-muted mt-0.5" title={when.toLocaleString()}>
                                                {formatRelativeTime(when)}
                                            </p>
                                        </div>
                                    </div>
                                )
                            })}
                        </div>

                        <div className="flex items-center justify-between text-sm">
                            <span className="text-app-muted">
                                {result.totalCount} event{result.totalCount === 1 ? '' : 's'}
                            </span>
                            {totalPages > 1 && (
                                <div className="flex items-center gap-2">
                                    <button
                                        onClick={() => goToPage(page - 1)}
                                        disabled={page <= 1 || isActivityLoading}
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
                                        disabled={page >= totalPages || isActivityLoading}
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
        </div>
    )
}