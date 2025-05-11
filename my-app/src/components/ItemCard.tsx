
import useWebSocket from "react-use-websocket"
import { useGameState } from "../GameContext"
import { ActionPayload, Item, Message } from "../vite-env"
import { gameId, socketUrl } from "../App"

interface Props {
    item: Item
    showItemsMenu: React.Dispatch<React.SetStateAction<boolean>> 
}

function ItemCard({ item, showItemsMenu }: Props) {
    const { sendJsonMessage } = useWebSocket(socketUrl, { share: true })
    const { player, turn, playerTurn } = useGameState()
    const invalid = (item.owned <= 0 || player !== playerTurn)
    return (
        <>
            <button className="item-card" onClick={() => {
                const payload = HandleItem(item)
                sendJsonMessage(payload)
                showItemsMenu(false)
            }} disabled={invalid} style={invalid ? { cursor: "not-allowed" } : undefined}>
                <span className="item-name"> {item.name}</span>
                <span className="item-desc"> {item.description}</span>
                <span className="item-amount">Owned: {item.owned}</span>
            </button>
        </>
    )
}

function HandleItem(item: Item) {
    const payload: ActionPayload = {
        action: "useItem",
        item: item.name
    }
    const message: Message = {
        id: gameId,
        type: "action",
        payload: payload
    }
    return message
}

export default ItemCard