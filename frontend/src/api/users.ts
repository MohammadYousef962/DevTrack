import apiClient from './client'
import type { UserResponse, UserLookupResponse } from '../types/user'

export async function getUsers(): Promise<UserResponse[]> {
    const response = await apiClient.get<UserResponse[]>('/users')
    return response.data
}

export async function lookupUserByEmail(email: string): Promise<UserLookupResponse> {
    const response = await apiClient.get<UserLookupResponse>('/users/lookup', { params: { email } })
    return response.data
}