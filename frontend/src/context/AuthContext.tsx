import { createContext, useContext, useState, useEffect, type ReactNode } from 'react'
import type { AuthResponse, LoginRequest, RegisterRequest } from '../types/auth'
import { login as loginApi, register as registerApi } from '../api/auth'
import { SESSION_KEY } from '../api/client'

interface AuthContextType {
    user: AuthResponse | null
    isLoading: boolean
    login: (request: LoginRequest) => Promise<void>
    register: (request: RegisterRequest) => Promise<void>
    logout: () => void
}

const AuthContext = createContext<AuthContextType | undefined>(undefined)

export function AuthProvider({ children }: { children: ReactNode }) {
    const [user, setUser] = useState<AuthResponse | null>(null)
    const [isLoading, setIsLoading] = useState(true)

    useEffect(() => {
        const stored = localStorage.getItem(SESSION_KEY)
        if (stored) {
            setUser(JSON.parse(stored))
        }
        setIsLoading(false)
    }, [])

    async function login(request: LoginRequest) {
        const result = await loginApi(request)
        localStorage.setItem(SESSION_KEY, JSON.stringify(result))
        setUser(result)
    }

    async function register(request: RegisterRequest) {
        const result = await registerApi(request)
        localStorage.setItem(SESSION_KEY, JSON.stringify(result))
        setUser(result)
    }

    function logout() {
        localStorage.removeItem(SESSION_KEY)
        setUser(null)
    }

    return (
        <AuthContext.Provider value={{ user, isLoading, login, register, logout }}>
            {children}
        </AuthContext.Provider>
    )
}

export function useAuth() {
    const context = useContext(AuthContext)
    if (!context) {
        throw new Error('useAuth must be used within an AuthProvider')
    }
    return context
}