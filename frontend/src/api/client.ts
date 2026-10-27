import axios from 'axios'
import type { AuthResponse } from '../types/auth'

export const SESSION_KEY = 'devtrack_session'
export const SESSION_EXPIRED_EVENT = 'devtrack:session-expired'

const apiClient = axios.create({
    baseURL: import.meta.env.PROD ? '/api' : 'http://localhost:5055/api',
})

apiClient.interceptors.request.use((config) => {
    const stored = localStorage.getItem(SESSION_KEY)
    if (stored) {
        const session: AuthResponse = JSON.parse(stored)
        config.headers.Authorization = `Bearer ${session.token}`
    }
    return config
})

apiClient.interceptors.response.use(
    (response) => response,
    (error: unknown) => {
        if (axios.isAxiosError(error) && error.response?.status === 401) {
            const isAuthRequest = (error.config?.url ?? '').startsWith('/auth/')
            if (!isAuthRequest) {
                localStorage.removeItem(SESSION_KEY)
                window.dispatchEvent(new Event(SESSION_EXPIRED_EVENT))
            }
        }
        return Promise.reject(error)
    }
)

export default apiClient