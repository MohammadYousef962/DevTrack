export type ProjectStatus = 'Planning' | 'Active' | 'Completed' | 'Archived'

export interface ProjectResponse {
    id: number
    name: string
    description: string | null
    status: ProjectStatus
    startDate: string
    dueDate: string | null
    teamId: number
    teamName: string
    createdAt: string
}

export interface CreateProjectRequest {
    name: string
    description?: string
    teamId: number
    startDate: string
    dueDate?: string
}

export interface ProjectMemberResponse {
    userId: number
    fullName: string
    email: string
    joinedAt: string
}