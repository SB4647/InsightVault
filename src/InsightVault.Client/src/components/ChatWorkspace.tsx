import { useState } from 'react'
import type { FormEvent } from 'react'
import { askQuestion } from '../api/chat'
import type { ChatResponseDto } from '../api/chat'
import type { RunRequest } from './WorkspaceShell'
import { SourceCitation } from './SourceCitation'
import mark from '../assets/insightvault-mark.png'
export function ChatWorkspace({ token, run, ready }: { token: string; run: RunRequest; ready: number }) {
  const [question, setQuestion] = useState('')
  const [submitted, setSubmitted] = useState('')
  const [response, setResponse] = useState<ChatResponseDto | null>(null)
  const [busy, setBusy] = useState(false)
  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (!question.trim()) return
    const value = question.trim()
    setBusy(true); setResponse(null); setSubmitted(value)
    await run(async () => setResponse(await askQuestion(value, token)))
    setBusy(false)
  }
  return <><div className="chat-grid"><section className="panel chat-main"><header className="chat-heading"><img src={mark} alt="" /><div><h2>Ask InsightVault</h2><p><span className="status-dot" />{ready} ready {ready === 1 ? 'document' : 'documents'} in your library</p></div><span className="small-tag">SOURCE-GROUNDED</span></header>
    {busy ? <div className="empty-state" role="status"><span className="loader" />Reading the relevant passages…</div> : response ? <div className="answer-content"><p className="question-bubble">{submitted}</p><p className="eyebrow">THE INSIGHT</p><div className="answer-text">{response.answer}</div><p className="answer-disclaimer">AI answers can be incomplete. Check the cited passages before relying on them.</p></div> : <div className="empty-state chat-empty"><span className="empty-icon">✦</span><h2>A good question changes everything.</h2><p>Bring your documents into focus with an answer grounded in their content.</p><div className="suggestions">{['What are the key takeaways?', 'Which risks should I pay attention to?'].map(q => <button key={q} onClick={() => setQuestion(q)}>{q}<span aria-hidden="true">↗</span></button>)}</div></div>}
    <form className="chat-composer" onSubmit={submit}><label className="sr-only" htmlFor="chat-question">Your question</label><textarea id="chat-question" placeholder="Ask a question about your documents…" value={question} onChange={e => setQuestion(e.target.value)} rows={3} disabled={busy} required /><div><span>Each question is answered independently.</span><button className="primary" disabled={busy || !question.trim()}>{busy ? 'Thinking…' : 'Ask question'} <span aria-hidden="true">↑</span></button></div></form>
    </section><aside className="panel sources-panel"><header><p className="eyebrow">FOLLOW THE EVIDENCE</p><h2>Sources <span className="count">{response?.sources.length ?? 0}</span></h2><p>The context behind the answer.</p></header>{response?.sources.length ? <div className="source-list">{response.sources.map(s => <SourceCitation key={s.chunkId} source={s} />)}</div> : <div className="source-placeholder"><span>▤</span><h3>{response ? 'No sources returned' : 'Your sources will appear here.'}</h3><p>{response ? 'This answer has no linked evidence. Try another question or check your library.' : 'Document names, page references, and the passages used to answer your question.'}</p></div>}</aside></div></>
}
