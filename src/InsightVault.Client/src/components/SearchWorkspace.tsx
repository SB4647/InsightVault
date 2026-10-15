import { useState } from 'react'
import type { FormEvent } from 'react'
import { searchDocuments } from '../api/search'
import type { SearchResultDto } from '../api/search'
import type { RunRequest } from './WorkspaceShell'
import { SourceCitation } from './SourceCitation'
export function SearchWorkspace({ token, run }: { token: string; run: RunRequest }) {
  const [query, setQuery] = useState('')
  const [submitted, setSubmitted] = useState<string | null>(null)
  const [results, setResults] = useState<SearchResultDto[]>([])
  const [busy, setBusy] = useState(false)
  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (!query.trim()) return
    setBusy(true); setSubmitted(null); setResults([])
    await run(async () => { const value = query.trim(); setResults(await searchDocuments(value, token)); setSubmitted(value) })
    setBusy(false)
  }
  return <><form className="panel query-form" onSubmit={submit}><label htmlFor="search-query">SEARCH YOUR LIBRARY</label><div><span className="query-symbol" aria-hidden="true">⌕</span><input id="search-query" type="search" placeholder="What would you like to find?" value={query} onChange={e => setQuery(e.target.value)} disabled={busy} required /><button className="primary" disabled={busy || !query.trim()}>{busy ? 'Searching…' : 'Find passages'} <span aria-hidden="true">→</span></button></div><p>Describe a concept, ask a question, or search for a specific topic.</p></form>
    {busy ? <div className="empty-state" role="status"><span className="loader" />Finding relevant passages…</div> : submitted !== null ? <section aria-label="Search results"><div className="results-heading"><h2>{results.length} {results.length === 1 ? 'passage' : 'passages'} found</h2><p>Results for “{submitted}”</p></div>{results.length ? <div className="search-results">{results.map(r => <div key={r.chunkId} className="search-result"><SourceCitation source={r} /><span className="retrieval-score">Retrieval score {r.score.toFixed(3)} · not an accuracy estimate</span></div>)}</div> : <div className="panel empty-state"><h3>No relevant passages found.</h3><p>Try a broader question or check that your documents are ready.</p></div>}</section> : <div className="panel empty-state search-empty"><span className="empty-icon">⌕</span><h2>Start with a thought.</h2><p>Search goes beyond exact keywords to surface the context you need.</p><div className="feature-pills"><span>Search by meaning</span><span>Traceable passages</span><span>Your sources</span></div></div>}
  </>
}
