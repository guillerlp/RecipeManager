import { useState } from 'react'
import reactLogo from './assets/react.svg'
import viteLogo from '/vite.svg'
import './App.css'
import RecipeList from './pages/RecipeList'

function App() {
  const [count, setCount] = useState(0)

  return (
    <>
      <div>
        <RecipeList></RecipeList>
      </div>
    </>
  )
}

export default App
