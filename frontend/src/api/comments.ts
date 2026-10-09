import apiClient from './client'
import type { CommentResponse } from '../types/comment'

export async function getComments(taskId: number): Promise<CommentResponse[]> {
    const response = await apiClient.get<CommentResponse[]>(`/tasks/${taskId}/comments`)
    return response.data
}

export async function addComment(taskId: number, content: string): Promise<CommentResponse> {
    const response = await apiClient.post<CommentResponse>(`/tasks/${taskId}/comments`, { content })
    return response.data
}

export async function updateComment(id: number, content: string): Promise<CommentResponse> {
    const response = await apiClient.put<CommentResponse>(`/comments/${id}`, { content })
    return response.data
}

export async function deleteComment(id: number): Promise<void> {
    await apiClient.delete(`/comments/${id}`)
}