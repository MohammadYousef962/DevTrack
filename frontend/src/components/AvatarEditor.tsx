import { useEffect, useRef, useState, type ChangeEvent } from 'react'
import { Camera } from 'lucide-react'
import { removeMyAvatar, uploadMyAvatar } from '../api/users'
import { getErrorMessage } from '../api/errors'
import { ACCEPTED_AVATAR_TYPES, prepareAvatar } from '../lib/avatarImage'
import { Avatar } from './Avatar'
import { ConfirmDialog } from './ConfirmDialog'
import { Modal } from './Modal'
import type { UserResponse } from '../types/user'

interface AvatarEditorProps {
    profile: UserResponse
    onChanged: (updated: UserResponse) => void
}

// The profile picture itself is the control: click it, choose a file, preview it, then save.
export function AvatarEditor({ profile, onChanged }: AvatarEditorProps) {
    const fileInput = useRef<HTMLInputElement>(null)
    const [pending, setPending] = useState<{ blob: Blob; previewUrl: string } | null>(null)
    const [notice, setNotice] = useState('')
    const [saveError, setSaveError] = useState('')
    const [isSaving, setIsSaving] = useState(false)
    const [confirmRemove, setConfirmRemove] = useState(false)
    const [removeError, setRemoveError] = useState('')
    const [isRemoving, setIsRemoving] = useState(false)

    // Frees the preview picture when it is replaced, dismissed or the page is left.
    useEffect(() => {
        return () => {
            if (pending) URL.revokeObjectURL(pending.previewUrl)
        }
    }, [pending])

    async function handleFileChosen(e: ChangeEvent<HTMLInputElement>) {
        const file = e.target.files?.[0]
        // Clearing the input means choosing the same file again still counts as a change.
        e.target.value = ''
        if (!file) return
        setNotice('')
        setSaveError('')
        try {
            const blob = await prepareAvatar(file)
            setPending({ blob, previewUrl: URL.createObjectURL(blob) })
        } catch (err) {
            setNotice(err instanceof Error ? err.message : "That picture couldn't be used.")
        }
    }

    async function handleSave() {
        if (!pending) return
        setSaveError('')
        setIsSaving(true)
        try {
            onChanged(await uploadMyAvatar(pending.blob))
            setPending(null)
        } catch (err) {
            setSaveError(getErrorMessage(err))
        } finally {
            setIsSaving(false)
        }
    }

    async function handleRemove() {
        setRemoveError('')
        setIsRemoving(true)
        try {
            onChanged(await removeMyAvatar())
            setConfirmRemove(false)
        } catch (err) {
            setRemoveError(getErrorMessage(err))
        } finally {
            setIsRemoving(false)
        }
    }

    return (
        <div className="flex flex-col items-center gap-2 shrink-0">
            <button
                type="button"
                onClick={() => fileInput.current?.click()}
                aria-label="Change profile picture"
                title="Change profile picture"
                className="group relative rounded-full focus:outline-none focus-visible:ring-2 focus-visible:ring-app-accent focus-visible:ring-offset-2 focus-visible:ring-offset-app-surface"
            >
                <Avatar name={profile.fullName} imageUrl={profile.profileImageUrl} size="xl" />
                <span className="absolute bottom-0 right-0 flex h-7 w-7 items-center justify-center rounded-full border border-app-border bg-app-surface text-app-muted shadow transition-colors group-hover:text-app-text">
                    <Camera className="w-3.5 h-3.5" />
                </span>
            </button>

            <input
                ref={fileInput}
                type="file"
                accept={ACCEPTED_AVATAR_TYPES.join(',')}
                onChange={handleFileChosen}
                className="hidden"
            />

            {profile.profileImageUrl && (
                <button
                    type="button"
                    onClick={() => {
                        setRemoveError('')
                        setConfirmRemove(true)
                    }}
                    className="text-xs text-app-muted hover:text-app-bug transition-colors"
                >
                    Remove photo
                </button>
            )}
            {notice && <p className="text-xs text-app-bug max-w-[10rem] text-center">{notice}</p>}

            <Modal isOpen={pending !== null} onClose={() => !isSaving && setPending(null)} title="New profile picture">
                {pending && (
                    <div className="space-y-4">
                        <img
                            src={pending.previewUrl}
                            alt="Preview of your new profile picture"
                            className="w-32 h-32 rounded-full object-cover mx-auto"
                        />
                        <p className="text-xs text-app-muted text-center">
                            The picture is cropped to a square. Administrators see it in the user list; everywhere else people see your initials.
                        </p>
                        {saveError && <p className="text-sm text-app-bug">{saveError}</p>}
                        <div className="flex gap-2">
                            <button
                                type="button"
                                onClick={() => setPending(null)}
                                disabled={isSaving}
                                className="flex-1 border border-app-border text-app-text text-sm font-medium py-2 rounded-lg hover:bg-app-hover transition-colors disabled:opacity-50"
                            >
                                Cancel
                            </button>
                            <button
                                type="button"
                                onClick={handleSave}
                                disabled={isSaving}
                                className="flex-1 bg-app-accent text-white text-sm font-medium py-2 rounded-lg hover:bg-app-accentHover transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
                            >
                                {isSaving ? 'Saving...' : 'Use this picture'}
                            </button>
                        </div>
                    </div>
                )}
            </Modal>

            <ConfirmDialog
                isOpen={confirmRemove}
                title="Remove profile picture"
                message="Remove your profile picture? Your initials will be shown instead."
                confirmLabel="Remove"
                isProcessing={isRemoving}
                error={removeError}
                onConfirm={handleRemove}
                onCancel={() => setConfirmRemove(false)}
            />
        </div>
    )
}