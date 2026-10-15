import { useCallback, useEffect, useRef, useState } from 'react'
import type { AuthResponse } from '../api/auth'
import { getDocuments } from '../api/documents'
import type { DocumentDto } from '../api/documents'
import { isSessionExpiredError, SESSION_EXPIRED_MESSAGE } from '../api/errors'
import { Brand } from './Brand'
import { DocumentLibrary } from './DocumentLibrary'
import { SearchWorkspace } from './SearchWorkspace'
import { ChatWorkspace } from './ChatWorkspace'
export type RunRequest = (work: () => Promise<void>) => Promise<void>
type View = 'library' | 'search' | 'chat'
const views = [{ id: 'library', title: 'Document library', icon: '▤' }, { id: 'search', title: 'Semantic search', icon: '⌕' }, { id: 'chat', title: 'Ask InsightVault', icon: '✦' }] as const
export function WorkspaceShell({ auth, onLogout }: { auth: AuthResponse; onLogout: (message?: string) => void }) {
  const [view, setView] = useState<View>('library')
  const [documents, setDocuments] = useState<DocumentDto[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [revision, setRevision] = useState(0)
  const active = useRef(true)
  useEffect(() => { active.current = true; return () => { active.current = false } }, [])
  const run: RunRequest = useCallback(async (work) => {
    if (!active.current) return
    setError('')
    try { await work() } catch (err) {
      if (!active.current) return
      if (isSessionExpiredError(err)) onLogout(SESSION_EXPIRED_MESSAGE)
      else setError(err instanceof Error ? err.message : 'Something went wrong. Please try again.')
    }
  }, [onLogout])
  const refresh = useCallback(async () => {
    setLoading(true)
    await run(async () => { setDocuments(await getDocuments(auth.token)) })
    setLoading(false)
  }, [auth.token, run])
  useEffect(() => {
    let cancelled = false
    void getDocuments(auth.token).then(value => {
      if (!cancelled) setDocuments(value)
    }).catch(err => {
      if (cancelled) return
      if (isSessionExpiredError(err)) onLogout(SESSION_EXPIRED_MESSAGE)
      else setError(err instanceof Error ? err.message : 'Could not load documents.')
    }).finally(() => { if (!cancelled) setLoading(false) })
    return () => { cancelled = true }
  }, [auth.token, onLogout])
  const ready = documents.filter(d => d.status === 'Processed').length
  function navigate(next: View) { setView(next); setError('') }
  return <div className="workspace"><a className="skip-link" href="#workspace-main">Skip to content</a><aside className="sidebar"><Brand /><p className="nav-label">WORKSPACE</p><nav aria-label="Workspace">{views.map(item => <button key={item.id} className={view === item.id ? 'nav-item active' : 'nav-item'} aria-current={view === item.id ? 'page' : undefined} onClick={() => navigate(item.id)}><span className="nav-icon" aria-hidden="true">{item.icon}</span><span>{item.title}</span>{item.id === 'library' && <small>{documents.length}</small>}</button>)}</nav><div className="sidebar-bottom"><div className="sidebar-note"><span className="status-dot" /><span>A little more clarity.<br /><strong>One question at a time.</strong></span></div><div className="account"><span className="avatar">{auth.email.slice(0, 2).toUpperCase()}</span><span className="account-email" title={auth.email}>{auth.email}<small>Personal workspace</small></span></div><button className="logout" onClick={() => onLogout()}>↗ Sign out</button></div></aside>
    <div className="workspace-body"><header className="topbar"><span className="breadcrumb">Workspace <span>/</span> <strong>{views.find(v => v.id === view)?.title}</strong></span><span className="private-label"><span className="status-dot" />PRIVATE LIBRARY</span></header><main id="workspace-main" tabIndex={-1}><div className="page-intro"><p className="eyebrow">YOUR KNOWLEDGE, CONNECTED</p><h1>{view === 'library' ? 'A home for your knowledge.' : view === 'search' ? 'Find the signal.' : 'Go from information to insight.'}</h1><p>{view === 'library' ? 'Everything you upload. Ready for your next good question.' : view === 'search' ? 'Find relevant passages by meaning, even when the words are different.' : 'Ask a question. Get a grounded answer, with sources you can follow.'}</p></div>
      {error && <div className="notice error" role="alert"><span>{error}</span><button className="text-button" onClick={() => setError('')} aria-label="Dismiss error">Dismiss</button></div>}
      <div hidden={view !== 'library'}><DocumentLibrary token={auth.token} documents={documents} setDocuments={setDocuments} loading={loading} refresh={refresh} run={run} onAsk={() => navigate('chat')} onDeleted={() => setRevision(v => v + 1)} /></div>
      <div hidden={view !== 'search'}><SearchWorkspace key={'search-' + revision} token={auth.token} run={run} /></div>
      <div hidden={view !== 'chat'}><ChatWorkspace key={'chat-' + revision} token={auth.token} run={run} ready={ready} /></div>
      <footer className="workspace-footer"><span>INSIGHTVAULT</span><span>Built for questions worth asking.</span></footer>
    </main></div>
  </div>
}
