export interface AuthResponse {
  userId: string
  email: string
  token: string
}

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL ?? 'https://localhost:7227'

export async function login(email: string, password: string): Promise<AuthResponse> {
  return authenticate('login', email, password)
}

export async function register(email: string, password: string): Promise<AuthResponse> {
  return authenticate('register', email, password)
}

async function authenticate(
  action: 'login' | 'register',
  email: string,
  password: string,
): Promise<AuthResponse> {
  const response = await fetch(`${API_BASE_URL}/api/auth/${action}`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify({ email, password }),
  })

  if (!response.ok) {
    const message = await response.text()
    throw new Error(message || `Could not ${action}.`)
  }

  return response.json()
}
