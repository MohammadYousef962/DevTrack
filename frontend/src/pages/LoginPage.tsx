import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useAuth } from '../context/AuthContext'

export function LoginPage() {
    const { login } = useAuth()
    const navigate = useNavigate()
    const [error, setError] = useState('')

    async function handleTestLogin() {
        setError('')
        try {
            await login({ email: 'admin@devtrack.local', password: 'Admin123!' })
            navigate('/dashboard')
        } catch {
            setError('Login failed')
        }
    }

    return (
        <div className="h-screen w-full flex flex-col items-center justify-center gap-4 bg-app-base">
            <span className="text-2xl font-bold text-app-text">DevTrack — Login (placeholder)</span>
            <button
                onClick={handleTestLogin}
                className="px-4 py-2 rounded-md bg-app-accent text-white text-sm hover:bg-app-accentHover transition-colors"
            >
                Log in as Admin (test)
            </button>
            {error && <span className="text-app-bug text-sm">{error}</span>}
        </div>
    )
}