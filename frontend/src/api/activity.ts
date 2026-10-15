import apiClient from './client'
import type { PagedResult } from '../types/common'
import type { ActivityLogResponse, RecentActivityResponse } from '../types/activity'

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

export async function getRecentActivity(
    page: number,
    pageSize: number
): Promise<PagedResult<RecentActivityResponse>> {
    const response = await apiClient.get<PagedResult<RecentActivityResponse>>('/activity', {
        params: { page, pageSize },
    })
    return response.data
}