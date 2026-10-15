import { useState } from 'react'

interface AvatarProps {
    name: string
    imageUrl?: string | null
    size?: 'sm' | 'md' | 'lg'
}

const sizeClasses = {
    sm: 'w-7 h-7 text-xs',
    md: 'w-9 h-9 text-sm',
    lg: 'w-16 h-16 text-xl',
}

export function Avatar({ name, imageUrl, size = 'sm' }: AvatarProps) {
    const [failedUrl, setFailedUrl] = useState<string | null>(null)

    return (
        <div
            className={`${sizeClasses[size]} rounded-full overflow-hidden shrink-0 bg-app-accent flex items-center justify-center text-white font-semibold`}
        >
            {imageUrl && imageUrl !== failedUrl ? (
                <img
                    src={imageUrl}
                    alt=""
                    referrerPolicy="no-referrer"
                    onError={() => setFailedUrl(imageUrl)}
                    className="w-full h-full object-cover"
                />
            ) : (
                name.charAt(0) || '?'
            )}
        </div>
    )
}