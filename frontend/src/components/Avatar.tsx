import { useEffect, useState } from 'react'
import { downloadAvatar } from '../api/users'

const sizeClasses = {
    sm: 'w-8 h-8 text-xs',
    md: 'w-10 h-10 text-sm',
    lg: 'w-16 h-16 text-xl',
    xl: 'w-24 h-24 text-3xl',
}

interface AvatarProps {
    name: string
    imageUrl?: string | null
    size?: keyof typeof sizeClasses
}

// Downloaded pictures are kept for the session, so the sidebar and the profile page share one download.
const pictureCache = new Map<string, Promise<string>>()

function loadPicture(path: string): Promise<string> {
    let picture = pictureCache.get(path)
    if (!picture) {
        picture = downloadAvatar(path).then((blob) => URL.createObjectURL(blob))
        // A failed download isn't remembered, so a later render can try again.
        picture.catch(() => pictureCache.delete(path))
        pictureCache.set(path, picture)
    }
    return picture
}

export function Avatar({ name, imageUrl, size = 'sm' }: AvatarProps) {
    // Uploaded pictures live behind the API. Anything else (such as an old saved value) is ignored.
    const path = imageUrl && imageUrl.startsWith('/api/') ? imageUrl : null
    const [loaded, setLoaded] = useState<{ path: string; src: string } | null>(null)

    useEffect(() => {
        if (!path) return
        let cancelled = false
        loadPicture(path)
            .then((src) => {
                if (!cancelled) setLoaded({ path, src })
            })
            .catch(() => undefined) // The initials stay on screen if the picture can't be loaded.
        return () => {
            cancelled = true
        }
    }, [path])

    const src = path && loaded?.path === path ? loaded.src : null

    return (
        <div
            className={`${sizeClasses[size]} rounded-full overflow-hidden shrink-0 bg-app-accent text-white font-semibold flex items-center justify-center`}
        >
            {src ? <img src={src} alt="" className="w-full h-full object-cover" /> : name.charAt(0) || '?'}
        </div>
    )
}