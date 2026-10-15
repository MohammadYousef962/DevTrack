import apiClient from './client'
import type { PagedResult } from '../types/common'
import type { ActivityLogResponse } from '../types/activity'

export async function getProjectActivity(
    projectId: number,
    page: number,
    pageSize: number
): Promise<PagedResult<ActivityLogResponse>> {
    const response = await apiClient.get<PagedResult<ActivityLogResponse>>(`/projects/${projectId}/activity`, {
        params: { page, pageSize },
    })
    return response.data
}