import type { ProjectStatus } from './project'
import type { TaskPriority, TaskStatus } from './task'

export interface DashboardStatusCount {
    status: TaskStatus
    count: number
}

export interface DashboardUpcomingTask {
    id: number
    title: string
    projectId: number
    projectName: string
    priority: TaskPriority
    status: TaskStatus
    dueDate: string
    assigneeName: string | null
}

export interface DashboardProjectProgress {
    projectId: number
    name: string
    status: ProjectStatus
    dueDate: string | null
    totalTasks: number
    completedTasks: number
    progressPercent: number
}

export interface DashboardStats {
    totalProjects: number
    activeProjects: number
    totalTeams: number
    totalTasks: number
    completedTasks: number
    overdueTasks: number
    myOpenTasks: number
    myTasksDueSoon: number
    tasksByStatus: DashboardStatusCount[]
    upcomingTasks: DashboardUpcomingTask[]
    projectProgress: DashboardProjectProgress[]
    totalUsers: number | null
    activeUsers: number | null
}