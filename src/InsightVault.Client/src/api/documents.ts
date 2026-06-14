export interface DocumentDto {
  id: string
  originalFileName: string
  contentType: string
  sizeInBytes: number
  blobName: string
  uploadedAtUtc: string
  status: string
  chunkCount: number
  isOwner: boolean
  accessLevel: string
}

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL ?? 'https://localhost:7227'

export async function getDocuments(token: string): Promise<DocumentDto[]> {
  const response = await fetch(`${API_BASE_URL}/api/documents`, {
    headers: authHeaders(token),
  })

  if (!response.ok) {
    if (response.status === 401) {
      throw new Error('Your session has expired. Please log in again.')
    }

    const message = await response.text()
    throw new Error(message || 'Could not load documents.')
  }

  return response.json()
}

export async function uploadDocument(file: File, token: string): Promise<DocumentDto> {
  const formData = new FormData()
  formData.append('file', file)

  const response = await fetch(`${API_BASE_URL}/api/documents`, {
    method: 'POST',
    headers: authHeaders(token),
    body: formData,
  })

  if (!response.ok) {
    const message = await response.text()
    throw new Error(message || 'Could not upload document.')
  }

  return response.json()
}

export interface DocumentProcessingResultDto {
  documentId: string
  chunkCount: number
  status: string
}

export interface DocumentShareDto {
  documentId: string
  sharedWithUserId: string
  sharedWithEmail: string
  accessLevel: string
}

export async function processDocument(
  documentId: string,
  token: string,
): Promise<DocumentProcessingResultDto> {
  const response = await fetch(`${API_BASE_URL}/api/documents/${documentId}/process`, {
    method: 'POST',
    headers: authHeaders(token),
  })

  if (!response.ok) {
    const message = await response.text()
    throw new Error(message || 'Could not process document.')
  }

  return response.json()
}

export async function shareDocument(
  documentId: string,
  email: string,
  token: string,
): Promise<DocumentShareDto> {
  const response = await fetch(`${API_BASE_URL}/api/documents/${documentId}/share`, {
    method: 'POST',
    headers: {
      ...authHeaders(token),
      'Content-Type': 'application/json',
    },
    body: JSON.stringify({ email }),
  })

  if (!response.ok) {
    const message = await response.text()
    throw new Error(message || 'Could not share document.')
  }

  return response.json()
}

export async function deleteDocument(documentId: string, token: string): Promise<void> {
  const response = await fetch(`${API_BASE_URL}/api/documents/${documentId}`, {
    method: 'DELETE',
    headers: authHeaders(token),
  })

  if (!response.ok) {
    const message = await response.text()
    throw new Error(message || 'Could not delete document.')
  }
}

function authHeaders(token: string) {
  return {
    Authorization: `Bearer ${token}`,
  }
}
