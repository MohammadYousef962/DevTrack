import type { ProjectStatus } from '../types/project'
import type { TaskPriority, TaskStatus } from '../types/task'

export const projectStatusStyles: Record<ProjectStatus, string> = {
    Planning: 'border-app-todo/30 text-app-todo bg-app-todo/10',
    Active: 'border-app-progress/30 text-app-progress bg-app-progress/10',
    Completed: 'border-app-done/30 text-app-done bg-app-done/10',
    Archived: 'border-app-border text-app-muted bg-app-hover',
}

export const taskPriorityStyles: Record<TaskPriority, string> = {
    Low: 'border-app-border text-app-muted bg-app-hover',
    Medium: 'border-app-todo/30 text-app-todo bg-app-todo/10',
    High: 'border-app-progress/30 text-app-progress bg-app-progress/10',
    Critical: 'border-app-bug/30 text-app-bug bg-app-bug/10',
}

export const taskStatusLabels: Record<TaskStatus, string> = {
    Backlog: 'Backlog',
    ToDo: 'To Do',
    InProgress: 'In Progress',
    InReview: 'In Review',
    Done: 'Done',
}

export const taskStatusOrder: TaskStatus[] = ['Backlog', 'ToDo', 'InProgress', 'InReview', 'Done']

export const taskPriorityOrder: TaskPriority[] = ['Low', 'Medium', 'High', 'Critical']