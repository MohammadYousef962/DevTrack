import { useEffect, useState, type FormEvent, type ReactNode } from 'react'
import { Loader2 } from 'lucide-react'
import { changePassword, getMe, updateMyProfile } from '../api/users'
import { getErrorMessage } from '../api/errors'
import { useAuth } from '../context/AuthContext'
import { parseApiDate } from '../lib/dates'
import { roleLabel, roleStyle } from '../lib/roles'
import { Avatar } from '../components/Avatar'
import type { UserResponse } from '../types/user'

const inputClass =
    'w-full bg-app-base border border-app-border rounded-lg px-3 py-2 text-sm text-app-text focus:outline-none focus:border-app-accent focus:ring-1 focus:ring-app-accent'

export function ProfilePage() {
    const { updateUser } = useAuth()
    const [profile, setProfile] = useState<UserResponse | null>(null)
    const [isLoading, setIsLoading] = useState(true)
    const [loadError, setLoadError] = useState('')
    const [attempt, setAttempt] = useState(0)

    useEffect(() => {
        let cancelled = false
        getMe()
            .then((me) => {
                if (cancelled) return
                setProfile(me)
                setLoadError('')
                // Keep the sidebar in step, including for sessions saved before profile pictures existed.
                updateUser({ fullName: me.fullName, profileImageUrl: me.profileImageUrl })
            })
            .catch((err) => {
                if (!cancelled) setLoadError(getErrorMessage(err))
            })
            .finally(() => {
                if (!cancelled) setIsLoading(false)
            })
        return () => {
            cancelled = true
        }
    }, [attempt, updateUser])

    function retry() {
        setIsLoading(true)
        setAttempt((n) => n + 1)
    }

    function handleSaved(updated: UserResponse) {
        setProfile(updated)
        updateUser({ fullName: updated.fullName, profileImageUrl: updated.profileImageUrl })
    }

    if (isLoading) {
        return (
            <div className="flex items-center justify-center py-24 text-app-muted">
                <Loader2 className="w-5 h-5 animate-spin mr-2" /> Loading profile...
            </div>
        )
    }

    if (loadError || !profile) {
        return (
            <div className="text-center py-24">
                <p className="text-app-bug text-sm mb-3">{loadError || 'Could not load your profile.'}</p>
                <button onClick={retry} className="text-sm text-app-accent hover:underline">
                    Try again
                </button>
            </div>
        )
    }

    return (
        <div className="p-6 md:p-8 max-w-2xl mx-auto space-y-8">
            <header>
                <h1 className="text-2xl font-semibold tracking-tight text-app-text">Profile</h1>
                <p className="text-sm text-app-muted mt-1">Your account details and password.</p>
            </header>

            <div className="bg-app-surface border border-app-border rounded-xl p-5 flex items-center gap-4">
                <Avatar name={profile.fullName} imageUrl={profile.profileImageUrl} size="lg" />
                <div className="min-w-0">
                    <p className="text-lg font-semibold text-app-text truncate">{profile.fullName}</p>
                    <p className="text-sm text-app-muted truncate">{profile.email}</p>
                    <div className="flex items-center gap-2 mt-2 flex-wrap">
                        <span className={`px-2 py-0.5 rounded text-[10px] font-semibold border ${roleStyle(profile.role)}`}>
                            {roleLabel(profile.role)}
                        </span>
                        <span className="text-xs text-app-muted">
                            Member since {parseApiDate(profile.createdAt).toLocaleDateString()}
                        </span>
                    </div>
                </div>
            </div>

            <Section title="Edit profile">
                <ProfileForm profile={profile} onSaved={handleSaved} />
            </Section>

            <Section title="Change password">
                <PasswordForm />
            </Section>
        </div>
    )
}

function Section({ title, children }: { title: string; children: ReactNode }) {
    return (
        <section className="space-y-3">
            <h2 className="text-sm font-semibold tracking-wide uppercase text-app-muted">{title}</h2>
            {children}
        </section>
    )
}

function ProfileForm({ profile, onSaved }: { profile: UserResponse; onSaved: (profile: UserResponse) => void }) {
    const [fullName, setFullName] = useState(profile.fullName)
    const [imageUrl, setImageUrl] = useState(profile.profileImageUrl ?? '')
    const [error, setError] = useState('')
    const [saved, setSaved] = useState(false)
    const [isSaving, setIsSaving] = useState(false)

    async function handleSubmit(e: FormEvent) {
        e.preventDefault()
        const name = fullName.trim()
        if (!name) return
        setError('')
        setSaved(false)
        setIsSaving(true)
        try {
            const updated = await updateMyProfile({ fullName: name, profileImageUrl: imageUrl.trim() || null })
            onSaved(updated)
            setSaved(true)
        } catch (err) {
            setError(getErrorMessage(err))
        } finally {
            setIsSaving(false)
        }
    }

    return (
        <form onSubmit={handleSubmit} className="space-y-4">
            <div>
                <label className="block text-xs font-medium text-app-muted mb-1.5">Full name</label>
                <input
                    type="text"
                    required
                    maxLength={150}
                    value={fullName}
                    onChange={(e) => {
                        setFullName(e.target.value)
                        setSaved(false)
                    }}
                    className={inputClass}
                />
            </div>
            <div>
                <label className="block text-xs font-medium text-app-muted mb-1.5">Profile picture link</label>
                <input
                    type="url"
                    maxLength={2048}
                    value={imageUrl}
                    onChange={(e) => {
                        setImageUrl(e.target.value)
                        setSaved(false)
                    }}
                    placeholder="https://example.com/me.png"
                    className={inputClass}
                />
                <p className="text-xs text-app-muted mt-1.5">
                    A link to a picture. For now only you see it; other people see your initials. Leave it empty to remove it.
                </p>
            </div>

            <p className="text-xs text-app-muted">Your email and role can only be changed by an administrator.</p>
            {error && <p className="text-sm text-app-bug">{error}</p>}
            {saved && <p className="text-sm text-app-done">Profile updated.</p>}

            <button
                type="submit"
                disabled={isSaving || !fullName.trim()}
                className="bg-app-accent text-white px-3 py-1.5 rounded-md text-sm font-medium hover:bg-app-accentHover transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
            >
                {isSaving ? 'Saving...' : 'Save changes'}
            </button>
        </form>
    )
}

function PasswordForm() {
    const [currentPassword, setCurrentPassword] = useState('')
    const [newPassword, setNewPassword] = useState('')
    const [confirmNewPassword, setConfirmNewPassword] = useState('')
    const [error, setError] = useState('')
    const [saved, setSaved] = useState(false)
    const [isSaving, setIsSaving] = useState(false)

    async function handleSubmit(e: FormEvent) {
        e.preventDefault()
        setError('')
        setSaved(false)
        setIsSaving(true)
        try {
            await changePassword({ currentPassword, newPassword, confirmNewPassword })
            setCurrentPassword('')
            setNewPassword('')
            setConfirmNewPassword('')
            setSaved(true)
        } catch (err) {
            setError(getErrorMessage(err))
        } finally {
            setIsSaving(false)
        }
    }

    return (
        <form onSubmit={handleSubmit} className="space-y-4">
            <div>
                <label className="block text-xs font-medium text-app-muted mb-1.5">Current password</label>
                <input
                    type="password"
                    required
                    autoComplete="current-password"
                    value={currentPassword}
                    onChange={(e) => setCurrentPassword(e.target.value)}
                    className={inputClass}
                />
            </div>
            <div>
                <label className="block text-xs font-medium text-app-muted mb-1.5">New password</label>
                <input
                    type="password"
                    required
                    minLength={8}
                    autoComplete="new-password"
                    value={newPassword}
                    onChange={(e) => setNewPassword(e.target.value)}
                    placeholder="At least 8 characters"
                    className={inputClass}
                />
            </div>
            <div>
                <label className="block text-xs font-medium text-app-muted mb-1.5">Confirm new password</label>
                <input
                    type="password"
                    required
                    autoComplete="new-password"
                    value={confirmNewPassword}
                    onChange={(e) => setConfirmNewPassword(e.target.value)}
                    className={inputClass}
                />
            </div>

            {error && <p className="text-sm text-app-bug">{error}</p>}
            {saved && <p className="text-sm text-app-done">Password updated.</p>}

            <button
                type="submit"
                disabled={isSaving}
                className="bg-app-accent text-white px-3 py-1.5 rounded-md text-sm font-medium hover:bg-app-accentHover transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
            >
                {isSaving ? 'Updating...' : 'Update password'}
            </button>
        </form>
    )
}