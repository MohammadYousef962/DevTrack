import apiClient from './client'
import type { DashboardStats } from '../types/dashboard'

// "today" is the browser's local date (YYYY-MM-DD) so the server judges "overdue" in the user's timezone.
export async function getDashboardStats(today: string): Promise<DashboardStats> {
    const response = await apiClient.get<DashboardStats>('/dashboard/stats', { params: { today } })
    return response.data
}