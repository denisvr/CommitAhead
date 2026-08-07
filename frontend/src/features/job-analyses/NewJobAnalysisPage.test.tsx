import { describe, it, expect, vi } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import { server } from '../../mocks/server'
import { NewJobAnalysisPage } from './NewJobAnalysisPage'

describe('NewJobAnalysisPage', () => {
  it('creates a JobAnalysis from pasted text by default', async () => {
    let requestBody: Record<string, unknown> | undefined
    server.use(
      http.post('/api/job-analyses', async ({ request }) => {
        requestBody = (await request.json()) as Record<string, unknown>
        return HttpResponse.json({ id: 'new-id' }, { status: 201 })
      }),
    )
    const onCreated = vi.fn()

    render(<NewJobAnalysisPage onCreated={onCreated} onCancel={vi.fn()} />)
    await userEvent.type(screen.getByLabelText('Title'), 'Acme — Backend Engineer')
    await userEvent.type(screen.getByLabelText('Job posting text'), 'We are looking for a backend engineer.')
    await userEvent.click(screen.getByRole('button', { name: 'Create' }))

    expect(onCreated).toHaveBeenCalledWith('new-id')
    expect(requestBody?.title).toBe('Acme — Backend Engineer')
    expect(requestBody?.jobPostingText).toBe('We are looking for a backend engineer.')
  })

  it('switches to the upload tab and sends a multipart request with the chosen file', async () => {
    let contentType: string | null = null
    let receivedBody = ''
    server.use(
      http.post('/api/job-analyses/upload', async ({ request }) => {
        contentType = request.headers.get('content-type')
        // Reading via request.formData() hits a Vitest jsdom-environment bug (its FormData/File
        // compat shim downgrades File to a plain, unnamed Blob before handing the request to
        // Node's real fetch), so this reads the raw multipart body instead of decoding it.
        receivedBody = await request.text()
        return HttpResponse.json({ id: 'uploaded-id' }, { status: 201 })
      }),
    )
    const onCreated = vi.fn()

    render(<NewJobAnalysisPage onCreated={onCreated} onCancel={vi.fn()} />)
    await userEvent.type(screen.getByLabelText('Title'), 'Acme — Backend Engineer')
    await userEvent.click(screen.getByRole('tab', { name: 'Upload PDF' }))
    await userEvent.upload(screen.getByLabelText('PDF file'), new File(['%PDF-fake'], 'posting.pdf', { type: 'application/pdf' }))
    await userEvent.click(screen.getByRole('button', { name: 'Create' }))

    // The file part's own bytes aren't asserted here: Vitest's jsdom-environment compat shim
    // (its makeCompatBlob) drops a File's content and filename entirely when converting it for
    // Node's real fetch, an upstream Vitest limitation (confirmed against vitest@4.1.10) rather
    // than app behaviour — a real browser preserves both correctly.
    expect(onCreated).toHaveBeenCalledWith('uploaded-id')
    expect(contentType).toContain('multipart/form-data')
    expect(receivedBody).toContain('name="Title"')
    expect(receivedBody).toContain('Acme — Backend Engineer')
    expect(receivedBody).toContain('name="File"')
  })

  it('shows the backend-provided rejection reason instead of a generic message', async () => {
    server.use(
      http.post('/api/job-analyses/upload', () =>
        HttpResponse.json({ title: 'Validation failed.', detail: 'The uploaded PDF contains no extractable text.' }, { status: 422 }),
      ),
    )

    render(<NewJobAnalysisPage onCreated={vi.fn()} onCancel={vi.fn()} />)
    await userEvent.type(screen.getByLabelText('Title'), 'Acme — Backend Engineer')
    await userEvent.click(screen.getByRole('tab', { name: 'Upload PDF' }))
    await userEvent.upload(screen.getByLabelText('PDF file'), new File(['%PDF-fake'], 'posting.pdf', { type: 'application/pdf' }))
    await userEvent.click(screen.getByRole('button', { name: 'Create' }))

    expect(await screen.findByRole('alert')).toHaveTextContent('The uploaded PDF contains no extractable text.')
  })
})
