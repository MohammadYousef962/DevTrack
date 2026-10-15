import apiClient from './client'
import type {
    UserResponse,
    UserLookupResponse,
    UpdateProfileRequest,
    ChangePasswordRequest,
} from '../types/user'
import type { UserRoleName } from '../lib/roles'

export async function getMe(): Promise<UserResponse> {
    const response = await apiClient.get<UserResponse>('/users/me')
    return response.data
}

export async function updateMyProfile(request: UpdateProfileRequest): Promise<UserResponse> {
    const response = await apiClient.put<UserResponse>('/users/me', request)
    return response.data
}

export async function changePassword(request: ChangePasswordRequest): Promise<void> {
    await apiClient.post('/users/change-password', request)
}

export async function getUsers(): Promise<UserResponse[]> {
    const response = await apiClient.get<UserResponse[]>('/users')
    return response.data
}

export async function lookupUserByEmail(email: string): Promise<UserLookupResponse> {
    const response = await apiClient.get<UserLookupResponse>('/users/lookup', { params: { email } })
    return response.data
}

export async function updateUserRole(id: number, role: UserRoleName): Promise<UserResponse> {
    const response = await apiClient.patch<UserResponse>(`/users/${id}/role`, { role })
    return response.data
}

export async function updateUserStatus(id: number, isActive: boolean): Promise<UserResponse> {
    const response = await apiClient.patch<UserResponse>(`/users/${id}/status`, { isActive })
    return response.data
}