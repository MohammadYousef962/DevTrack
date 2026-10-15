export interface CommentResponse {
    id: number
    content: string
    taskId: number
    authorId: number
    authorName: string
    createdAt: string
    updatedAt?: string | null
}

export interface CreateCommentRequest {
    content: string
}

export interface UpdateCommentRequest {
    content: string
}