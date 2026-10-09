import apiClient from './client'
import type { PagedResult } from '../types/common'
import type {
    TaskResponse,
    CreateTaskRequest,
    UpdateTaskRequest,
    TaskStatus,
    TaskPriority,
} from '../types/task'

export interface TaskQueryParams {
    status?: string
    priority?: string
    assigneeId?: number
    projectId?: number
    search?: string
    dueBefore?: string
    dueAfter?: string
    page?: number
    pageSize?: number
}

export async function getTasks(params: TaskQueryParams = {}): Promise<PagedResult<TaskResponse>> {
    const response = await apiClient.get<PagedResult<TaskResponse>>('/tasks', { params })
    return response.data
}

export async function createTask(request: CreateTaskRequest): Promise<TaskResponse> {
    const response = await apiClient.post<TaskResponse>('/tasks', request)
    return response.data
}

export async function updateTask(id: number, request: UpdateTaskRequest): Promise<TaskResponse> {
    const response = await apiClient.put<TaskResponse>(`/tasks/${id}`, request)
    return response.data
}

export async function deleteTask(id: number): Promise<void> {
    await apiClient.delete(`/tasks/${id}`)
}

export async function updateTaskStatus(id: number, status: TaskStatus): Promise<TaskResponse> {
    const response = await apiClient.patch<TaskResponse>(`/tasks/${id}/status`, { status })
    return response.data
}

export async function updateTaskPriority(id: number, priority: TaskPriority): Promise<TaskResponse> {
    const response = await apiClient.patch<TaskResponse>(`/tasks/${id}/priority`, { priority })
    return response.data
}

export async function updateTaskAssignee(id: number, assigneeId: number | null): Promise<TaskResponse> {
    const response = await apiClient.patch<TaskResponse>(`/tasks/${id}/assignee`, { assigneeId })
    return response.data
}