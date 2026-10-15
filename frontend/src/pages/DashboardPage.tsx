import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import {
    FolderKanban,
    Users,
    CheckSquare,
    AlertTriangle,
    UserCheck,
    Calendar,
    Loader2,
    type LucideIcon,
} from 'lucide-react'
import { useAuth } from '../context/AuthContext'
import { getDashboardStats } from '../api/dashboard'
import { getRecentActivity } from '../api/activity'
import { getErrorMessage } from '../api/errors'
import { todayLocal } from '../lib/dates'
import { roleLabel } from '../lib/roles'
import { projectStatusStyles, taskPriorityStyles, taskStatusLabels } from '../lib/styles'
import { ActivityFeedItem } from '../components/ActivityFeedItem'
import type { RecentActivityResponse } from '../types/activity'
import type { DashboardStats } from '../types/dashboard'
import type { TaskStatus } from '../types/task'

const statusColors: Record<TaskStatus, string> = {
    Backlog: 'bg-app-muted',
    ToDo: 'bg-app-todo',
    InProgress: 'bg-app-progress',
    InReview: 'bg-app-review',
    Done: 'bg-app-done',
}

interface StatCard {
    label: string
    value: number
    note?: string
    icon: LucideIcon
    color: string
    to?: string
}

export function DashboardPage() {
    const { user } = useAuth()
    const [stats, setStats] = useState<DashboardStats | null>(null)
    const [activity, setActivity] = useState<RecentActivityResponse[] | null>(null)
    const [isLoading, setIsLoading] = useState(true)
    const [loadError, setLoadError] = useState('')
    const [attempt, setAttempt] = useState(0)

    useEffect(() => {
        let cancelled = false
        Promise.all([
            getDashboardStats(todayLocal()),
            // The feed is a nice-to-have, so a failure there shouldn't take the whole page down.
            getRecentActivity(1, 6).catch(() => null),
        ])
            .then(([statsData, activityData]) => {
                if (cancelled) return
                setStats(statsData)
                setActivity(activityData ? activityData.items : null)
                setLoadError('')
            })
            .catch((err) => {
                if (!cancelled) setLoadError(getErrorMessage(err))
            })
            .finally(() => {
                if (!cancelled) setIsLoading(false)
            })
        return () => {
            cancelled = true
        }
    }, [attempt])

    if (isLoading) {
        return (
            <div className="flex items-center justify-center py-24 text-app-muted">
                <Loader2 className="w-5 h-5 animate-spin mr-2" /> Loading dashboard...
            </div>
        )
    }

    if (loadError || !stats) {
        return (
            <div className="text-center py-24">
                <p className="text-app-bug text-sm mb-3">{loadError || 'Could not load the dashboard.'}</p>
                <button
                    onClick={() => {
                        setIsLoading(true)
                        setAttempt((n) => n + 1)
                    }}
                    className="text-sm text-app-accent hover:underline"
                >
                    Try again
                </button>
            </div>
        )
    }

    const cards: StatCard[] = [
        {
            label: 'Projects',
            value: stats.totalProjects,
            note: `${stats.activeProjects} active`,
            icon: FolderKanban,
            color: 'text-app-accent',
            to: '/projects',
        },
        { label: 'Teams', value: stats.totalTeams, icon: Users, color: 'text-app-review', to: '/teams' },
        {
            label: 'Tasks',
            value: stats.totalTasks,
            note: `${stats.completedTasks} completed`,
            icon: CheckSquare,
            color: 'text-app-done',
        },
        {
            label: 'Overdue',
            value: stats.overdueTasks,
            icon: AlertTriangle,
            color: stats.overdueTasks > 0 ? 'text-app-bug' : 'text-app-muted',
        },
        {
            label: 'Assigned to me',
            value: stats.myOpenTasks,
            note: `${stats.myTasksDueSoon} due this week`,
            icon: UserCheck,
            color: 'text-app-progress',
            to: '/my-tasks',
        },
    ]
    if (typeof stats.totalUsers === 'number' && typeof stats.activeUsers === 'number') {
        cards.push({
            label: 'Users',
            value: stats.totalUsers,
            note: `${stats.activeUsers} active`,
            icon: Users,
            color: 'text-app-accent',
            to: '/admin',
        })
    }

    return (
        <div className="p-6 md:p-8 max-w-6xl mx-auto space-y-8">
            <header>
                <h1 className="text-2xl font-semibold tracking-tight text-app-text">Welcome back, {user?.fullName}</h1>
                <p className="text-app-muted text-sm mt-1">{user ? roleLabel(user.role) : ''}</p>
            </header>

            <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
                {cards.map((card) => (
                    <StatCardView key={card.label} card={card} />
                ))}
            </div>

            <section className="space-y-3">
                <h2 className="text-sm font-semibold tracking-wide uppercase text-app-muted">Tasks by status</h2>
                <div className="bg-app-surface border border-app-border rounded-xl p-5">
                    {stats.totalTasks === 0 ? (
                        <p className="text-sm text-app-muted">No tasks yet.</p>
                    ) : (
                        <>
                            <div className="flex h-2.5 w-full overflow-hidden rounded-full bg-app-base">
                                {stats.tasksByStatus
                                    .filter((s) => s.count > 0)
                                    .map((s) => (
                                        <div
                                            key={s.status}
                                            className={statusColors[s.status]}
                                            style={{ width: `${(s.count / stats.totalTasks) * 100}%` }}
                                            title={`${taskStatusLabels[s.status]}: ${s.count}`}
                                        />
                                    ))}
                            </div>
                            <div className="mt-3 flex flex-wrap gap-x-5 gap-y-1.5 text-xs text-app-muted">
                                {stats.tasksByStatus.map((s) => (
                                    <span key={s.status} className="flex items-center gap-1.5">
                                        <span className={`w-2 h-2 rounded-full ${statusColors[s.status]}`} />
                                        {taskStatusLabels[s.status]}
                                        <span className="font-medium text-app-text">{s.count}</span>
                                    </span>
                                ))}
                            </div>
                        </>
                    )}
                </div>
            </section>

            <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
                <section className="space-y-3">
                    <h2 className="text-sm font-semibold tracking-wide uppercase text-app-muted">Project progress</h2>
                    <div className="bg-app-surface border border-app-border rounded-xl divide-y divide-app-border overflow-hidden">
                        {stats.projectProgress.length === 0 && (
                            <p className="p-6 text-sm text-app-muted text-center">No open projects to show.</p>
                        )}
                        {stats.projectProgress.map((p) => (
                            <Link
                                key={p.projectId}
                                to={`/projects/${p.projectId}`}
                                className="block p-3 hover:bg-app-hover transition-colors"
                            >
                                <div className="flex items-center justify-between gap-3 mb-2">
                                    <div className="flex items-center gap-2 min-w-0">
                                        <span className="text-sm font-medium text-app-text truncate">{p.name}</span>
                                        <span
                                            className={`px-1.5 py-0.5 rounded text-[10px] font-semibold border shrink-0 ${projectStatusStyles[p.status]}`}
                                        >
                                            {p.status}
                                        </span>
                                    </div>
                                    <span className="text-xs text-app-muted shrink-0">
                                        {p.completedTasks} of {p.totalTasks} done
                                    </span>
                                </div>
                                <div className="h-1.5 w-full bg-app-base rounded-full overflow-hidden">
                                    <div className="h-full bg-app-done rounded-full" style={{ width: `${p.progressPercent}%` }} />
                                </div>
                            </Link>
                        ))}
                    </div>
                </section>

                <section className="space-y-3">
                    <h2 className="text-sm font-semibold tracking-wide uppercase text-app-muted">Upcoming deadlines</h2>
                    <div className="bg-app-surface border border-app-border rounded-xl divide-y divide-app-border overflow-hidden">
                        {stats.upcomingTasks.length === 0 && (
                            <p className="p-6 text-sm text-app-muted text-center">Nothing due in the next 7 days.</p>
                        )}
                        {stats.upcomingTasks.map((task) => (
                            <Link
                                key={task.id}
                                to={`/projects/${task.projectId}?task=${task.id}`}
                                className="p-3 flex items-center justify-between gap-3 hover:bg-app-hover transition-colors"
                            >
                                <div className="min-w-0">
                                    <p className="text-sm font-medium text-app-text truncate">{task.title}</p>
                                    <div className="flex items-center gap-2 mt-1">
                                        <span className="text-xs text-app-muted truncate">{task.projectName}</span>
                                        <span
                                            className={`px-1.5 py-0.5 rounded text-[10px] font-semibold border shrink-0 ${taskPriorityStyles[task.priority]}`}
                                        >
                                            {task.priority}
                                        </span>
                                    </div>
                                </div>
                                <span className="text-xs text-app-muted shrink-0 flex items-center gap-1">
                                    <Calendar className="w-3 h-3" />
                                    {new Date(task.dueDate).toLocaleDateString()}
                                </span>
                            </Link>
                        ))}
                    </div>
                </section>
            </div>

            <section className="space-y-3">
                <div className="flex items-center justify-between">
                    <h2 className="text-sm font-semibold tracking-wide uppercase text-app-muted">Recent activity</h2>
                    <Link to="/activity" className="text-xs text-app-accent hover:underline">
                        View all
                    </Link>
                </div>
                <div className="bg-app-surface border border-app-border rounded-xl divide-y divide-app-border overflow-hidden">
                    {activity === null && (
                        <p className="p-6 text-sm text-app-muted text-center">Couldn't load recent activity.</p>
                    )}
                    {activity !== null && activity.length === 0 && (
                        <p className="p-6 text-sm text-app-muted text-center">No activity yet.</p>
                    )}
                    {activity?.map((entry) => (
                        <ActivityFeedItem key={entry.id} entry={entry} />
                    ))}
                </div>
            </section>
        </div>
    )
}

function StatCardView({ card }: { card: StatCard }) {
    const content = (
        <>
            <div className="flex items-center justify-between mb-3">
                <h3 className="text-sm font-medium text-app-muted">{card.label}</h3>
                <card.icon className={`w-4 h-4 ${card.color}`} />
            </div>
            <div className="flex items-baseline gap-2">
                <span className="text-3xl font-semibold text-app-text">{card.value}</span>
                {card.note && <span className="text-xs text-app-muted">{card.note}</span>}
            </div>
        </>
    )

    const className = 'bg-app-surface border border-app-border rounded-xl p-5'
    return card.to ? (
        <Link to={card.to} className={`${className} hover:border-app-borderLighter transition-colors`}>
            {content}
        </Link>
    ) : (
        <div className={className}>{content}</div>
    )
}