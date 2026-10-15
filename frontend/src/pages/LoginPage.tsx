import { useState, type FormEvent } from 'react'
import { useNavigate, useLocation, Link } from 'react-router-dom'
import { Layers } from 'lucide-react'
import { useAuth } from '../context/AuthContext'
import { getErrorMessage } from '../api/errors'

export function LoginPage() {
    const { login, signedOutReason } = useAuth()
    const navigate = useNavigate()
    const location = useLocation()
    const [email, setEmail] = useState('')
    const [password, setPassword] = useState('')
    const [error, setError] = useState('')
    const [isSubmitting, setIsSubmitting] = useState(false)

    const from = (location.state as { from?: string } | null)?.from
    const destination = from && from.startsWith('/') && !from.startsWith('//') ? from : '/dashboard'

    async function handleSubmit(e: FormEvent) {
        e.preventDefault()
        setError('')
        setIsSubmitting(true)
        try {
            await login({ email, password })
            navigate(destination, { replace: true })
        } catch (err) {
            setError(getErrorMessage(err))
        } finally {
            setIsSubmitting(false)
        }
    }

    return (
        <div className="fixed inset-0 bg-app-base overflow-x-hidden overflow-y-auto flex flex-col p-4">
            <div className="absolute top-1/2 left-1/2 -translate-x-1/2 -translate-y-1/2 w-[500px] h-[500px] bg-app-accent/10 rounded-full blur-[100px] pointer-events-none" />

            <div className="w-full max-w-sm relative z-10 m-auto">
                <div className="flex items-center justify-center gap-2 mb-8">
                    <div className="w-8 h-8 rounded-md bg-app-accent flex items-center justify-center text-white shadow-glow">
                        <Layers className="w-5 h-5" />
                    </div>
                    <span className="text-2xl font-bold tracking-tight text-app-text">DevTrack</span>
                </div>

                <div className="bg-app-surface border border-app-border rounded-xl p-8 shadow-2xl">
                    <h1 className="text-lg font-medium text-app-text mb-1">Sign in to your workspace</h1>
                    <p className="text-sm text-app-muted mb-6">Enter your details to proceed.</p>

                    {signedOutReason === 'expired' && (
                        <div className="mb-4 rounded-lg border border-app-progress/30 bg-app-progress/10 px-3 py-2 text-sm text-app-progress">
                            Your session expired. Please sign in again.
                        </div>
                    )}

                    <form onSubmit={handleSubmit} className="space-y-4">
                        <div>
                            <label className="block text-xs font-medium text-app-muted mb-1.5">Email</label>
                            <input
                                type="email"
                                required
                                value={email}
                                onChange={(e) => setEmail(e.target.value)}
                                className="w-full bg-app-base border border-app-border rounded-lg px-3 py-2 text-sm text-app-text placeholder-app-muted focus:outline-none focus:border-app-accent focus:ring-1 focus:ring-app-accent transition-colors"
                                placeholder="you@company.com"
                            />
                        </div>
                        <div>
                            <label className="block text-xs font-medium text-app-muted mb-1.5">Password</label>
                            <input
                                type="password"
                                required
                                value={password}
                                onChange={(e) => setPassword(e.target.value)}
                                className="w-full bg-app-base border border-app-border rounded-lg px-3 py-2 text-sm text-app-text placeholder-app-muted focus:outline-none focus:border-app-accent focus:ring-1 focus:ring-app-accent transition-colors"
                                placeholder="••••••••"
                            />
                        </div>

                        {error && <p className="text-sm text-app-bug">{error}</p>}

                        <button
                            type="submit"
                            disabled={isSubmitting}
                            className="w-full bg-app-text text-app-base font-medium text-sm py-2.5 rounded-lg hover:bg-gray-200 transition-colors mt-2 disabled:opacity-50 disabled:cursor-not-allowed"
                        >
                            {isSubmitting ? 'Signing in...' : 'Sign In'}
                        </button>
                    </form>
                </div>

                <p className="text-center text-xs text-app-muted mt-6">
                    Don't have an account?{' '}
                    <Link to="/register" className="text-app-text hover:underline">
                        Sign up
                    </Link>
                </p>
            </div>
        </div>
    )
}