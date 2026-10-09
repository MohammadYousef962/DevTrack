import { useCallback, useEffect, useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { Plus, Users, Loader2 } from 'lucide-react'
import { createTeam, getTeams } from '../api/teams'
import { getErrorMessage } from '../api/errors'
import { useAuth } from '../context/AuthContext'
import { Modal } from '../components/Modal'
import { TeamForm } from '../components/TeamForm'
import type { TeamResponse } from '../types/team'

export function TeamsPage() {
    const { user } = useAuth()
    const navigate = useNavigate()
    const [teams, setTeams] = useState<TeamResponse[]>([])
    const [isLoading, setIsLoading] = useState(true)
    const [loadError, setLoadError] = useState('')
    const [isModalOpen, setIsModalOpen] = useState(false)

    const canCreate = user?.role === 'Admin' || user?.role === 'ProjectManager'

    const loadTeams = useCallback(async () => {
        setLoadError('')
        try {
            setTeams(await getTeams())
        } catch (err) {
            setLoadError(getErrorMessage(err))
        } finally {
            setIsLoading(false)
        }
    }, [])

    useEffect(() => {
        loadTeams()
    }, [loadTeams])

    return (
        <div className="p-6 md:p-8 max-w-6xl mx-auto space-y-6">
            <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 border-b border-app-border pb-4">
                <div>
                    <h1 className="text-2xl font-semibold tracking-tight text-app-text">Teams</h1>
                    <p className="text-sm text-app-muted mt-1">
                        {user?.role === 'Admin' ? 'Every team in DevTrack.' : 'Every team you own or belong to.'}
                    </p>
                </div>
                {canCreate && (
                    <button
                        onClick={() => setIsModalOpen(true)}
                        className="bg-app-text text-app-base px-3 py-1.5 rounded-md text-sm font-medium hover:bg-gray-200 transition-colors flex items-center justify-center gap-2"
                    >
                        <Plus className="w-4 h-4" /> New Team
                    </button>
                )}
            </div>

            {isLoading && (
                <div className="flex items-center justify-center py-16 text-app-muted">
                    <Loader2 className="w-5 h-5 animate-spin mr-2" /> Loading teams...
                </div>
            )}

            {!isLoading && loadError && (
                <div className="text-center py-16">
                    <p className="text-app-bug text-sm mb-3">{loadError}</p>
                    <button
                        onClick={() => {
                            setIsLoading(true)
                            loadTeams()
                        }}
                        className="text-sm text-app-accent hover:underline"
                    >
                        Try again
                    </button>
                </div>
            )}

            {!isLoading && !loadError && teams.length === 0 && (
                <div className="text-center py-16 border border-dashed border-app-border rounded-xl">
                    <p className="text-app-text font-medium mb-1">No teams yet</p>
                    <p className="text-app-muted text-sm">
                        {canCreate
                            ? 'Create your first team to get started.'
                            : "You're not on any team yet. Ask a team owner to add you."}
                    </p>
                </div>
            )}

            {!isLoading && !loadError && teams.length > 0 && (
                <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
                    {teams.map((team) => (
                        <Link
                            key={team.id}
                            to={`/teams/${team.id}`}
                            className="bg-app-surface border border-app-border rounded-xl p-5 flex flex-col h-full hover:border-app-borderLighter transition-colors"
                        >
                            <div className="flex justify-between items-start mb-3">
                                <div className="w-9 h-9 rounded-lg bg-app-accent/10 border border-app-accent/20 flex items-center justify-center text-app-accent">
                                    <Users className="w-4 h-4" />
                                </div>
                                {team.ownerId === user?.userId && (
                                    <span className="px-2 py-0.5 rounded text-[10px] font-semibold border border-app-accent/30 text-app-accent bg-app-accent/10">
                                        Owner
                                    </span>
                                )}
                            </div>
                            <h3 className="text-base font-semibold text-app-text mb-1">{team.name}</h3>
                            <p className="text-sm text-app-muted line-clamp-2 mb-4 flex-1">
                                {team.description || 'No description.'}
                            </p>
                            <div className="flex items-center justify-between text-xs text-app-muted pt-3 border-t border-app-border/50">
                                <span>
                                    {team.memberCount} member{team.memberCount === 1 ? '' : 's'}
                                </span>
                                <span className="truncate ml-2">Owner: {team.ownerName}</span>
                            </div>
                        </Link>
                    ))}
                </div>
            )}

            <Modal isOpen={isModalOpen} onClose={() => setIsModalOpen(false)} title="New Team">
                <TeamForm
                    submitLabel="Create Team"
                    submittingLabel="Creating..."
                    onSubmit={async (values) => {
                        const created = await createTeam(values)
                        navigate(`/teams/${created.id}`)
                    }}
                />
            </Modal>
        </div>
    )
}