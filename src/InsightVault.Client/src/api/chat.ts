export interface SourceCitationDto {
  documentId: string
  documentName: string
  chunkId: string
  chunkIndex: number
  text: string
  score: number
}

export interface ChatResponseDto {
  answer: string
  sources: SourceCitationDto[]
}

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL ?? 'https://localhost:7227'

export async function askQuestion(question: string, token: string): Promise<ChatResponseDto> {
  const response = await fetch(`${API_BASE_URL}/api/chat`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      Authorization: `Bearer ${token}`,
    },
    body: JSON.stringify({ question }),
  })

  if (!response.ok) {
    const message = await response.text()
    throw new Error(message || 'Could not answer question.')
  }

  return response.json()
}
