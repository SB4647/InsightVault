import { useEffect, useState } from 'react'
import type { FormEvent } from 'react'
import './App.css'
import {
  deleteDocument,
  getDocuments,
  processDocument,
  shareDocument,
  uploadDocument,
} from './api/documents'
import type { DocumentDto } from './api/documents'
import { searchDocuments } from './api/search'
import type { SearchResultDto } from './api/search'
import { askQuestion } from './api/chat'
import type { ChatResponseDto } from './api/chat'
import { login, register } from './api/auth'
import type { AuthResponse } from './api/auth'

const AUTH_STORAGE_KEY = 'insightvault.auth'

function App() {
  const [documents, setDocuments] = useState<DocumentDto[]>([])
  const [auth, setAuth] = useState<AuthResponse | null>(() => loadStoredAuth())
  const [authMode, setAuthMode] = useState<'login' | 'register'>('login')
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [selectedFile, setSelectedFile] = useState<File | null>(null)
  const [searchQuery, setSearchQuery] = useState('')
  const [searchResults, setSearchResults] = useState<SearchResultDto[]>([])
  const [chatQuestion, setChatQuestion] = useState('')
  const [chatResponse, setChatResponse] = useState<ChatResponseDto | null>(null)
  const [shareEmails, setShareEmails] = useState<Record<string, string>>({})
  const [isLoading, setIsLoading] = useState(() => Boolean(auth))
  const [isAuthenticating, setIsAuthenticating] = useState(false)
  const [isUploading, setIsUploading] = useState(false)
  const [isSearching, setIsSearching] = useState(false)
  const [isAsking, setIsAsking] = useState(false)
  const [processingDocumentId, setProcessingDocumentId] = useState<string | null>(null)
  const [sharingDocumentId, setSharingDocumentId] = useState<string | null>(null)
  const [deletingDocumentId, setDeletingDocumentId] = useState<string | null>(null)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    if (!auth) {
      return
    }

    const currentAuth = auth
    let isActive = true

    async function loadInitialDocuments() {
      setIsLoading(true)
      setError(null)

      try {
        const loadedDocuments = await getDocuments(currentAuth.token)
        if (isActive) {
          setDocuments(loadedDocuments)
        }
      } catch (err) {
        if (isActive) {
          setError(err instanceof Error ? err.message : 'Could not load documents.')
        }
      } finally {
        if (isActive) {
          setIsLoading(false)
        }
      }
    }

    void loadInitialDocuments()

    return () => {
      isActive = false
    }
  }, [auth])

  async function loadDocuments(token = auth?.token) {
    if (!token) {
      return
    }

    setIsLoading(true)
    setError(null)

    try {
      setDocuments(await getDocuments(token))
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Could not load documents.')
    } finally {
      setIsLoading(false)
    }
  }

  async function handleAuth(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()

    if (!email.trim() || !password) {
      setError('Email and password are required.')
      return
    }

    setIsAuthenticating(true)
    setError(null)

    try {
      const result =
        authMode === 'login'
          ? await login(email.trim(), password)
          : await register(email.trim(), password)

      localStorage.setItem(AUTH_STORAGE_KEY, JSON.stringify(result))
      setAuth(result)
      setPassword('')
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Could not authenticate.')
    } finally {
      setIsAuthenticating(false)
    }
  }

  function handleLogout() {
    localStorage.removeItem(AUTH_STORAGE_KEY)
    setAuth(null)
    setDocuments([])
    setSearchResults([])
    setChatResponse(null)
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    const form = event.currentTarget

    if (!selectedFile) {
      setError('Choose a document before uploading.')
      return
    }

    setIsUploading(true)
    setError(null)

    try {
      const uploadedDocument = await uploadDocument(selectedFile, auth?.token ?? '')
      setDocuments((current) => [uploadedDocument, ...current])
      setSelectedFile(null)
      form.reset()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Could not upload document.')
    } finally {
      setIsUploading(false)
    }
  }

  async function handleProcess(documentId: string) {
    setProcessingDocumentId(documentId)
    setError(null)

    try {
      const result = await processDocument(documentId, auth?.token ?? '')
      setDocuments((current) =>
        current.map((document) =>
          document.id === documentId
            ? { ...document, status: result.status, chunkCount: result.chunkCount }
            : document,
        ),
      )
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Could not process document.')
    } finally {
      setProcessingDocumentId(null)
    }
  }

  async function handleShare(event: FormEvent<HTMLFormElement>, documentId: string) {
    event.preventDefault()

    const email = shareEmails[documentId]?.trim()
    if (!email) {
      setError('Enter an email address to share with.')
      return
    }

    setSharingDocumentId(documentId)
    setError(null)

    try {
      await shareDocument(documentId, email, auth?.token ?? '')
      setShareEmails((current) => ({ ...current, [documentId]: '' }))
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Could not share document.')
    } finally {
      setSharingDocumentId(null)
    }
  }

  async function handleDelete(documentId: string) {
    const confirmed = window.confirm(
      'Delete this document? This removes the uploaded file and any processed data.',
    )

    if (!confirmed) {
      return
    }

    setDeletingDocumentId(documentId)
    setError(null)

    try {
      await deleteDocument(documentId, auth?.token ?? '')
      setDocuments((current) => current.filter((document) => document.id !== documentId))
      setSearchResults((current) => current.filter((result) => result.documentId !== documentId))
      setChatResponse(null)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Could not delete document.')
    } finally {
      setDeletingDocumentId(null)
    }
  }

  async function handleSearch(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()

    if (!searchQuery.trim()) {
      setError('Enter a search query.')
      return
    }

    setIsSearching(true)
    setError(null)

    try {
      setSearchResults(await searchDocuments(searchQuery.trim(), auth?.token ?? ''))
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Could not search documents.')
    } finally {
      setIsSearching(false)
    }
  }

  async function handleAsk(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()

    if (!chatQuestion.trim()) {
      setError('Enter a chat question.')
      return
    }

    setIsAsking(true)
    setError(null)

    try {
      setChatResponse(await askQuestion(chatQuestion.trim(), auth?.token ?? ''))
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Could not answer question.')
    } finally {
      setIsAsking(false)
    }
  }

  return (
    <main className="app-shell">
      <section className="page-header">
        <p className="eyebrow">InsightVault</p>
        <h1>Document Library</h1>
        <p>
          Upload source documents, process them, search semantically, and ask grounded
          questions across your private library.
        </p>
      </section>

      <section className="auth-panel" aria-labelledby="auth-title">
        {auth ? (
          <>
            <div>
              <h2 id="auth-title">Signed in</h2>
              <p>{auth.email}</p>
            </div>
            <button type="button" onClick={handleLogout}>
              Log out
            </button>
          </>
        ) : (
          <>
            <div>
              <h2 id="auth-title">{authMode === 'login' ? 'Log in' : 'Create account'}</h2>
              <p>Sign in to access your document library.</p>
            </div>

            <form onSubmit={handleAuth} className="auth-form">
              <input
                type="email"
                value={email}
                onChange={(event) => setEmail(event.target.value)}
                placeholder="Email"
                autoComplete="email"
              />
              <input
                type="password"
                value={password}
                onChange={(event) => setPassword(event.target.value)}
                placeholder="Password"
                autoComplete={authMode === 'login' ? 'current-password' : 'new-password'}
              />
              <button type="submit" disabled={isAuthenticating}>
                {isAuthenticating ? 'Working...' : authMode === 'login' ? 'Log in' : 'Register'}
              </button>
              <button
                type="button"
                className="secondary-button"
                onClick={() => setAuthMode(authMode === 'login' ? 'register' : 'login')}
              >
                {authMode === 'login' ? 'Need an account?' : 'Already registered?'}
              </button>
            </form>
          </>
        )}
      </section>

      {error && <p className="message error">{error}</p>}

      {auth && (
        <>
          <section className="upload-panel" aria-labelledby="upload-title">
        <div>
          <h2 id="upload-title">Upload document</h2>
          <p>Phase 1 stores the file and metadata only.</p>
        </div>

        <form onSubmit={handleSubmit} className="upload-form">
          <input
            type="file"
            accept=".pdf,application/pdf"
            onChange={(event) => setSelectedFile(event.target.files?.[0] ?? null)}
          />
          <button type="submit" disabled={isUploading}>
            {isUploading ? 'Uploading...' : 'Upload'}
          </button>
        </form>
          </section>

          <section className="search-panel" aria-labelledby="search-title">
        <div>
          <h2 id="search-title">Semantic search</h2>
          <p>Search across processed document chunks.</p>
        </div>

        <form onSubmit={handleSearch} className="search-form">
          <input
            type="search"
            value={searchQuery}
            onChange={(event) => setSearchQuery(event.target.value)}
            placeholder="Ask about your processed documents"
          />
          <button type="submit" disabled={isSearching}>
            {isSearching ? 'Searching...' : 'Search'}
          </button>
        </form>

        {searchResults.length > 0 && (
          <div className="search-results">
            {searchResults.map((result) => (
              <article className="search-result" key={result.chunkId}>
                <div>
                  <h3>{result.documentName}</h3>
                  <p>{result.text}</p>
                </div>
                <span>{Math.round(result.score * 100)}%</span>
              </article>
            ))}
          </div>
        )}
          </section>

          <section className="chat-panel" aria-labelledby="chat-title">
        <div>
          <h2 id="chat-title">RAG chat</h2>
          <p>Ask a question and get an answer grounded in processed document chunks.</p>
        </div>

        <form onSubmit={handleAsk} className="chat-form">
          <textarea
            value={chatQuestion}
            onChange={(event) => setChatQuestion(event.target.value)}
            placeholder="What should I know from these documents?"
            rows={3}
          />
          <button type="submit" disabled={isAsking}>
            {isAsking ? 'Asking...' : 'Ask'}
          </button>
        </form>

        {chatResponse && (
          <div className="chat-answer">
            <h3>Answer</h3>
            <p>{chatResponse.answer}</p>

            {chatResponse.sources.length > 0 && (
              <div className="source-list">
                <h3>Sources</h3>
                {chatResponse.sources.map((source, index) => (
                  <article className="source-item" key={source.chunkId}>
                    <div>
                      <strong>
                        [{index + 1}] {source.documentName}
                      </strong>
                      <span>Chunk {source.chunkIndex}</span>
                    </div>
                    <p>{source.text}</p>
                  </article>
                ))}
              </div>
            )}
          </div>
        )}
          </section>

          <section className="documents-section" aria-labelledby="documents-title">
        <div className="section-heading">
          <h2 id="documents-title">Uploaded documents</h2>
          <button type="button" onClick={() => loadDocuments()} disabled={isLoading}>
            Refresh
          </button>
        </div>

        {isLoading ? (
          <p className="message">Loading documents...</p>
        ) : documents.length === 0 ? (
          <p className="message">No documents uploaded yet.</p>
        ) : (
          <div className="document-list">
            {documents.map((document) => (
              <article className="document-row" key={document.id}>
                <div>
                  <h3>{document.originalFileName}</h3>
                  <p>{document.contentType}</p>
                </div>
                <dl>
                  <div>
                    <dt>Size</dt>
                    <dd>{formatBytes(document.sizeInBytes)}</dd>
                  </div>
                  <div>
                    <dt>Status</dt>
                    <dd>{document.status}</dd>
                  </div>
                  <div>
                    <dt>Access</dt>
                    <dd>{document.accessLevel}</dd>
                  </div>
                  <div>
                    <dt>Chunks</dt>
                    <dd>{document.chunkCount}</dd>
                  </div>
                  <div>
                    <dt>Uploaded</dt>
                    <dd>{new Date(document.uploadedAtUtc).toLocaleString()}</dd>
                  </div>
                </dl>
                {document.isOwner ? (
                  <div className="document-actions">
                    <button
                      type="button"
                      onClick={() => handleProcess(document.id)}
                      disabled={processingDocumentId === document.id || document.status === 'Processed'}
                    >
                      {processingDocumentId === document.id ? 'Processing...' : 'Process'}
                    </button>
                    <form className="share-form" onSubmit={(event) => handleShare(event, document.id)}>
                      <input
                        type="email"
                        value={shareEmails[document.id] ?? ''}
                        onChange={(event) =>
                          setShareEmails((current) => ({
                            ...current,
                            [document.id]: event.target.value,
                          }))
                        }
                        placeholder="Share by email"
                      />
                      <button type="submit" disabled={sharingDocumentId === document.id}>
                        {sharingDocumentId === document.id ? 'Sharing...' : 'Share'}
                      </button>
                    </form>
                    <button
                      type="button"
                      className="danger-button"
                      onClick={() => handleDelete(document.id)}
                      disabled={
                        deletingDocumentId === document.id ||
                        processingDocumentId === document.id ||
                        sharingDocumentId === document.id
                      }
                    >
                      {deletingDocumentId === document.id ? 'Deleting...' : 'Delete'}
                    </button>
                  </div>
                ) : (
                  <span className="viewer-badge">Shared with you</span>
                )}
              </article>
            ))}
          </div>
        )}
          </section>
        </>
      )}
    </main>
  )
}

function loadStoredAuth() {
  const raw = localStorage.getItem(AUTH_STORAGE_KEY)
  if (!raw) {
    return null
  }

  try {
    return JSON.parse(raw) as AuthResponse
  } catch {
    localStorage.removeItem(AUTH_STORAGE_KEY)
    return null
  }
}

function formatBytes(bytes: number) {
  if (bytes < 1024) {
    return `${bytes} B`
  }

  if (bytes < 1024 * 1024) {
    return `${(bytes / 1024).toFixed(1)} KB`
  }

  return `${(bytes / 1024 / 1024).toFixed(1)} MB`
}

export default App
