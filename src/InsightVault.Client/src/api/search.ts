export interface SearchResultDto {
  documentId: string
  documentName: string
  chunkId: string
  chunkIndex: number
  text: string
  score: number
}

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL ?? 'https://localhost:7227'

export async function searchDocuments(query: string): Promise<SearchResultDto[]> {
  const params = new URLSearchParams({ query })
  const response = await fetch(`${API_BASE_URL}/api/search?${params.toString()}`)

  if (!response.ok) {
    const message = await response.text()
    throw new Error(message || 'Could not search documents.')
  }

  return response.json()
}
