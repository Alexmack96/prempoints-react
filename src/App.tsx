import { Outlet } from 'react-router-dom'
import './App.css'
import NavBar from './features/shared/NavBar'
import { UserModal } from './features/user/components/UserModal'

function App() {

  return (
    <>
        <NavBar />
      <Outlet />
      <div className='flex items-center justify-center min-h-screen bg-pink-50'>
        <UserModal/>
      </div>
    </>
  )
}

export default App
