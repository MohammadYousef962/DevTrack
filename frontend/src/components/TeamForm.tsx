import { useState, type FormEvent } from 'react'
import { getErrorMessage } from '../api/errors'
import type { SaveTeamRequest } from '../types/team'

interface TeamFormProps {
    initialName?: string
    initialDescription?: string
    submitLabel: string
    submittingLabel: string
    onSubmit: (values: SaveTeamRequest) => Promise<void>
}

const inputClass =
    'w-full bg-app-base border border-app-border rounded-lg px-3 py-2 text-sm text-app-text focus:outline-none focus:border-app-accent focus:ring-1 focus:ring-app-accent'

export function TeamForm({
    initialName = '',
    initialDescription = '',
    submitLabel,
    submittingLabel,
    onSubmit,
}: TeamFormProps) {
    const [name, setName] = useState(initialName)
    const [description, setDescription] = useState(initialDescription)
    const [error, setError] = useState('')
    const [isSubmitting, setIsSubmitting] = useState(false)

    async function handleSubmit(e: FormEvent) {
        e.preventDefault()
        const trimmedName = name.trim()
        if (!trimmedName) return
        setError('')
        setIsSubmitting(true)
        try {
            await onSubmit({ name: trimmedName, description: description.trim() || null })
        } catch (err) {
            setError(getErrorMessage(err))
        } finally {
            setIsSubmitting(false)
        }
    }

    return (
        <form onSubmit={handleSubmit} className="space-y-4">
            <div>
                <label className="block text-xs font-medium text-app-muted mb-1.5">Name</label>
                <input
                    type="text"
                    required
                    maxLength={150}
                    value={name}
                    onChange={(e) => setName(e.target.value)}
                    className={inputClass}
                />
            </div>
            <div>
                <label className="block text-xs font-medium text-app-muted mb-1.5">Description</label>
                <textarea
                    rows={3}
                    maxLength={1000}
                    value={description}
                    onChange={(e) => setDescription(e.target.value)}
                    className={`${inputClass} resize-none`}
                />
            </div>

            {error && <p className="text-sm text-app-bug">{error}</p>}

            <button
                type="submit"
                disabled={isSubmitting || !name.trim()}
                className="w-full bg-app-accent text-white font-medium text-sm py-2.5 rounded-lg hover:bg-app-accentHover transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
            >
                {isSubmitting ? submittingLabel : submitLabel}
            </button>
        </form>
    )
}