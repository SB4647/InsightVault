import { useEffect, useState } from 'react'
import type { FormEvent } from 'react'
import './App.css'
import { getDocuments, processDocument, uploadDocument } from './api/documents'
import type { DocumentDto } from './api/documents'

function App() {
  const [documents, setDocuments] = useState<DocumentDto[]>([])
  const [selectedFile, setSelectedFile] = useState<File | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [isUploading, setIsUploading] = useState(false)
  const [processingDocumentId, setProcessingDocumentId] = useState<string | null>(null)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    loadDocuments()
  }, [])

  async function loadDocuments() {
    setIsLoading(true)
    setError(null)

    try {
      setDocuments(await getDocuments())
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Could not load documents.')
    } finally {
      setIsLoading(false)
    }
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()

    if (!selectedFile) {
      setError('Choose a document before uploading.')
      return
    }

    setIsUploading(true)
    setError(null)

    try {
      const uploadedDocument = await uploadDocument(selectedFile)
      setDocuments((current) => [uploadedDocument, ...current])
      setSelectedFile(null)
      event.currentTarget.reset()
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
      const result = await processDocument(documentId)
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

  return (
    <main className="app-shell">
      <section className="page-header">
        <p className="eyebrow">InsightVault</p>
        <h1>Document Library</h1>
        <p>
          Upload source documents and keep their file metadata available for the
          processing, search, and chat phases later.
        </p>
      </section>

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

      {error && <p className="message error">{error}</p>}

      <section className="documents-section" aria-labelledby="documents-title">
        <div className="section-heading">
          <h2 id="documents-title">Uploaded documents</h2>
          <button type="button" onClick={loadDocuments} disabled={isLoading}>
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
                    <dt>Chunks</dt>
                    <dd>{document.chunkCount}</dd>
                  </div>
                  <div>
                    <dt>Uploaded</dt>
                    <dd>{new Date(document.uploadedAtUtc).toLocaleString()}</dd>
                  </div>
                </dl>
                <button
                  type="button"
                  onClick={() => handleProcess(document.id)}
                  disabled={processingDocumentId === document.id || document.status === 'Processed'}
                >
                  {processingDocumentId === document.id ? 'Processing...' : 'Process'}
                </button>
              </article>
            ))}
          </div>
        )}
      </section>
    </main>
  )
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
