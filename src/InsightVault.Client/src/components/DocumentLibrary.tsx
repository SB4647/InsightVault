import { useState } from 'react'
import type { Dispatch, FormEvent, SetStateAction } from 'react'
import { deleteDocument, retryDocumentProcessing, shareDocument, uploadDocument } from '../api/documents'
import type { DocumentDto } from '../api/documents'
import type { RunRequest } from './WorkspaceShell'
interface Props {
  token: string; documents: DocumentDto[]; setDocuments: Dispatch<SetStateAction<DocumentDto[]>>
  loading: boolean; refresh: () => Promise<void>; run: RunRequest; onAsk: () => void; onDeleted: () => void
}
function bytes(value: number) { return value < 1024 * 1024 ? (value / 1024).toFixed(1) + ' KB' : (value / 1024 / 1024).toFixed(1) + ' MB' }
export function DocumentLibrary({ token, documents, setDocuments, loading, refresh, run, onAsk, onDeleted }: Props) {
  const [file, setFile] = useState<File | null>(null)
  const [uploading, setUploading] = useState(false)
  const [notice, setNotice] = useState('')
  const [filter, setFilter] = useState('')
  const [busyId, setBusyId] = useState<string | null>(null)
  const [sharing, setSharing] = useState<string | null>(null)
  async function upload(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (!file) return
    const form = event.currentTarget
    setUploading(true); setNotice('')
    await run(async () => {
      if (!file.name.toLowerCase().endsWith('.pdf') || file.size === 0 || file.size > 25 * 1024 * 1024) throw new Error('Choose a non-empty PDF up to 25 MB.')
      const result = await uploadDocument(file, token)
      setDocuments(current => [result, ...current])
      setFile(null); form.reset(); setNotice('Document uploaded. Refresh to check processing progress.')
    })
    setUploading(false)
  }
  async function action(id: string, work: () => Promise<void>) {
    setBusyId(id); setNotice(''); await run(work); setBusyId(null)
  }
  async function share(event: FormEvent<HTMLFormElement>, doc: DocumentDto) {
    event.preventDefault()
    const email = String(new FormData(event.currentTarget).get('shareEmail')).trim()
    await action(doc.id, async () => {
      await shareDocument(doc.id, email, token); setSharing(null); setNotice('Shared ' + doc.originalFileName + ' with ' + email + '.')
    })
  }
  const ready = documents.filter(d => d.status === 'Processed').length
  const processing = documents.filter(d => ['Uploaded', 'Processing'].includes(d.status)).length
  const failed = documents.filter(d => d.status === 'Failed').length
  const visible = documents.filter(d => d.originalFileName.toLowerCase().includes(filter.toLowerCase()))
  return <>
    <div className="library-summary"><div className="stat"><span>DOCUMENTS</span><strong>{loading ? '—' : documents.length.toString().padStart(2, '0')}</strong><small>In your library</small></div><div className="stat"><span>READY TO EXPLORE</span><strong>{loading ? '—' : ready.toString().padStart(2, '0')}</strong><small className="positive">Processed & searchable</small></div><div className="stat"><span>IN PROGRESS</span><strong>{loading ? '—' : processing.toString().padStart(2, '0')}</strong><small>{failed ? failed + ' need attention' : 'Upload to unlock more context'}</small></div><button className="ask-card" onClick={onAsk}><span className="spark">✦</span><strong>Put your library to work.</strong><span>Ask a question across your documents <b>↗</b></span></button></div>
    <section className="panel upload-panel" aria-labelledby="upload-title"><div><span className="upload-symbol" aria-hidden="true">↑</span><div><h2 id="upload-title">Add to your knowledge.</h2><p>PDF documents · Up to 25 MB each</p></div></div><form onSubmit={upload}><label className="file-picker">Choose a PDF<input type="file" accept=".pdf,application/pdf" disabled={uploading} onChange={e => setFile(e.target.files?.[0] ?? null)} /></label><span className="selected-file">{file?.name ?? 'No file selected'}</span><button className="primary" disabled={!file || uploading}>{uploading ? 'Uploading…' : 'Upload document'} <span aria-hidden="true">↑</span></button></form></section>
    {notice && <p className="notice" role="status">{notice}</p>}
    <section className="panel document-panel" aria-labelledby="documents-title"><header className="section-heading"><div><h2 id="documents-title">Your documents <span className="count">{documents.length}</span></h2><p>Source material for your next insight.</p></div><div className="toolbar"><label className="sr-only" htmlFor="filter-docs">Filter documents</label><input id="filter-docs" type="search" placeholder="Filter documents…" value={filter} onChange={e => setFilter(e.target.value)} /><button disabled={loading || uploading || !!busyId} onClick={() => void refresh()}>{loading ? 'Loading…' : '↻ Refresh'}</button></div></header>
      {loading ? <div className="empty-state" role="status"><span className="loader" />Loading your library…</div> : visible.length === 0 ? <div className="empty-state"><span className="empty-icon">▤</span><h3>{documents.length ? 'No matching documents' : 'Your next insight starts here.'}</h3><p>{documents.length ? 'Try a different filename.' : 'Upload your first PDF to start searching and asking questions.'}</p></div> :
      <div className="document-list">{visible.map(doc => <article className="document-row" key={doc.id}><div className="document-info"><span className="pdf-icon">PDF</span><div><h3>{doc.originalFileName}</h3><p>{bytes(doc.sizeInBytes)} <span>·</span> {new Date(doc.uploadedAtUtc).toLocaleDateString()} <span>·</span> {doc.isOwner ? 'Owner' : 'Shared with you'}</p></div></div><div className="document-status"><span className={'status status-' + doc.status.toLowerCase()}>{doc.status === 'Processed' ? 'Ready' : doc.status}</span><small>{doc.chunkCount} chunks</small></div><div className="document-actions">{doc.isOwner ? <>{doc.status === 'Failed' && <button disabled={!!busyId} onClick={() => void action(doc.id, async () => { const result = await retryDocumentProcessing(doc.id, token); setDocuments(current => current.map(d => d.id === doc.id ? result : d)); setNotice('Processing retry requested.') })}>Retry processing</button>}<button disabled={!!busyId} onClick={() => setSharing(sharing === doc.id ? null : doc.id)} aria-expanded={sharing === doc.id}>Share</button><button className="danger-quiet" disabled={!!busyId} aria-label={'Delete ' + doc.originalFileName} onClick={() => { if (window.confirm('Delete ' + doc.originalFileName + '? This removes the file and processed data.')) void action(doc.id, async () => { await deleteDocument(doc.id, token); setDocuments(current => current.filter(d => d.id !== doc.id)); onDeleted(); setNotice('Document deleted.') }) }}>Delete</button></> : <span className="muted">View access</span>}{busyId === doc.id && <span role="status">Working…</span>}</div>
      {sharing === doc.id && <form className="share-form" onSubmit={e => void share(e, doc)}><label>Share with an existing account<input name="shareEmail" type="email" placeholder="colleague@example.com" required disabled={!!busyId} /></label><button className="primary" disabled={!!busyId}>Confirm share</button><button type="button" onClick={() => setSharing(null)} disabled={!!busyId}>Cancel</button></form>}</article>)}</div>}
    </section><p className="library-hint">Ready documents are available to search and ask about. Refresh to see the latest processing status.</p>
  </>
}
