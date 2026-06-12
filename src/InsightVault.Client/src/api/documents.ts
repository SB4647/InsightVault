export interface DocumentDto {
  id: string
  originalFileName: string
  contentType: string
  sizeInBytes: number
  blobName: string
  uploadedAtUtc: string
  status: string
}

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL ?? 'https://localhost:7227'

export async function getDocuments(): Promise<DocumentDto[]> {
  const response = await fetch(`${API_BASE_URL}/api/documents`)

  if (!response.ok) {
    throw new Error('Could not load documents.')
  }

  return response.json()
}

export async function uploadDocument(file: File): Promise<DocumentDto> {
  const formData = new FormData()
  formData.append('file', file)

  const response = await fetch(`${API_BASE_URL}/api/documents`, {
    method: 'POST',
    body: formData,
  })

  if (!response.ok) {
    const message = await response.text()
    throw new Error(message || 'Could not upload document.')
  }

  return response.json()
}
