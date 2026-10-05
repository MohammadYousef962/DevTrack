import axios from 'axios'
import type { AuthResponse } from '../types/auth'

export const SESSION_KEY = 'devtrack_session'

const apiClient = axios.create({
    baseURL: 'http://localhost:5055/api',
})

apiClient.interceptors.request.use((config) => {
    const stored = localStorage.getItem(SESSION_KEY)
    if (stored) {
        const session: AuthResponse = JSON.parse(stored)
        config.headers.Authorization = `Bearer ${session.token}`
    }
    return config
})

export default apiClient