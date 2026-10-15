export interface ActivityLogResponse {
    id: number
    userId: number
    userName: string
    projectId: number | null
    action: string
    entityType: string
    entityId: number
    details: string | null
    timestamp: string
}

// An entry in the cross-project feed. It also says which project it belongs to.
export interface RecentActivityResponse extends ActivityLogResponse {
    projectName: string | null
}