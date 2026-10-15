import { useEffect, useMemo, useState } from 'react'
import { Link } from 'react-router-dom'
import { Loader2, Search } from 'lucide-react'
import { getUsers, updateUserRole, updateUserStatus } from '../api/users'
import { getErrorMessage } from '../api/errors'
import { useAuth } from '../context/AuthContext'
import { parseApiDate } from '../lib/dates'
import { roleLabel, userRoles, type UserRoleName } from '../lib/roles'
import { ConfirmDialog } from '../components/ConfirmDialog'
import type { UserResponse } from '../types/user'

type PendingChange =
    | { kind: 'role'; user: UserResponse; newRole: UserRoleName }
    | { kind: 'deactivate'; user: UserResponse }

function describeChange(change: PendingChange) {
    const name = change.user.fullName

    if (change.kind === 'deactivate') {
        return {
            title: 'Deactivate user',
            confirmLabel: 'Deactivate',
            message: `Deactivate ${name}? They won't be able to sign in. A session that's already open can keep working until it expires (currently 60 minutes). You can reactivate the account at any time.`,
        }
    }

    let message = `Change ${name}'s role from ${roleLabel(change.user.role)} to ${roleLabel(change.newRole)}? It takes effect the next time they sign in.`
    if (change.newRole === 'Admin') message += ' Admins can manage every user, team and project.'
    else if (change.user.role === 'Admin') message += ' They will lose admin access.'
    return { title: 'Change role', confirmLabel: 'Change role', message }
}

export function AdminPage() {
    const { user } = useAuth()
    const isAdmin = user?.role === 'Admin'

    const [users, setUsers] = useState<UserResponse[]>([])
    const [isLoading, setIsLoading] = useState(true)
    const [loadError, setLoadError] = useState('')
    const [search, setSearch] = useState('')
    const [attempt, setAttempt] = useState(0)

    const [pending, setPending] = useState<PendingChange | null>(null)
    const [dialogError, setDialogError] = useState('')
    const [isApplying, setIsApplying] = useState(false)

    const [busyUserId, setBusyUserId] = useState<number | null>(null)
    const [actionError, setActionError] = useState('')

    useEffect(() => {
        if (!isAdmin) return
        let cancelled = false
        getUsers()
            .then((data) => {
                if (cancelled) return
                setUsers(data)
                setLoadError('')
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
    }, [isAdmin, attempt])

    const filtered = useMemo(() => {
        const q = search.trim().toLowerCase()
        if (!q) return users
        return users.filter((u) => u.fullName.toLowerCase().includes(q) || u.email.toLowerCase().includes(q))
    }, [users, search])

    function replaceUser(updated: UserResponse) {
        setUsers((prev) => prev.map((u) => (u.id === updated.id ? updated : u)))
    }

    async function applyPending() {
        if (!pending) return
        setDialogError('')
        setIsApplying(true)
        try {
            const updated =
                pending.kind === 'role'
                    ? await updateUserRole(pending.user.id, pending.newRole)
                    : await updateUserStatus(pending.user.id, false)
            replaceUser(updated)
            setPending(null)
        } catch (err) {
            setDialogError(getErrorMessage(err))
        } finally {
            setIsApplying(false)
        }
    }

    async function activate(target: UserResponse) {
        setActionError('')
        setBusyUserId(target.id)
        try {
            replaceUser(await updateUserStatus(target.id, true))
        } catch (err) {
            setActionError(getErrorMessage(err))
        } finally {
            setBusyUserId(null)
        }
    }

    if (!isAdmin) {
        return (
            <div className="text-center py-24">
                <p className="text-app-text font-medium mb-1">Admins only</p>
                <p className="text-app-muted text-sm mb-3">You don't have access to this page.</p>
                <Link to="/dashboard" className="text-sm text-app-accent hover:underline">
                    Back to dashboard
                </Link>
            </div>
        )
    }

    if (isLoading) {
        return (
            <div className="flex items-center justify-center py-24 text-app-muted">
                <Loader2 className="w-5 h-5 animate-spin mr-2" /> Loading users...
            </div>
        )
    }

    if (loadError) {
        return (
            <div className="text-center py-24">
                <p className="text-app-bug text-sm mb-3">{loadError}</p>
                <button
                    onClick={() => {
                        setIsLoading(true)
                        setAttempt((n) => n + 1)
                    }}
                    className="text-sm text-app-accent hover:underline"
                >
                    Try again
                </button>
            </div>
        )
    }

    const activeCount = users.filter((u) => u.isActive).length
    const dialog = pending ? describeChange(pending) : null

    return (
        <div className="p-6 md:p-8 max-w-5xl mx-auto space-y-6">
            <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 border-b border-app-border pb-4">
                <div>
                    <h1 className="text-2xl font-semibold tracking-tight text-app-text">Users</h1>
                    <p className="text-sm text-app-muted mt-1">
                        {users.length} user{users.length === 1 ? '' : 's'} &middot; {activeCount} active
                    </p>
                </div>
                <div className="relative w-full sm:w-72">
                    <Search className="w-4 h-4 text-app-muted absolute left-3 top-1/2 -translate-y-1/2" />
                    <input
                        type="text"
                        value={search}
                        onChange={(e) => setSearch(e.target.value)}
                        placeholder="Search by name or email"
                        className="w-full bg-app-surface border border-app-border rounded-lg pl-9 pr-3 py-2 text-sm text-app-text placeholder-app-muted focus:outline-none focus:border-app-accent focus:ring-1 focus:ring-app-accent"
                    />
                </div>
            </div>

            {actionError && <p className="text-sm text-app-bug">{actionError}</p>}

            <div className="bg-app-surface border border-app-border rounded-xl overflow-hidden">
                <div className="overflow-x-auto">
                    <table className="w-full text-sm">
                        <thead>
                            <tr className="border-b border-app-border text-left text-xs font-medium text-app-muted">
                                <th className="px-4 py-3">User</th>
                                <th className="px-4 py-3">Role</th>
                                <th className="px-4 py-3">Status</th>
                                <th className="px-4 py-3">Joined</th>
                                <th className="px-4 py-3 text-right">Actions</th>
                            </tr>
                        </thead>
                        <tbody className="divide-y divide-app-border">
                            {filtered.map((u) => {
                                const isSelf = u.id === user?.userId
                                const isBusy = busyUserId === u.id
                                return (
                                    <tr key={u.id}>
                                        <td className="px-4 py-3">
                                            <div className="flex items-center gap-3 min-w-0">
                                                <div className="w-8 h-8 rounded-full bg-app-accent flex items-center justify-center text-white text-xs font-semibold shrink-0">
                                                    {u.fullName.charAt(0)}
                                                </div>
                                                <div className="min-w-0">
                                                    <p className="font-medium text-app-text truncate">
                                                        {u.fullName}
                                                        {isSelf ? ' (you)' : ''}
                                                    </p>
                                                    <p className="text-xs text-app-muted truncate">{u.email}</p>
                                                </div>
                                            </div>
                                        </td>
                                        <td className="px-4 py-3">
                                            <select
                                                value={u.role}
                                                disabled={isSelf || isBusy}
                                                title={isSelf ? "You can't change your own role" : undefined}
                                                onChange={(e) => {
                                                    setDialogError('')
                                                    setPending({ kind: 'role', user: u, newRole: e.target.value as UserRoleName })
                                                }}
                                                className="bg-app-base border border-app-border rounded-lg px-2 py-1 text-sm text-app-text focus:outline-none focus:border-app-accent focus:ring-1 focus:ring-app-accent disabled:opacity-50 disabled:cursor-not-allowed"
                                            >
                                                {userRoles.map((r) => (
                                                    <option key={r} value={r}>
                                                        {roleLabel(r)}
                                                    </option>
                                                ))}
                                            </select>
                                        </td>
                                        <td className="px-4 py-3">
                                            <span
                                                className={`px-2 py-0.5 rounded text-[10px] font-semibold border ${u.isActive
                                                        ? 'border-app-done/30 text-app-done bg-app-done/10'
                                                        : 'border-app-border text-app-muted bg-app-hover'
                                                    }`}
                                            >
                                                {u.isActive ? 'Active' : 'Deactivated'}
                                            </span>
                                        </td>
                                        <td className="px-4 py-3 text-app-muted whitespace-nowrap">
                                            {parseApiDate(u.createdAt).toLocaleDateString()}
                                        </td>
                                        <td className="px-4 py-3 text-right whitespace-nowrap">
                                            {isSelf ? (
                                                <span className="text-xs text-app-muted" title="You can't change your own account here">
                                                    &mdash;
                                                </span>
                                            ) : isBusy ? (
                                                <Loader2 className="w-3.5 h-3.5 animate-spin text-app-muted inline" />
                                            ) : u.isActive ? (
                                                <button
                                                    onClick={() => {
                                                        setDialogError('')
                                                        setPending({ kind: 'deactivate', user: u })
                                                    }}
                                                    className="text-xs text-app-muted hover:text-app-bug transition-colors"
                                                >
                                                    Deactivate
                                                </button>
                                            ) : (
                                                <button
                                                    onClick={() => activate(u)}
                                                    className="text-xs text-app-muted hover:text-app-done transition-colors"
                                                >
                                                    Reactivate
                                                </button>
                                            )}
                                        </td>
                                    </tr>
                                )
                            })}
                        </tbody>
                    </table>
                </div>

                {filtered.length === 0 && (
                    <p className="p-8 text-sm text-app-muted text-center">
                        {users.length === 0 ? 'No users yet.' : `No users match "${search.trim()}".`}
                    </p>
                )}
            </div>

            <ConfirmDialog
                isOpen={pending !== null}
                title={dialog?.title ?? ''}
                message={dialog?.message ?? ''}
                confirmLabel={dialog?.confirmLabel ?? 'Confirm'}
                isProcessing={isApplying}
                error={dialogError}
                onConfirm={applyPending}
                onCancel={() => setPending(null)}
            />
        </div>
    )
}