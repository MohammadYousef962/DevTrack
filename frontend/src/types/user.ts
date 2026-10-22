export interface UserResponse {
    id: number
    fullName: string
    email: string
    role: string
    profileImageUrl: string | null
    isActive: boolean
    createdAt: string
}

export interface UserLookupResponse {
    id: number
    fullName: string
    email: string
}

export interface UpdateProfileRequest {
    fullName: string
}

export interface ChangePasswordRequest {
    currentPassword: string
    newPassword: string
    confirmNewPassword: string
}