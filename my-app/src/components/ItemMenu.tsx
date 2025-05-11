
import {  Player } from "../vite-env"
import ItemCard from "./ItemCard"


interface Props {
    playerObj: Player
    showItemsMenu: React.Dispatch<React.SetStateAction<boolean>>
}

function ItemMenu({ playerObj, showItemsMenu }: Props) {

    return (
        <div className="item-menu">
            <span className="delete-button" onClick={() => { showItemsMenu(false) }}></span>
            {Object.values(playerObj.inventory).map((item) =>
                <ItemCard item={item} showItemsMenu={showItemsMenu} />
            )}
        </div>
    )
}

export default ItemMenu