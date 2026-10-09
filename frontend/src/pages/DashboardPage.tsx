import { useEffect, useState } from 'react'
import { FolderKanban, TrendingUp, CheckSquare, AlertTriangle, Users, UserCheck, Calendar, Loader2 } from 'lucide-react'
import { useAuth } from '../context/AuthContext'
import { getProjects } from '../api/projects'
import { getTasks } from '../api/tasks'
import { getUsers } from '../api/users'
import { getErrorMessage } from '../api/errors'
import type { ProjectResponse } from '../types/project'
import type { TaskResponse } from '../types/task'
import type { UserResponse } from '../types/user'

export function DashboardPage() {
  const { user } = useAuth()
  const [projects, setProjects] = useState<ProjectResponse[]>([])
  const [totalTaskCount, setTotalTaskCount] = useState(0)
  const [overdueTasks, setOverdueTasks] = useState<TaskResponse[]>([])
  const [upcomingTasks, setUpcomingTasks] = useState<TaskResponse[]>([])
  const [users, setUsers] = useState<UserResponse[] | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [loadError, setLoadError] = useState('')

  useEffect(() => {
    loadDashboard()
  }, [])

  async function loadDashboard() {
    setIsLoading(true)
    setLoadError('')
    try {
      const today = new Date().toISOString().slice(0, 10)
      const weekFromNow = new Date(Date.now() + 7 * 24 * 60 * 60 * 1000).toISOString().slice(0, 10)

      const [projectsData, totalResult, overdueResult, upcomingResult, usersData] = await Promise.all([
        getProjects(),
        getTasks({ pageSize: 1 }),
        getTasks({ dueBefore: today, pageSize: 50 }),
        getTasks({ dueAfter: today, dueBefore: weekFromNow, pageSize: 50 }),
        user?.role === 'Admin' ? getUsers() : Promise.resolve(null),
      ])

      setProjects(projectsData)
      setTotalTaskCount(totalResult.totalCount)
      setOverdueTasks(overdueResult.items.filter((t) => t.status !== 'Done'))
      setUpcomingTasks(
        upcomingResult.items
          .filter((t) => t.status !== 'Done')
          .sort((a, b) => new Date(a.dueDate!).getTime() - new Date(b.dueDate!).getTime())
          .slice(0, 5)
      )
      setUsers(usersData)
    } catch (err) {
      setLoadError(getErrorMessage(err))
    } finally {
      setIsLoading(false)
    }
  }

  if (isLoading) {
    return (
      <div className="flex items-center justify-center py-24 text-app-muted">
        <Loader2 className="w-5 h-5 animate-spin mr-2" /> Loading dashboard...
      </div>
    )
  }

  if (loadError) {
    return (
      <div className="text-center py-24">
        <p className="text-app-bug text-sm mb-3">{loadError}</p>
        <button onClick={loadDashboard} className="text-sm text-app-accent hover:underline">
          Try again
        </button>
      </div>
    )
  }

  const activeProjects = projects.filter((p) => p.status === 'Active').length

  const stats = [
    { label: 'Total Projects', value: projects.length, icon: FolderKanban, color: 'text-app-accent' },
    { label: 'Active Projects', value: activeProjects, icon: TrendingUp, color: 'text-app-progress' },
    { label: 'Total Tasks', value: totalTaskCount, icon: CheckSquare, color: 'text-app-done' },
    { label: 'Tasks Overdue', value: overdueTasks.length, icon: AlertTriangle, color: 'text-app-bug' },
    ...(users
      ? [
          { label: 'Total Users', value: users.length, icon: Users, color: 'text-app-accent' },
          { label: 'Active Users', value: users.filter((u) => u.isActive).length, icon: UserCheck, color: 'text-app-done' },
        ]
      : []),
  ]

  return (
    <div className="p-6 md:p-8 max-w-6xl mx-auto space-y-8">
      <header>
        <h1 className="text-2xl font-semibold tracking-tight text-app-text">Welcome back, {user?.fullName}</h1>
        <p className="text-app-muted text-sm mt-1">Role: {user?.role}</p>
      </header>

      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
        {stats.map((stat) => (
          <div key={stat.label} className="bg-app-surface border border-app-border rounded-xl p-5">
            <div className="flex items-center justify-between mb-3">
              <h3 className="text-sm font-medium text-app-muted">{stat.label}</h3>
              <stat.icon className={`w-4 h-4 ${stat.color}`} />
            </div>
            <span className="text-3xl font-semibold text-app-text">{stat.value}</span>
          </div>
        ))}
      </div>

      <div className="space-y-4">
        <h2 className="text-sm font-semibold tracking-wide uppercase text-app-muted">Upcoming Deadlines</h2>
        <div className="bg-app-surface border border-app-border rounded-xl divide-y divide-app-border overflow-hidden">
          {upcomingTasks.length === 0 && (
            <p className="p-6 text-sm text-app-muted text-center">Nothing due in the next 7 days.</p>
          )}
          {upcomingTasks.map((task) => (
            <div key={task.id} className="p-3 flex items-center justify-between gap-3">
              <div className="min-w-0">
                <p className="text-sm font-medium text-app-text truncate">{task.title}</p>
                <p className="text-xs text-app-muted mt-0.5">{task.projectName} &middot; {task.priority}</p>
              </div>
              <span className="text-xs text-app-muted shrink-0 flex items-center gap-1">
                <Calendar className="w-3 h-3" />
                {new Date(task.dueDate!).toLocaleDateString()}
              </span>
            </div>
          ))}
        </div>
      </div>
    </div>
  )
}