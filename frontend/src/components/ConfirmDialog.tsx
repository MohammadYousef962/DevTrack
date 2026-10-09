import { useEffect } from 'react'
import { Loader2 } from 'lucide-react'
import { Modal } from './Modal'

interface ConfirmDialogProps {
    isOpen: boolean
    title: string
    message: string
    confirmLabel: string
    isProcessing: boolean
    error: string
    onConfirm: () => void
    onCancel: () => void
}

export function ConfirmDialog({
    isOpen,
    title,
    message,
    confirmLabel,
    isProcessing,
    error,
    onConfirm,
    onCancel,
}: ConfirmDialogProps) {
    useEffect(() => {
        if (!isOpen) return
        function handleKeyDown(e: globalThis.KeyboardEvent) {
            if (e.key === 'Escape' && !isProcessing) onCancel()
        }
        window.addEventListener('keydown', handleKeyDown)
        return () => window.removeEventListener('keydown', handleKeyDown)
    }, [isOpen, isProcessing, onCancel])

    return (
        <Modal isOpen={isOpen} onClose={() => !isProcessing && onCancel()} title={title}>
            <p className="text-sm text-app-muted mb-4">{message}</p>
            {error && <p className="text-sm text-app-bug mb-4">{error}</p>}
            <div className="flex justify-end gap-2">
                <button
                    type="button"
                    onClick={onCancel}
                    disabled={isProcessing}
                    className="px-3 py-1.5 rounded-md text-sm text-app-text border border-app-border hover:bg-app-hover transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
                >
                    Cancel
                </button>
                <button
                    type="button"
                    onClick={onConfirm}
                    disabled={isProcessing}
                    className="px-3 py-1.5 rounded-md text-sm font-medium text-white bg-app-bug hover:opacity-90 transition-opacity flex items-center gap-2 disabled:opacity-50 disabled:cursor-not-allowed"
                >
                    {isProcessing && <Loader2 className="w-3.5 h-3.5 animate-spin" />}
                    {confirmLabel}
                </button>
            </div>
        </Modal>
    )
}