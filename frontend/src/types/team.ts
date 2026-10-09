export interface TeamResponse {
    id: number
    name: string
    description: string | null
    ownerId: number
    ownerName: string
    memberCount: number
    createdAt: string
}

export interface TeamMemberResponse {
    userId: number
    fullName: string
    email: string
    joinedAt: string
}

export interface SaveTeamRequest {
    name: string
    description: string | null
}