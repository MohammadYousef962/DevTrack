import apiClient from './client'
import type { TeamResponse, TeamMemberResponse, SaveTeamRequest } from '../types/team'

export async function getTeams(): Promise<TeamResponse[]> {
    const response = await apiClient.get<TeamResponse[]>('/teams')
    return response.data
}

export async function getTeam(id: number): Promise<TeamResponse> {
    const response = await apiClient.get<TeamResponse>(`/teams/${id}`)
    return response.data
}

export async function createTeam(request: SaveTeamRequest): Promise<TeamResponse> {
    const response = await apiClient.post<TeamResponse>('/teams', request)
    return response.data
}

export async function updateTeam(id: number, request: SaveTeamRequest): Promise<TeamResponse> {
    const response = await apiClient.put<TeamResponse>(`/teams/${id}`, request)
    return response.data
}

export async function deleteTeam(id: number): Promise<void> {
    await apiClient.delete(`/teams/${id}`)
}

export async function getTeamMembers(id: number): Promise<TeamMemberResponse[]> {
    const response = await apiClient.get<TeamMemberResponse[]>(`/teams/${id}/members`)
    return response.data
}

export async function addTeamMember(id: number, userId: number): Promise<TeamMemberResponse> {
    const response = await apiClient.post<TeamMemberResponse>(`/teams/${id}/members`, { userId })
    return response.data
}

export async function removeTeamMember(id: number, userId: number): Promise<void> {
    await apiClient.delete(`/teams/${id}/members/${userId}`)
}