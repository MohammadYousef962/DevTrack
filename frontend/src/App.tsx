import { Layers } from 'lucide-react'

function App() {
    return (
        <div className="h-screen w-full flex items-center justify-center bg-app-base">
            <div className="flex items-center gap-2">
                <div className="w-8 h-8 rounded-md bg-app-accent flex items-center justify-center text-white shadow-glow">
                    <Layers className="w-5 h-5" />
                </div>
                <span className="text-2xl font-bold tracking-tight text-app-text">DevTrack</span>
            </div>
        </div>
    )
}

export default App