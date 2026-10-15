import apiClient from './client'
import type {
    ProjectResponse,
    CreateProjectRequest,
    UpdateProjectRequest,
    ProjectStatus,
    ProjectMemberResponse,
} from '../types/project'

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

export async function updateProject(id: number, request: UpdateProjectRequest): Promise<ProjectResponse> {
    const response = await apiClient.put<ProjectResponse>(`/projects/${id}`, request)
    return response.data
}

export async function updateProjectStatus(id: number, status: ProjectStatus): Promise<ProjectResponse> {
    const response = await apiClient.patch<ProjectResponse>(`/projects/${id}/status`, { status })
    return response.data
}

export async function deleteProject(id: number): Promise<void> {
    await apiClient.delete(`/projects/${id}`)
}

export async function getProjectMembers(id: number): Promise<ProjectMemberResponse[]> {
    const response = await apiClient.get<ProjectMemberResponse[]>(`/projects/${id}/members`)
    return response.data
}

export async function addProjectMember(id: number, userId: number): Promise<ProjectMemberResponse> {
    const response = await apiClient.post<ProjectMemberResponse>(`/projects/${id}/members`, { userId })
    return response.data
}

export async function removeProjectMember(id: number, userId: number): Promise<void> {
    await apiClient.delete(`/projects/${id}/members/${userId}`)
}