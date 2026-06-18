export const SESSION_EXPIRED_MESSAGE = 'Your session has expired. Please log in again.'

export class SessionExpiredError extends Error {
  constructor() {
    super(SESSION_EXPIRED_MESSAGE)
    this.name = 'SessionExpiredError'
  }
}

export function isSessionExpiredError(error: unknown): error is SessionExpiredError {
  return error instanceof SessionExpiredError
}

export async function throwApiError(response: Response, fallbackMessage: string): Promise<never> {
  if (response.status === 401) {
    throw new SessionExpiredError()
  }

  const message = await response.text()
  throw new Error(message || fallbackMessage)
}
