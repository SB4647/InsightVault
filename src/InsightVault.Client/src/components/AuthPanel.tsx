import { useState } from 'react'
import type { FormEvent } from 'react'
import { login, register } from '../api/auth'
import type { AuthResponse } from '../api/auth'
import sage from '../assets/insightvault-sage.png'
import { Brand } from './Brand'
export function AuthPanel({ notice, onAuthenticated }: { notice: string; onAuthenticated: (auth: AuthResponse) => void }) {
  const [mode, setMode] = useState<'login' | 'register'>('login')
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')
  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    const data = new FormData(event.currentTarget)
    setBusy(true); setError('')
    try { onAuthenticated(await (mode === 'login' ? login : register)(String(data.get('email')).trim(), String(data.get('password')))) }
    catch (err) { setError(err instanceof Error ? err.message : 'Unable to sign in. Please try again.') }
    finally { setBusy(false) }
  }
  return <main className="auth-page">
    <section className="auth-story"><Brand /><div className="auth-art"><img src={sage} alt="InsightVault's knowledge keeper with flowing data streams" /></div><div className="auth-copy"><p className="eyebrow">KNOWLEDGE, WITH CONTEXT</p><h1>Your documents.<br />A clearer perspective.</h1><p>Find the right passage. Ask the next question.<br />Follow every answer back to its source.</p><div className="feature-pills"><span>Private library</span><span>Semantic search</span><span>Cited answers</span></div></div><span className="auth-footnote">INSIGHTVAULT / YOUR KNOWLEDGE WORKSPACE</span></section>
    <section className="auth-entry"><div className="auth-form-card"><p className="eyebrow">YOUR WORKSPACE AWAITS</p><h2>{mode === 'login' ? 'Welcome back.' : 'Make room for insight.'}</h2><p className="muted">{mode === 'login' ? 'Sign in to pick up where your curiosity left off.' : 'Create an account for your own document library.'}</p>
      {notice && <p className="notice" role="status">{notice}</p>}{error && <p className="notice error" role="alert">{error}</p>}
      <form onSubmit={submit} className="auth-form"><label>Email address<input name="email" type="email" placeholder="you@example.com" autoComplete="email" required disabled={busy} /></label><label>Password<input name="password" type="password" placeholder="Enter your password" autoComplete={mode === 'login' ? 'current-password' : 'new-password'} required disabled={busy} /></label><button className="primary" disabled={busy}>{busy ? 'Connecting…' : mode === 'login' ? 'Sign in to workspace' : 'Create account'} <span aria-hidden="true">→</span></button></form>
      <p className="auth-switch">{mode === 'login' ? 'New to InsightVault?' : 'Already have an account?'} <button className="text-button" disabled={busy} onClick={() => { setMode(mode === 'login' ? 'register' : 'login'); setError('') }}>{mode === 'login' ? 'Create an account' : 'Sign in'}</button></p><p className="auth-privacy">Your documents stay scoped to your account and the people you share them with.</p>
    </div></section>
  </main>
}
