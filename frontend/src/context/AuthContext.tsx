import { createContext, useCallback, useContext, useEffect, useState, type ReactNode } from 'react'
import type { AuthResponse, LoginRequest, RegisterRequest } from '../types/auth'
import { login as loginApi, register as registerApi } from '../api/auth'
import { SESSION_KEY, SESSION_EXPIRED_EVENT } from '../api/client'

type ProfilePatch = Partial<Pick<AuthResponse, 'fullName' | 'profileImageUrl'>>
export type SignedOutReason = 'manual' | 'expired'

interface AuthContextType {
    user: AuthResponse | null
    signedOutReason: SignedOutReason | null
    login: (request: LoginRequest) => Promise<void>
    register: (request: RegisterRequest) => Promise<void>
    logout: () => void
    updateUser: (patch: ProfilePatch) => void
}

const AuthContext = createContext<AuthContextType | undefined>(undefined)

// Reads the saved session once, before the first render, so a page refresh never flashes a
// "signed out" state. A damaged entry is removed and treated as "not signed in".
function readStoredSession(): AuthResponse | null {
    const stored = localStorage.getItem(SESSION_KEY)
    if (!stored) return null
    try {
        return JSON.parse(stored) as AuthResponse
    } catch {
        localStorage.removeItem(SESSION_KEY)
        return null
    }
}

export function AuthProvider({ children }: { children: ReactNode }) {
    const [user, setUser] = useState<AuthResponse | null>(readStoredSession)
    const [signedOutReason, setSignedOutReason] = useState<SignedOutReason | null>(null)

    // The API client raises this when the server rejects the token.
    useEffect(() => {
        function handleSessionExpired() {
            setUser(null)
            setSignedOutReason('expired')
        }
        window.addEventListener(SESSION_EXPIRED_EVENT, handleSessionExpired)
        return () => window.removeEventListener(SESSION_EXPIRED_EVENT, handleSessionExpired)
    }, [])

    async function login(request: LoginRequest) {
        const result = await loginApi(request)
        localStorage.setItem(SESSION_KEY, JSON.stringify(result))
        setUser(result)
        setSignedOutReason(null)
    }

    async function register(request: RegisterRequest) {
        const result = await registerApi(request)
        localStorage.setItem(SESSION_KEY, JSON.stringify(result))
        setUser(result)
        setSignedOutReason(null)
    }

    function logout() {
        localStorage.removeItem(SESSION_KEY)
        setUser(null)
        setSignedOutReason('manual')
    }

    // Keeps the saved session (and so the sidebar) in step after the profile changes.
    // Reads the stored session instead of the current state so the function stays stable.
    const updateUser = useCallback((patch: ProfilePatch) => {
        const stored = localStorage.getItem(SESSION_KEY)
        if (!stored) return
        const next: AuthResponse = { ...JSON.parse(stored), ...patch }
        localStorage.setItem(SESSION_KEY, JSON.stringify(next))
        setUser(next)
    }, [])

    return (
        <AuthContext.Provider value={{ user, signedOutReason, login, register, logout, updateUser }}>
            {children}
        </AuthContext.Provider>
    )
}

// A hook next to its provider is the normal pattern for React context. This lint rule only protects
// fast refresh while developing (editing this file does a full reload instead of a hot update),
// so it's switched off for this one export. It doesn't catch bugs.
// eslint-disable-next-line react-refresh/only-export-components
export function useAuth() {
    const context = useContext(AuthContext)
    if (!context) {
        throw new Error('useAuth must be used within an AuthProvider')
    }
    return context
}