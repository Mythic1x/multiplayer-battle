import { useEffect, useState, } from 'react'
import BattleStats from '../components/BattleStats'
import OpponentStats from '../components/OpponentStats'
import '../App.css'
import { Fighter, GameState, Player, ServerMessage, } from '../vite-env'
import FighterSelect from '../components/FighterSelect'
import SkillMenu from '../components/SkillMenu'
import { GameContext } from '../GameContext'
import ItemMenu from '../components/ItemMenu'

import useWebSocket, { ReadyState } from 'react-use-websocket';
import { useNavigate, useParams } from 'react-router-dom'
import Loading from '../components/Loading'

const testFighter: Fighter = {
  strength: 10,
  dexterity: 10,
  magic: 5,
  luck: 10,
  image: "https://i.pinimg.com/736x/20/33/c5/2033c5021a4ae8ff7495116820ff4f03.jpg",
  skills: {
    slash: {
      name: "Slash",
      damage: [20, 30],
      hpCost: 10,
      type: "physical",
      method: "attack",
      description: "Slash the enemy for 15 damage",
      elementType: "physical"
    },
  },
  level: 1,
  name: "testFighter",
  type: "physical",
  elementType: "physical"
}

const testFighter2: Fighter = {
  strength: 10,
  dexterity: 10,
  magic: 15,
  luck: 10,
  image: "https://i.pinimg.com/736x/20/33/c5/2033c5021a4ae8ff7495116820ff4f03.jpg",
  skills: {
    fireball: {
      name: "Fireball",
      damage: [20, 25],
      spCost: 10,
      type: "magic",
      method: "attack",
      description: "Fireball the enemy for 20 damage",
      elementType: "fire"
    },
  },
  level: 1,
  name: "testFighter2",
  type: "magic",
  elementType: "fire"
}

const dummyPlayer: Player = {
  level: 0,
  money: 0,
  xpForLevelUp: 0,
  xp: 0,
  id: 0,
  name: "No player",
  hp: 100,
  sp: 100,
  maxHp: 100,
  maxSp: 100,
  selectedFighter: testFighter,
  fighters: { testPhys: testFighter, testMagic: testFighter2 },
  inventory: {
    "health potion": {
      type: "heal",
      effectAmount: 50,
      description: "Restores 50 HP.",
      owned: 3,
      name: "Health Potion"
    },
    "mana potion": {
      type: "heal",
      effectAmount: 30,
      description: "Restores 30 SP.",
      owned: 2,
      name: "Mana Potion"
    }
  },
}

export const socketUrl = `ws://${window.location.hostname}:5050/ws`
const gameState: GameState = {
  player1: dummyPlayer,
  player2: dummyPlayer,
  turn: 1
}


export function BattlePage() {
  const [battleState, setBattleState] = useState(gameState)
  const [assignment, setAssignment] = useState<undefined | "player1" | "player2">(undefined)
  const [player, setPlayer] = useState<undefined | Player>(undefined)
  const [loading, setLoading] = useState(true)
  const [validRoom, setValidRoom] = useState(false)
  const navigate = useNavigate()
  const { roomId } = useParams<{ roomId: string }>()
  if (!roomId) return

  useEffect(() => {
    const checkRoomValidity = async (roomId: string) => {
      const res = await fetch(`http://${window.location.hostname}:5050/battle/${roomId}`, { credentials: "include" })
      if (res.ok) {
        setValidRoom(true)
        return true
      } else {
        setLoading(false)
        throw new Error("Invalid room id")
      }
    }
    const fetchPlayer = async () => {
      try {
        const res = await fetch(`http://${window.location.hostname}:5050/player`, { credentials: "include" })
        if (!res.ok) {
          const error = await res.text()
          throw new Error(error)
        }
        const fetchedPlayer = await res.json() as unknown as Player
        if (!fetchedPlayer.id) {
          throw new Error("Error with player data")
        }
        setPlayer(fetchedPlayer)
        setBattleState({ player1: fetchedPlayer, player2: dummyPlayer, turn: 1 })
        setLoading(false)
      } catch (error: any) {
        setLoading(false)
        alert(error.toString())
        console.log(error.toString())
      }
    }

    const setUpBattle = async () => {
      const valid = await checkRoomValidity(roomId as string)
      if (valid) {
        await fetchPlayer()
      }
    }
    setUpBattle()
  }, [])


  const { sendJsonMessage, lastJsonMessage, readyState } = useWebSocket(
    player ? socketUrl : null,
    {
      share: true,
    }
  )
  useEffect(() => {
    if (readyState === ReadyState.OPEN && player) {
      sendJsonMessage({
        type: "connect",
        id: roomId,
        payload: {
          player: player.name,
          playerData: player,
        },
      });
    }
  }, [readyState, player, sendJsonMessage]);

  useEffect(() => {
    let message = lastJsonMessage as ServerMessage
    if (!message) return
    switch (message.type) {
      case "assignment":
        if (message.payload.message === "player1" || message.payload.message === "player2") {
          setAssignment(message.payload.message)
        }
        break;
      case "reconnection":
      case "connection":
      case "disconnect":
        setDescription(message.payload.message)
        break;
      case "start":
        setDescription("battle started")
        setBattleState(message.payload.state)
        break;
      case "stateUpdate":
        setDescription(message.payload.message!)
        setBattleState(message.payload.state)
        break;
      case "error":
        alert(`${message.payload.errorType}: ${message.payload.errorMessage}`)
        break;
      case "end":
        setDescription(message.payload.message)
        setBattleState(message.payload.state)
        setTimeout(() => {
          alert(`${message.payload.winner} has beaten ${message.payload.loser}`)
          navigate("/")
        }, 2000)
        break;
      case "reconnect":
        if (message.payload.assignment === "player1" || message.payload.assignment === "player2") {
          setAssignment(message.payload.assignment)
        }
        setBattleState(message.payload.state)
    }

  }, [lastJsonMessage])


  const [itemsMenu, setShowItemsMenu] = useState(false)
  const [SkillsMenu, setShowSkillsMenu] = useState(false)
  const [fightersMenu, setShowFightersMenu] = useState(false)
  const [description, setDescription] = useState('')
  const { player1, player2, turn } = battleState
  const playerTurn = turn % 2 === 0 ? player2 : player1

  if (loading) {
    return <Loading></Loading>
  }

  
  if (!validRoom) {
    return (
      <div className="not-found">
        Game session not found
      </div>
    )
  }

  

  return (
    <>
      <h1 className="test">{playerTurn.selectedFighter.name}, Turn: {`${playerTurn.name}'s turn`}</h1>
      <div className="description-container">
        <span className="description">{description}</span>
      </div>
      <div className="stats-wrapper">
        <GameContext.Provider value={{ player: player1, turn: turn, playerTurn: playerTurn, roomId: roomId }}>
          <div className="p1-container">
            {(assignment === "player1" || !assignment)
              ?
              <div className="select-menu-container">
                {fightersMenu && (<FighterSelect fighters={player1.fighters} showFightersMenu={setShowFightersMenu} />)}
                {SkillsMenu && (<SkillMenu fighter={player1.selectedFighter} sp={player1.sp} hp={player1.hp} showSkillsMenu={setShowSkillsMenu} />)}
                {itemsMenu && (<ItemMenu playerObj={player1} showItemsMenu={setShowItemsMenu} />)}
              </div>
              :
              <></>
            }
            <div className="stat-container">
              <span className="player-name">{player1.name}</span>
              {(assignment === "player1" || !assignment)
                ? <><BattleStats health={player1.hp} sp={player1.sp} showFightersMenu={setShowFightersMenu} showSkillsMenu={setShowSkillsMenu} showItemsMenu={setShowItemsMenu} isP2={false} />
                </>
                : <OpponentStats health={player1.hp} sp={player1.sp} isP2={false} />
              }
            </div>
          </div>
        </GameContext.Provider>
        <GameContext.Provider value={{ player: player2, turn: turn, playerTurn: playerTurn, roomId: roomId }}>
          <div className="p2-container">
            {assignment === "player2"
              ?
              <div className="select-menu-container">
                {fightersMenu && (<FighterSelect fighters={player2.fighters} showFightersMenu={setShowFightersMenu} />)}
                {SkillsMenu && (<SkillMenu fighter={player2.selectedFighter} sp={player2.sp} hp={player2.hp} showSkillsMenu={setShowSkillsMenu} />)}
                {itemsMenu && (<ItemMenu playerObj={player2} showItemsMenu={setShowItemsMenu} />)}
              </div>
              :
              <></>
            }
            <div className="p2stat-container">
              <span className="player-name">{player2.name}</span>
              {assignment === "player2"
                ? <><BattleStats health={player2.hp} sp={player2.sp} showFightersMenu={setShowFightersMenu} showSkillsMenu={setShowSkillsMenu} showItemsMenu={setShowItemsMenu} isP2={true} />
                </>
                : <OpponentStats health={player2.hp} sp={player2.sp} isP2={true} />
              }
            </div>
          </div>
        </GameContext.Provider>
      </div>
    </>
  )
}
