import { useEffect, useState } from "react"
import { Player } from "../vite-env"


export default function usePlayer(id: string) {
    const [player, setPlayer] = useState<Player | null>(null)
    const [loading, setLoading] = useState(true)
    const [error, setError] = useState<any>(null)
    useEffect(() => {
        const fetchPlayer = async () => {
            const payload = {
                id: id
            }
            try {
                const res = await fetch(`http://${window.location.hostname}:5050/get-player`, { credentials: "include", body: JSON.stringify(payload), method: "POST", headers: { "Content-type": "application/json" } })
                if (!res.ok) {
                    const fetchError = await res.text()
                    throw new Error(fetchError)
                }
                const fetchedPlayer = await res.json() as unknown as Player
                if (!fetchedPlayer.id) {
                    throw new Error("Error with player data")
                }
                setPlayer(fetchedPlayer)
                setLoading(false)
            } catch (error: any) {
                setError(error)
                setLoading(false)
                console.log(error.toString())
            }
        }
        fetchPlayer()
    }, [])

    return { player, loading, error }
}