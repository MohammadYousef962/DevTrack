import { useCallback, useEffect, useState, type FormEvent } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { ChevronRight, Loader2, Pencil, Trash2, UserPlus } from 'lucide-react'
import {
    addTeamMember,
    deleteTeam,
    getTeam,
    getTeamMembers,
    removeTeamMember,
    updateTeam,
} from '../api/teams'
import { lookupUserByEmail } from '../api/users'
import { getErrorMessage } from '../api/errors'
import { useAuth } from '../context/AuthContext'
import { parseApiDate } from '../lib/dates'
import { Modal } from '../components/Modal'
import { ConfirmDialog } from '../components/ConfirmDialog'
import { TeamForm } from '../components/TeamForm'
import type { TeamMemberResponse, TeamResponse } from '../types/team'

type DeleteTarget = { kind: 'team' } | { kind: 'member'; member: TeamMemberResponse }

export function TeamDetailPage() {
    const { teamId } = useParams()
    const id = Number(teamId)
    const { user } = useAuth()
    const navigate = useNavigate()

    const [team, setTeam] = useState<TeamResponse | null>(null)
    const [members, setMembers] = useState<TeamMemberResponse[]>([])
    const [isLoading, setIsLoading] = useState(true)
    const [loadError, setLoadError] = useState('')

    const [isEditOpen, setIsEditOpen] = useState(false)

    const [newMemberEmail, setNewMemberEmail] = useState('')
    const [addError, setAddError] = useState('')
    const [isAdding, setIsAdding] = useState(false)

    const [deleteTarget, setDeleteTarget] = useState<DeleteTarget | null>(null)
    const [deleteError, setDeleteError] = useState('')
    const [isDeleting, setIsDeleting] = useState(false)

    const loadTeam = useCallback(async () => {
        setLoadError('')
        try {
            const [teamData, membersData] = await Promise.all([getTeam(id), getTeamMembers(id)])
            setTeam(teamData)
            setMembers(membersData)
        } catch (err) {
            setLoadError(getErrorMessage(err))
        } finally {
            setIsLoading(false)
        }
    }, [id])

    useEffect(() => {
        loadTeam()
    }, [loadTeam])

    async function handleAddMember(e: FormEvent) {
        e.preventDefault()
        const email = newMemberEmail.trim()
        if (!email) return
        setAddError('')
        setIsAdding(true)
        try {
            const found = await lookupUserByEmail(email)
            const added = await addTeamMember(id, found.id)
            setMembers((prev) => [...prev, added])
            setNewMemberEmail('')
        } catch (err) {
            setAddError(getErrorMessage(err))
        } finally {
            setIsAdding(false)
        }
    }

    async function handleConfirmDelete() {
        if (!deleteTarget) return
        setDeleteError('')
        setIsDeleting(true)
        try {
            if (deleteTarget.kind === 'team') {
                await deleteTeam(id)
                navigate('/teams')
                return
            }
            const userId = deleteTarget.member.userId
            await removeTeamMember(id, userId)
            setMembers((prev) => prev.filter((m) => m.userId !== userId))
            setDeleteTarget(null)
        } catch (err) {
            setDeleteError(getErrorMessage(err))
        } finally {
            setIsDeleting(false)
        }
    }

    if (isLoading) {
        return (
            <div className="flex items-center justify-center py-24 text-app-muted">
                <Loader2 className="w-5 h-5 animate-spin mr-2" /> Loading team...
            </div>
        )
    }

    if (loadError || !team) {
        return (
            <div className="text-center py-24">
                <p className="text-app-bug text-sm mb-3">{loadError || 'Team not found.'}</p>
                <div className="flex items-center justify-center gap-4 text-sm">
                    <button
                        onClick={() => {
                            setIsLoading(true)
                            loadTeam()
                        }}
                        className="text-app-accent hover:underline"
                    >
                        Try again
                    </button>
                    <Link to="/teams" className="text-app-muted hover:text-app-text">
                        Back to teams
                    </Link>
                </div>
            </div>
        )
    }

    const canManage = user?.role === 'Admin' || team.ownerId === user?.userId

    const deleteMessage =
        deleteTarget?.kind === 'member'
            ? `Remove ${deleteTarget.member.fullName} from "${team.name}"? You can add them back later.`
            : `Delete "${team.name}"? Its memberships will be removed too. A team that still has projects can't be deleted. This can't be undone.`

    return (
        <div className="p-6 md:p-8 max-w-4xl mx-auto space-y-8">
            <div>
                <div className="flex items-center gap-2 text-sm text-app-muted mb-4">
                    <Link to="/teams" className="hover:text-app-text transition-colors">
                        Teams
                    </Link>
                    <ChevronRight className="w-3 h-3" />
                    <span className="text-app-text font-medium">{team.name}</span>
                </div>

                <div className="flex flex-col sm:flex-row sm:items-start justify-between gap-4">
                    <div className="min-w-0">
                        <h1 className="text-2xl font-semibold tracking-tight text-app-text break-words">{team.name}</h1>
                        <p className="text-sm text-app-muted mt-2 whitespace-pre-wrap break-words">
                            {team.description || 'No description.'}
                        </p>
                        <p className="text-xs text-app-muted mt-3">
                            Owned by {team.ownerName} &middot; Created {parseApiDate(team.createdAt).toLocaleDateString()}
                        </p>
                    </div>
                    {canManage && (
                        <div className="flex items-center gap-2 shrink-0">
                            <button
                                onClick={() => setIsEditOpen(true)}
                                className="flex items-center gap-1.5 px-2.5 py-1.5 rounded-md text-xs font-medium text-app-muted border border-app-border hover:text-app-text hover:bg-app-hover transition-colors"
                            >
                                <Pencil className="w-3.5 h-3.5" /> Edit
                            </button>
                            <button
                                onClick={() => {
                                    setDeleteError('')
                                    setDeleteTarget({ kind: 'team' })
                                }}
                                className="flex items-center gap-1.5 px-2.5 py-1.5 rounded-md text-xs font-medium text-app-bug border border-app-border hover:bg-app-bug/10 transition-colors"
                            >
                                <Trash2 className="w-3.5 h-3.5" /> Delete
                            </button>
                        </div>
                    )}
                </div>
            </div>

            <div className="space-y-3">
                <h2 className="text-sm font-semibold tracking-wide uppercase text-app-muted">
                    Members ({members.length})
                </h2>
                <div className="bg-app-surface border border-app-border rounded-xl overflow-hidden">
                    <div className="divide-y divide-app-border">
                        {members.map((m) => (
                            <div key={m.userId} className="p-3 flex items-center gap-3">
                                <div className="w-8 h-8 rounded-full bg-app-accent flex items-center justify-center text-white text-xs font-semibold shrink-0">
                                    {m.fullName.charAt(0)}
                                </div>
                                <div className="min-w-0 flex-1">
                                    <p className="text-sm font-medium text-app-text truncate">
                                        {m.fullName}
                                        {m.userId === user?.userId ? ' (you)' : ''}
                                    </p>
                                    <p className="text-xs text-app-muted truncate">{m.email}</p>
                                </div>
                                {m.userId === team.ownerId ? (
                                    <span className="px-2 py-0.5 rounded text-[10px] font-semibold border border-app-accent/30 text-app-accent bg-app-accent/10">
                                        Owner
                                    </span>
                                ) : canManage ? (
                                    <button
                                        onClick={() => {
                                            setDeleteError('')
                                            setDeleteTarget({ kind: 'member', member: m })
                                        }}
                                        className="text-xs text-app-muted hover:text-app-bug transition-colors"
                                    >
                                        Remove
                                    </button>
                                ) : null}
                            </div>
                        ))}
                    </div>

                    {canManage && (
                        <form
                            onSubmit={handleAddMember}
                            className={`p-3 flex gap-2 ${members.length > 0 ? 'border-t border-app-border' : ''}`}
                        >
                            <input
                                type="email"
                                value={newMemberEmail}
                                onChange={(e) => setNewMemberEmail(e.target.value)}
                                placeholder="Add a member by email"
                                className="flex-1 min-w-0 bg-app-base border border-app-border rounded-lg px-3 py-1.5 text-sm text-app-text placeholder-app-muted focus:outline-none focus:border-app-accent focus:ring-1 focus:ring-app-accent"
                            />
                            <button
                                type="submit"
                                disabled={isAdding || !newMemberEmail.trim()}
                                className="flex items-center gap-1.5 bg-app-accent text-white px-3 py-1.5 rounded-lg text-sm font-medium hover:bg-app-accentHover transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
                            >
                                {isAdding ? <Loader2 className="w-4 h-4 animate-spin" /> : <UserPlus className="w-4 h-4" />}
                                Add
                            </button>
                        </form>
                    )}
                </div>
                {addError && <p className="text-sm text-app-bug">{addError}</p>}
            </div>

            <Modal isOpen={isEditOpen} onClose={() => setIsEditOpen(false)} title="Edit Team">
                <TeamForm
                    initialName={team.name}
                    initialDescription={team.description ?? ''}
                    submitLabel="Save changes"
                    submittingLabel="Saving..."
                    onSubmit={async (values) => {
                        const updated = await updateTeam(id, values)
                        setTeam(updated)
                        setIsEditOpen(false)
                    }}
                />
            </Modal>

            <ConfirmDialog
                isOpen={deleteTarget !== null}
                title={deleteTarget?.kind === 'member' ? 'Remove member' : 'Delete team'}
                message={deleteMessage}
                confirmLabel={deleteTarget?.kind === 'member' ? 'Remove' : 'Delete'}
                isProcessing={isDeleting}
                error={deleteError}
                onConfirm={handleConfirmDelete}
                onCancel={() => setDeleteTarget(null)}
            />
        </div>
    )
}