export type TaskStatus = 'Backlog' | 'ToDo' | 'InProgress' | 'InReview' | 'Done'
export type TaskPriority = 'Low' | 'Medium' | 'High' | 'Critical'

export interface TaskResponse {
    id: number
    title: string
    description: string | null
    status: TaskStatus
    priority: TaskPriority
    dueDate: string | null
    createdAt: string
    projectId: number
    projectName: string
    assigneeId: number | null
    assigneeName: string | null
    creatorId: number
    creatorName: string
}

export interface CreateTaskRequest {
    title: string
    description?: string
    projectId: number
    priority?: TaskPriority
    dueDate?: string
    assigneeId?: number
}

export interface UpdateTaskRequest {
    title: string
    description: string | null
    dueDate: string | null
}