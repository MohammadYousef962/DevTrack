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