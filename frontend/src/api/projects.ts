import apiClient from './client'
import type { ProjectResponse, CreateProjectRequest, ProjectMemberResponse } from '../types/project'

export async function getProjects(): Promise<ProjectResponse[]> {
    const response = await apiClient.get<ProjectResponse[]>('/projects')
    return response.data
}

export async function getProject(id: number): Promise<ProjectResponse> {
    const response = await apiClient.get<ProjectResponse>(`/projects/${id}`)
    return response.data
}

export async function createProject(request: CreateProjectRequest): Promise<ProjectResponse> {
    const response = await apiClient.post<ProjectResponse>('/projects', request)
    return response.data
}

export async function getProjectMembers(id: number): Promise<ProjectMemberResponse[]> {
    const response = await apiClient.get<ProjectMemberResponse[]>(`/projects/${id}/members`)
    return response.data
}