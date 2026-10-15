import type { ReactNode } from 'react'
import { Navigate, useLocation } from 'react-router-dom'
import { useAuth } from '../context/AuthContext'

export function ProtectedRoute({ children }: { children: ReactNode }) {
    const { user, signedOutReason } = useAuth()
    const location = useLocation()

    if (!user) {
        // Remember where the person was heading so login can send them back,
        // except after an explicit logout, where starting fresh is the expectation.
        const from = signedOutReason === 'manual' ? undefined : location.pathname + location.search
        return <Navigate to="/login" replace state={from ? { from } : undefined} />
    }

    return <>{children}</>
}