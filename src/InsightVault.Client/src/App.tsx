import { useCallback, useState } from 'react'
import type { AuthResponse } from './api/auth'
import { AuthPanel } from './components/AuthPanel'
import { WorkspaceShell } from './components/WorkspaceShell'
import './App.css'
const AUTH_STORAGE_KEY = 'insightvault.auth'
function loadStoredAuth(): AuthResponse | null {
  try {
    const value = JSON.parse(localStorage.getItem(AUTH_STORAGE_KEY) ?? 'null')
    if (value && typeof value.token === 'string' && value.token &&
      typeof value.email === 'string' && typeof value.userId === 'string') return value
  } catch { /* Inaccessible or corrupt storage is treated as signed out. */ }
  return null
}
export default function App() {
  const [auth, setAuth] = useState(loadStoredAuth)
  const [notice, setNotice] = useState('')
  const logout = useCallback((message = '') => {
    try { localStorage.removeItem(AUTH_STORAGE_KEY) } catch { /* Storage may be disabled. */ }
    setAuth(null); setNotice(message)
  }, [])
  function authenticated(value: AuthResponse) {
    try { localStorage.setItem(AUTH_STORAGE_KEY, JSON.stringify(value)) } catch { /* Keep session in memory. */ }
    setNotice(''); setAuth(value)
  }
  // Remount all private workspace state when the account/session changes.
  return auth ? <WorkspaceShell key={auth.token} auth={auth} onLogout={logout} /> : <AuthPanel notice={notice} onAuthenticated={authenticated} />
}
