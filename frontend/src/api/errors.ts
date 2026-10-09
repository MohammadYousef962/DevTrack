import { AxiosError } from 'axios'

export function getErrorMessage(err: unknown): string {
    if (err instanceof AxiosError && err.response?.data) {
        const data = err.response.data as { message?: string; errors?: Record<string, string[]> }
        if (data.errors) {
            return Object.values(data.errors).flat().join(' ')
        }
        if (data.message) {
            return data.message
        }
    }
    return 'Something went wrong. Please try again.'
}