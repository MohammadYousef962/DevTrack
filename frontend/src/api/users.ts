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
export async function uploadMyAvatar(picture: Blob): Promise<UserResponse> {
    const response = await apiClient.put<UserResponse>('/users/me/avatar', picture, {
        headers: { 'Content-Type': picture.type || 'image/jpeg' },
    })
    return response.data
}

export async function removeMyAvatar(): Promise<UserResponse> {
    const response = await apiClient.delete<UserResponse>('/users/me/avatar')
    return response.data
}

// Pictures sit behind the sign-in token, which an <img> tag can't send, so they are downloaded like any API call.
export async function downloadAvatar(path: string): Promise<Blob> {
    // The profile stores the address as "/api/users/1/avatar?v=...", and the client already adds "/api".
    const response = await apiClient.get<Blob>(path.replace(/^\/api/, ''), { responseType: 'blob' })
    return response.data
}